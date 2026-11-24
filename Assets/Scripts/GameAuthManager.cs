using System;
using System.Collections;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Desktop browser sign-in bridge. Unity starts a temporary loopback HTTP
/// listener, opens the portal login page, receives a single-use code, then
/// redeems it through the callable Cloud Function.
/// </summary>
public class GameAuthManager : MonoBehaviour
{
    public static GameAuthManager Instance { get; private set; }

    [Header("Website URLs")]
    [Tooltip("Firebase Hosting login URL for production.")]
    [SerializeField] private string productionLoginUrl = "https://barahangbayani.web.app/";

    [Tooltip("Live Server URL used while testing with Firebase emulators.")]
    [SerializeField] private string emulatorLoginUrl = "http://127.0.0.1:5500/";

    [Header("Cloud Function")]
    [SerializeField]
    private string productionRedeemUrl =
        "https://us-central1-barahangbayani.cloudfunctions.net/redeemGameAuthCode";

    [SerializeField]
    private string emulatorRedeemUrl =
        "http://127.0.0.1:5001/barahangbayani/us-central1/redeemGameAuthCode";

    [Header("Session")]
    // Keep Firebase's persisted authentication session across launches.
    // Explicit logout clears both Firebase auth and the local offline identity.

    public bool IsSigningIn { get; private set; }
    public bool IsOnlineAuthenticated => FirebaseManager.Auth != null &&
        FirebaseManager.Auth.CurrentUser != null &&
        !FirebaseManager.Auth.CurrentUser.IsAnonymous;

    // IsSignedIn means the player has an active game identity. It may be a
    // cached identity while offline; protected cloud calls must use
    // IsOnlineAuthenticated instead.
    public bool IsSignedIn => IsOnlineAuthenticated || IsOfflineSession;
    public bool IsOfflineSession { get; private set; }

    private const string CachedSessionFileName = "cached_game_session.json";
    private string CachedSessionPath =>
        System.IO.Path.Combine(Application.persistentDataPath, CachedSessionFileName);

    [Serializable]
    private class CachedGameSession
    {
        public string uid;
        public string username;
        public string role;
        public string createdAt;
        public string classroomID;
    }

    // StudentUID is retained as a compatibility alias for older progress code.
    // The authenticated identity may be either a Student or a Teacher.
    public string StudentUID { get; private set; }
    public string UserUID => StudentUID;
    public string Username { get; private set; }
    public string Role { get; private set; }
    public string CreatedAt { get; private set; }
    public string SelectedClassroomID { get; private set; } = "";

    public bool IsStudent => string.Equals(Role, "Student", StringComparison.OrdinalIgnoreCase);
    public bool IsTeacher => string.Equals(Role, "Teacher", StringComparison.OrdinalIgnoreCase);

    public void SignOut()
    {
        StopListener();

        if (FirebaseManager.Auth != null &&
            FirebaseManager.Auth.CurrentUser != null)
        {
            FirebaseManager.Auth.SignOut();
        }

        IsOfflineSession = false;
        DeleteCachedSession();

        StudentUID = string.Empty;
        Username = string.Empty;
        Role = string.Empty;
        CreatedAt = string.Empty;
        SelectedClassroomID = string.Empty;

        CurrentAccount.ActiveAccount = null;
        ProgressManager.Instance?.ClearActiveAccount();

        if (DatabaseManager.Instance != null)
            DatabaseManager.Instance.SetDatabase("DefaultDatabase");

        Debug.Log("Website game session signed out.");
    }

    public void ClearSelectedClassroom()
    {
        SelectedClassroomID = string.Empty;

        if (CurrentAccount.ActiveAccount != null)
            CurrentAccount.ActiveAccount.classroomID = string.Empty;

        // Do not clear the authenticated user or their local progress session.
        // A classroom must be selected before any classroom-scoped Firestore
        // progress operation can run.
    }

    public void SetSelectedClassroom(string classroomID)
    {
        if (string.IsNullOrWhiteSpace(classroomID))
            throw new ArgumentException("A valid website classroom ID is required.", nameof(classroomID));

        if (!IsSignedIn)
            throw new InvalidOperationException("Cannot select a classroom without an account session.");

        SelectedClassroomID = classroomID.Trim();
        SaveCachedSession();

        if (CurrentAccount.ActiveAccount != null)
            CurrentAccount.ActiveAccount.classroomID = SelectedClassroomID;

        if (ProgressManager.Instance != null && CurrentAccount.ActiveAccount != null)
            ProgressManager.Instance.SetActiveAccount(CurrentAccount.ActiveAccount);
    }

