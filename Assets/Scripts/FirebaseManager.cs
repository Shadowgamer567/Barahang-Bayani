using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;

/// <summary>
/// Initializes Firebase for the desktop game. Authentication is performed
/// through the website handoff; anonymous sign-in is intentionally disabled.
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }
    public static bool IsReady { get; private set; }
    public static Task InitializationTask => initializationTask;
    public static FirebaseAuth Auth { get; private set; }

    private static Task initializationTask = Task.CompletedTask;

#if UNITY_EDITOR
    private const bool USE_FIREBASE_EMULATOR = true;
#else
    private const bool USE_FIREBASE_EMULATOR = false;
#endif

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
            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
            {
                Debug.LogError("Firebase dependencies unavailable: " + status);
                return;
            }

            Auth = FirebaseAuth.DefaultInstance;

            // Configure the emulator before any authentication operation.
            if (USE_FIREBASE_EMULATOR)
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
