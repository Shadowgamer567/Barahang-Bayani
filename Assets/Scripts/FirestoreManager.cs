
using UnityEngine;
using Firebase.Firestore;

public class FirestoreManager : MonoBehaviour
{
    public static FirestoreManager Instance;

    public FirebaseFirestore DB { get; private set; }

#if UNITY_EDITOR
    private const bool USE_FIREBASE_EMULATOR = true;
#else
    private const bool USE_FIREBASE_EMULATOR = false;
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);

            DB = FirebaseFirestore.DefaultInstance;

            // Configure Firestore before any database operations.
            if (USE_FIREBASE_EMULATOR)
            {
                DB.Settings.Host = "127.0.0.1:8080";
                DB.Settings.SslEnabled = false;

                Debug.LogWarning(
                    "FIRESTORE EMULATOR MODE: Using localhost:8080"
                );
            }

            else
            {
                Debug.Log("Firestore connected to production.");
            }
        }

        else
        {
            Destroy(gameObject);
        }
    }

    public CollectionReference AccountsCollection()
    {
        return DB.Collection("Classroom")
            .Document(DatabaseManager.Instance.CurrentDatabase)
            .Collection("accounts");
    }

    public CollectionReference ProgressCollection()
    {
        return DB.Collection("Classroom")
            .Document(DatabaseManager.Instance.CurrentDatabase)
            .Collection("progress");
    }

    public CollectionReference AccountsCollection(string classroomID)
    {
        if (string.IsNullOrWhiteSpace(classroomID) ||
            classroomID == "DefaultDatabase")
        {
            throw new System.ArgumentException(
                "A valid classroom ID is required.",
                nameof(classroomID)
            );
        }

        return DB.Collection("Classroom")
            .Document(classroomID)
            .Collection("accounts");
    }

    public CollectionReference ProgressCollection(string classroomID)
    {
        if (string.IsNullOrWhiteSpace(classroomID) ||
            classroomID == "DefaultDatabase")
        {
            throw new System.ArgumentException(
                "A valid classroom ID is required.",
                nameof(classroomID)
            );
        }

        return DB.Collection("Classroom")
            .Document(classroomID)
            .Collection("progress");
    }

    public CollectionReference QuizCollection()
    {
        return DB.Collection("Classroom")
            .Document(DatabaseManager.Instance.CurrentDatabase)
            .Collection("quizzes");
    }
}