    private bool UseEmulator => FirebaseManager.UseEmulator;

    private HttpListener listener;
    private string expectedState;

    // Keep Unity running while the external browser is in front, then restore
    // the user's previous setting when the game regains focus.
    private bool previousRunInBackground;
    private bool backgroundExecutionForLogin;

    private void EnableBackgroundExecutionForLogin()
    {
        if (backgroundExecutionForLogin)
            return;

        previousRunInBackground = Application.runInBackground;
        backgroundExecutionForLogin = true;
        Application.runInBackground = true;

        Debug.Log("[GameAuth] Background execution enabled for browser login.");
    }

    private void RestoreBackgroundExecution()
    {
        if (!backgroundExecutionForLogin)
            return;

        Application.runInBackground = previousRunInBackground;
        backgroundExecutionForLogin = false;

        Debug.Log("[GameAuth] Restored Application.runInBackground to " +
            previousRunInBackground + ".");
    }


    private int callbackPort;
    private string receivedCode;
    private string receivedState;
    private string listenerError;

    [Serializable]
    private class CallableRequest
    {
        public CallableData data;
    }

    [Serializable]
    private class CallableData
    {
        public string code;
    }

    [Serializable]
    private class CallableResponse
    {
        public CallableResult result;
        public CallableResult data;
        public CallableError error;
    }

    [Serializable]
    private class CallableResult
    {
        public bool success;
        public string customToken;
        public string uid;
        public string username;
        public string role;
        public string createdAt;
    }

    [Serializable]
    private class CallableError
    {
        public CallableErrorDetail details;
        public string message;
    }

    [Serializable]
    private class CallableErrorDetail
    {
        public string message;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        bool firebaseReady = await FirebaseManager.WaitUntilReady();
        bool currentlyOffline =
            Application.internetReachability == NetworkReachability.NotReachable;

        // Online launches always begin at the MainMenu without restoring the
        // previous account session. Keep the cache file so it can be used if
        // the player later launches while offline.
        if (!currentlyOffline)
        {
            if (firebaseReady && FirebaseManager.Auth != null &&
                FirebaseManager.Auth.CurrentUser != null)
            {
                FirebaseManager.Auth.SignOut();
            }

            IsOfflineSession = false;
            StudentUID = string.Empty;
            Username = string.Empty;
            Role = string.Empty;
            CreatedAt = string.Empty;
            SelectedClassroomID = string.Empty;
            CurrentAccount.ActiveAccount = null;
            ProgressManager.Instance?.ClearActiveAccount();

            if (DatabaseManager.Instance != null)
                DatabaseManager.Instance.SetDatabase("DefaultDatabase");

            Debug.Log("Online startup: previous session was not restored. Opening MainMenu.");
            return;
        }

        // Offline launch: Firebase may or may not initialize. Either way,
        // restore only the local identity and save; cloud calls remain paused.
        if (!firebaseReady)
        {
            RestoreCachedOfflineSession();
            return;
        }

        CachedGameSession cached = ReadCachedSession();
        if (cached != null)
        {
            RestoreCachedOfflineSession(cached);
            return;
        }

        Debug.LogWarning("Offline startup: no cached account is available.");
    }

    private void RestoreCachedOfflineSession()
    {
        RestoreCachedOfflineSession(ReadCachedSession());
    }

    private void RestoreCachedOfflineSession(CachedGameSession cached)
    {
        if (cached == null || string.IsNullOrWhiteSpace(cached.uid) ||
            (!string.Equals(cached.role, "Student", StringComparison.OrdinalIgnoreCase) &&
             !string.Equals(cached.role, "Teacher", StringComparison.OrdinalIgnoreCase)))
        {
            Debug.LogWarning("No valid cached account session is available for offline play.");
            return;
        }

        RestoreCachedIdentity(cached, offline: true);
        Debug.LogWarning("Offline session restored. Cloud synchronization is paused until the account reconnects.");
    }

    private void RestoreCachedIdentity(CachedGameSession cached, bool offline)
    {
        StudentUID = cached.uid;
        Username = cached.username;
        Role = cached.role;
        CreatedAt = cached.createdAt;
        SelectedClassroomID = cached.classroomID ?? string.Empty;
        IsOfflineSession = offline;

        AccountData session = new AccountData
        {
            id = StudentUID,
            username = Username,
            accountType = IsTeacher ? AccountType.Teacher : AccountType.Student,
            classroomID = SelectedClassroomID
        };

        CurrentAccount.ActiveAccount = session;
        if (ProgressManager.Instance != null)
            ProgressManager.Instance.SetActiveAccount(session);

        if (!string.IsNullOrWhiteSpace(SelectedClassroomID) && DatabaseManager.Instance != null)
            DatabaseManager.Instance.SetDatabase(SelectedClassroomID);

        // Do not change the startup screen here. Cached identity restoration
        // is for authentication/progress state only; MainMenu remains visible.

        // Load the local save immediately. Do not call cloud endpoints from
        // this restoration path.
        if (ProgressManager.Instance != null)
            ProgressManager.Instance.LoadProgress();
    }

