using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;

/// <summary>
/// Initializes Firebase for the desktop game. Authentication is performed
/// through the website handoff; anonymous sign-in is intentionally disabled.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }
    public static bool IsReady { get; private set; }
    public static Task InitializationTask => initializationTask;
    public static FirebaseAuth Auth { get; private set; }

    private static Task initializationTask = Task.CompletedTask;

    /// <summary>
    /// True only when running in the Unity Editor and the complete local
    /// development stack (Auth, Firestore, Functions, and Live Server) responds.
    /// Standalone builds always use production.
    /// </summary>
    public static bool UseEmulator { get; private set; }

    private static readonly (string host, int port, string service)[] EmulatorServices =
    {
        ("127.0.0.1", 9099, "Firebase Auth"),
        ("127.0.0.1", 8080, "Firestore"),
        ("127.0.0.1", 5001, "Cloud Functions"),
        ("127.0.0.1", 5500, "Website Live Server")
    };

    private static bool IsTcpEndpointAlive(string host, int port, int timeoutMs = 350)
    {
        using (var client = new System.Net.Sockets.TcpClient())
        {
            try
            {
                IAsyncResult result = client.BeginConnect(host, port, null, null);
                bool connected = result.AsyncWaitHandle.WaitOne(timeoutMs);

                if (!connected)
                    return false;

                client.EndConnect(result);
                return client.Connected;
            }
            catch
            {
                return false;
            }
        }
    }

    private static void SelectEnvironment()
    {
#if UNITY_EDITOR
        UseEmulator = true;

        foreach (var service in EmulatorServices)
        {
            bool alive = IsTcpEndpointAlive(service.host, service.port);
            Debug.Log("[Environment Detection] " + service.service + " (" +
                      service.host + ":" + service.port + "): " +
                      (alive ? "ONLINE" : "OFFLINE"));

            if (!alive)
            {
                UseEmulator = false;
                break;
            }
        }

        Debug.LogWarning(UseEmulator
            ? "[Environment Detection] Complete emulator stack detected. Using EMULATOR."
            : "[Environment Detection] Emulator stack unavailable or incomplete. Using PRODUCTION.");
#else
        UseEmulator = false;
        Debug.Log("[Environment Detection] Standalone build. Using PRODUCTION.");
#endif
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
        initializationTask = InitializeFirebaseAsync();
    }

    private async Task InitializeFirebaseAsync()
    {
        IsReady = false;

        try
        {
            // Decide once before creating Auth or any Firestore client.
            SelectEnvironment();

            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
            {
                Debug.LogError("Firebase dependencies unavailable: " + status);
                return;
            }

            Auth = FirebaseAuth.DefaultInstance;

            // Configure the emulator before any authentication operation.
            if (UseEmulator)
            {
                Auth.UseEmulator("127.0.0.1", 9099);
                Debug.LogWarning("Firebase Auth Emulator enabled at 127.0.0.1:9099");
            }

            IsReady = true;
            Debug.Log("Firebase initialized. Waiting for website authentication.");
        }
        catch (System.Exception exception)
        {
            Debug.LogError("Firebase initialization failed: " + exception);
        }

    }
    public static bool IsAuthenticated
    {
        get
        {
            return Auth != null && Auth.CurrentUser != null;
        }
    }

    public static async Task<bool> WaitUntilReady()
    {
        await InitializationTask;
        return IsReady && Auth != null;
    }
}
