using System;
using System.Collections;
using System.Net;
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
    [SerializeField] private string productionRedeemUrl =
        "https://us-central1-barahangbayani.cloudfunctions.net/redeemGameAuthCode";

    [SerializeField] private string emulatorRedeemUrl =
        "http://127.0.0.1:5001/barahangbayani/us-central1/redeemGameAuthCode";

    [Header("Session")]
    [SerializeField] private bool signOutOnStartup = true;

    public bool IsSigningIn { get; private set; }
    public bool IsSignedIn => FirebaseManager.Auth != null &&
        FirebaseManager.Auth.CurrentUser != null &&
        !FirebaseManager.Auth.CurrentUser.IsAnonymous;

    public string StudentUID { get; private set; }
    public string Username { get; private set; }
    public string Role { get; private set; }

#if UNITY_EDITOR
    private const bool USE_EMULATOR = true;
#else
    private const bool USE_EMULATOR = false;
#endif

    private HttpListener listener;
    private string expectedState;
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
        if (!await FirebaseManager.WaitUntilReady())
        {
            Debug.LogError("Game sign-in unavailable because Firebase did not initialize.");
            return;
        }

        if (signOutOnStartup && FirebaseManager.Auth.CurrentUser != null)
        {
            FirebaseManager.Auth.SignOut();
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
            string baseUrl = USE_EMULATOR ? emulatorLoginUrl : productionLoginUrl;
            string loginUrl = baseUrl +
                (baseUrl.Contains("?") ? "&" : "?") +
                "unity=1&redirect=" + Uri.EscapeDataString(redirect) +
                "&state=" + Uri.EscapeDataString(expectedState);

            Application.OpenURL(loginUrl);
            StartCoroutine(WaitForCodeAndRedeem());
        }
        catch (Exception exception)
        {
            Debug.LogError("Unable to start website login: " + exception);
            StopListener();
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

            byte[] response = System.Text.Encoding.UTF8.GetBytes(
                "<html><body><h2>Barahang Bayani</h2><p>Sign-in received. You may return to the game.</p></body></html>");

            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = response.Length;
            await context.Response.OutputStream.WriteAsync(response, 0, response.Length);
            context.Response.Close();

            receivedCode = code;
            receivedState = state;
        }
        catch (Exception exception)
        {
            listenerError = exception.Message;
        }
    }

    private IEnumerator WaitForCodeAndRedeem()
    {
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

        yield return RedeemCode(receivedCode);
    }

    private IEnumerator RedeemCode(string code)
    {
        string endpoint = USE_EMULATOR ? emulatorRedeemUrl : productionRedeemUrl;
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

            Task signInTask =
                FirebaseManager.Auth.SignInWithCustomTokenAsync(result.customToken);

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

            // Supply the existing progress manager with a lightweight session
            // identity. This does not create or upload a legacy account record.
            AccountData session = new AccountData
            {
                id = StudentUID,
                username = Username,
                accountType = Role == "Teacher" ? AccountType.Teacher : AccountType.Student,
                classroomID = DatabaseManager.Instance != null
                    ? DatabaseManager.Instance.CurrentDatabase
                    : ""
            };

            CurrentAccount.ActiveAccount = session;

            if (ProgressManager.Instance != null)
                ProgressManager.Instance.SetActiveAccount(session);

            Debug.Log("Website game sign-in successful. UID: " + StudentUID +
                " | Role: " + Role);

            IsSigningIn = false;
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
        StopListener();
        if (Instance == this)
            Instance = null;
    }
}