    private CachedGameSession ReadCachedSession()
    {
        try
        {
            if (!System.IO.File.Exists(CachedSessionPath))
                return null;

            return JsonUtility.FromJson<CachedGameSession>(
                System.IO.File.ReadAllText(CachedSessionPath));
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Could not read cached game session: " + ex.Message);
            return null;
        }
    }

    private void SaveCachedSession()
    {
        if (string.IsNullOrWhiteSpace(StudentUID) || string.IsNullOrWhiteSpace(Role))
            return;

        try
        {
            CachedGameSession cached = new CachedGameSession
            {
                uid = StudentUID,
                username = Username,
                role = Role,
                createdAt = CreatedAt,
                classroomID = SelectedClassroomID
            };
            System.IO.File.WriteAllText(CachedSessionPath, JsonUtility.ToJson(cached, true));
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to save cached game session: " + ex.Message);
        }
    }

    private void DeleteCachedSession()
    {
        try
        {
            if (System.IO.File.Exists(CachedSessionPath))
                System.IO.File.Delete(CachedSessionPath);
        }
        catch (Exception ex)
        {
            Debug.LogError("Failed to clear cached game session: " + ex.Message);
        }
    }

    /// <summary>Connect this method to the Accounts Menu Login button.</summary>
    public async void BeginWebsiteLogin()
    {
        if (IsSigningIn)
        {
            Debug.LogWarning("A game login is already in progress.");
            return;
        }

        IsSigningIn = true;
        receivedCode = null;
        receivedState = null;
        listenerError = null;

        try
        {
            if (!await FirebaseManager.WaitUntilReady())
                throw new Exception("Firebase is not ready.");

            if (FirebaseManager.Auth.CurrentUser != null)
                FirebaseManager.Auth.SignOut();

            callbackPort = FindAvailablePort();
            expectedState = Guid.NewGuid().ToString("N");

            listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:" + callbackPort + "/");
            listener.Start();

            _ = WaitForBrowserCallback();

            string redirect = "http://127.0.0.1:" + callbackPort + "/";
            string baseUrl = UseEmulator ? emulatorLoginUrl : productionLoginUrl;
            string loginUrl = baseUrl +
                (baseUrl.Contains("?") ? "&" : "?") +
                "unity=1&redirect=" + Uri.EscapeDataString(redirect) +
                "&state=" + Uri.EscapeDataString(expectedState);

            EnableBackgroundExecutionForLogin();
            Application.OpenURL(loginUrl);
            StartCoroutine(WaitForCodeAndRedeem());
        }
        catch (Exception exception)
        {
            Debug.LogError("Unable to start website login: " + exception);
            StopListener();
            RestoreBackgroundExecution();
            IsSigningIn = false;
        }
    }

    private async Task WaitForBrowserCallback()
    {
        try
        {
            HttpListenerContext context = await listener.GetContextAsync();
            string code = context.Request.QueryString["code"];
            string state = context.Request.QueryString["state"];

            string html = @"
            <!DOCTYPE html>
            <html lang=""en"">
            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Barahang Bayani - Login Complete</title>

                <style>
                    * {
                        box-sizing: border-box;
                    }

                    body {
                        margin: 0;
                        min-height: 100vh;
                        display: flex;
                        justify-content: center;
                        align-items: center;
                        padding: 24px;
                        font-family: Georgia, 'Times New Roman', serif;
                        background:
                            radial-gradient(circle at top, #625038, #29251d 75%);
                        color: #3c2b1b;
                    }

                    .card {
                        width: 100%;
                        max-width: 440px;
                        padding: 42px 32px;
                        text-align: center;
                        background: #eee0bd;
                        border: 3px solid #a47a3e;
                        outline: 1px solid #d2b477;
                        outline-offset: -10px;
                        border-radius: 8px;
                        box-shadow: 0 18px 50px rgba(0,0,0,0.4);
                    }

                    .icon {
                        width: 76px;
                        height: 76px;
                        margin: 0 auto 22px;
                        display: flex;
                        align-items: center;
                        justify-content: center;
                        border: 3px solid #8b713c;
                        border-radius: 50%;
                        color: #52633b;
                        font-family: Arial, sans-serif;
                        font-size: 42px;
                    }

                    h1 {
                        margin: 0 0 12px;
                        color: #553b23;
                        font-size: 29px;
                    }

                    .subtitle {
                        margin: 0 0 24px;
                        color: #796343;
                        font-size: 13px;
                        letter-spacing: 3px;
                        text-transform: uppercase;
                    }

                    .message {
                        margin: 0 auto 28px;
                        max-width: 320px;
                        font-family: Arial, sans-serif;
                        font-size: 15px;
                        line-height: 1.7;
                        color: #574630;
                    }

                    .divider {
                        width: 65%;
                        height: 1px;
                        margin: 0 auto 22px;
                        background: #b99b69;
                    }

                    .countdown {
                        font-family: Arial, sans-serif;
                        font-size: 13px;
                        color: #78603e;
                    }

                    #seconds {
                        font-weight: bold;
                        color: #62451f;
                    }

                    .footer {
                        margin-top: 22px;
                        font-family: Arial, sans-serif;
                        font-size: 12px;
                        color: #927b57;
                    }
                </style>
            </head>

            <body>
                <main class=""card"">
                    <div class=""icon"">&#10003;</div>

                    <h1>Barahang Bayani</h1>
                    <p class=""subtitle"">Game Connection</p>

                    <p class=""message"">
                        Your sign-in request has been received successfully.
                        You may now return to the game and continue your adventure.
                    </p>

                    <div class=""divider""></div>

                    <p class=""countdown"">
                        This page will attempt to close in
                        <span id=""seconds"">5</span> seconds.
                    </p>

                    <p class=""footer"">
                        If the browser prevents automatic closing,
                        you may close this tab manually.
                    </p>
                </main>

                <script>
                    let remaining = 5;
                    const seconds = document.getElementById('seconds');

                    const countdown = setInterval(() => {
                        remaining--;
                        seconds.textContent = remaining;

                        if (remaining <= 0) {
                            clearInterval(countdown);
                            window.close();
                        }
                    }, 1000);
                </script>
            </body>
            </html>";

            byte[] response = System.Text.Encoding.UTF8.GetBytes(html);


            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = response.Length;
            await context.Response.OutputStream.WriteAsync(response, 0, response.Length);
            context.Response.Close();

            receivedCode = code;
            receivedState = state;

            Debug.Log("[GameAuth] Browser callback received.");
            Debug.Log("[GameAuth] Authorization code received: " + !string.IsNullOrEmpty(receivedCode));
            Debug.Log("[GameAuth] State received: " + !string.IsNullOrEmpty(receivedState));
        }
        catch (Exception exception)
        {
            listenerError = exception.Message;
        }
    }

    private IEnumerator WaitForCodeAndRedeem()
    {
        Debug.Log("[GameAuth] Waiting for browser callback...");

        float timeout = 180f;
        while (timeout > 0f && string.IsNullOrEmpty(receivedCode) &&
               string.IsNullOrEmpty(listenerError))
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        StopListener();

        if (!string.IsNullOrEmpty(listenerError))
        {
            Debug.LogError("Browser callback failed: " + listenerError);
            IsSigningIn = false;
            yield break;
        }

        if (string.IsNullOrEmpty(receivedCode))
        {
            Debug.LogWarning("Website sign-in timed out.");
            IsSigningIn = false;
            yield break;
        }

        if (!string.Equals(receivedState, expectedState, StringComparison.Ordinal))
        {
            Debug.LogError("Rejected browser callback: state validation failed.");
            IsSigningIn = false;
            yield break;
        }

        Debug.Log("[GameAuth] Callback received. Starting code redemption...");
        yield return RedeemCode(receivedCode);
    }

    private IEnumerator RedeemCode(string code)
    {
        Debug.Log("[GameAuth] RedeemCode started.");

        string endpoint = UseEmulator ? emulatorRedeemUrl : productionRedeemUrl;

        Debug.Log("[GameAuth] Redemption endpoint: " + endpoint);

        string json = JsonUtility.ToJson(new CallableRequest
        {
            data = new CallableData { code = code }
        });

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            Debug.Log("[GameAuth] Redemption request completed. Result: " + request.result);
            Debug.Log("[GameAuth] HTTP Response: " + request.responseCode);
            Debug.Log("[GameAuth] Response Body: " + request.downloadHandler.text);

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Game authorization redemption failed: " +
                    request.responseCode + " " + request.error + " " +
                    request.downloadHandler.text);
                IsSigningIn = false;
                yield break;
            }

            CallableResponse response;
            try
            {
                response = JsonUtility.FromJson<CallableResponse>(
                    request.downloadHandler.text);
            }
            catch (Exception exception)
            {
                Debug.LogError("Invalid authorization response: " + exception.Message);
                IsSigningIn = false;
                yield break;
            }

            CallableResult result = response.result ?? response.data;
            if (result == null || !result.success ||
                string.IsNullOrEmpty(result.customToken) ||
                string.IsNullOrEmpty(result.uid))
            {
                Debug.LogError("The authorization service returned an incomplete response.");
                IsSigningIn = false;
                yield break;
            }

            Debug.Log("[GameAuth] Custom token received. Starting Firebase sign-in...");

            Task signInTask =
                FirebaseManager.Auth.SignInWithCustomTokenAsync(result.customToken);

            yield return new WaitUntil(() => signInTask.IsCompleted);

            Debug.Log("[GameAuth] Firebase sign-in task completed.");

            if (signInTask.IsFaulted || signInTask.IsCanceled)
            {
                Debug.LogError("[GameAuth] Firebase sign-in failed: " +
                    (signInTask.Exception != null
                        ? signInTask.Exception.ToString()
                        : "Cancelled"));

                IsSigningIn = false;
                yield break;
            }

            Debug.Log("[GameAuth] Firebase sign-in successful.");
            Debug.Log("[GameAuth] Current Firebase UID: " +
                FirebaseManager.Auth.CurrentUser.UserId);

            yield return new WaitUntil(() => signInTask.IsCompleted);

            if (signInTask.IsFaulted || signInTask.IsCanceled)
            {
                Debug.LogError("Firebase game sign-in failed: " +
                    (signInTask.Exception != null ? signInTask.Exception.ToString() : "Cancelled"));
                IsSigningIn = false;
                yield break;
            }

            StudentUID = result.uid;
            Username = result.username;
            Role = result.role;
            CreatedAt = result.createdAt;
            SelectedClassroomID = "";
            IsOfflineSession = false;
            SaveCachedSession();

            if (!IsStudent && !IsTeacher)
            {
                FirebaseManager.Auth.SignOut();
                Debug.LogError("Website returned an unsupported game role: " + Role);
                IsSigningIn = false;
                yield break;
            }

            // Supply the existing progress manager with a lightweight session
            // identity. This does not create or upload a legacy account record.
            AccountData session = new AccountData
            {
                id = StudentUID,
                username = Username,
                accountType = IsTeacher ? AccountType.Teacher : AccountType.Student,
                // Do not inherit a classroom left by a previous local account.
                classroomID = ""
            };

            CurrentAccount.ActiveAccount = session;

            if (ProgressManager.Instance != null)
                ProgressManager.Instance.SetActiveAccount(session);

            // Switch the existing account area to its website-authenticated
            // mode, then let the classroom manager request authorized rooms.
            if (AccountManager.Instance != null)
                AccountManager.Instance.OpenWebsiteAccountMenu();

            if (FirestoreClassroomManager.Instance != null)
                FirestoreClassroomManager.Instance.OpenClassroomSelection();

            Debug.Log("Website game sign-in successful. UID: " + StudentUID +
                " | Role: " + Role);

            // Login and Firebase token redemption have succeeded. Unity no
            // longer needs to keep running in the background for the browser.
            RestoreBackgroundExecution();

            IsSigningIn = false;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus || !IsOnlineAuthenticated ||
            Application.internetReachability == NetworkReachability.NotReachable)
            return;

        if (IsOfflineSession)
        {
            IsOfflineSession = false;
            Debug.Log("Connectivity restored. Resuming cloud progress synchronization.");
        }

        if (ProgressManager.Instance != null &&
            CurrentAccount.ActiveAccount != null &&
            !string.IsNullOrWhiteSpace(SelectedClassroomID))
        {
            _ = ProgressManager.Instance.LoadProgressForActiveAccount();
        }
    }

    private static int FindAvailablePort()
    {
        var probe = new System.Net.Sockets.TcpListener(
            System.Net.IPAddress.Loopback, 0);
        probe.Start();
        int port = ((System.Net.IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    private void StopListener()
    {
        try
        {
            if (listener != null)
            {
                if (listener.IsListening)
                    listener.Stop();
                listener.Close();
                listener = null;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Could not close game login listener: " + exception.Message);
        }
    }

    private void OnDestroy()
    {
        RestoreBackgroundExecution();
        StopListener();

        if (Instance == this)
            Instance = null;
    }
}
