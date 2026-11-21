
using UnityEngine;
using Firebase.Firestore;

[DefaultExecutionOrder(-900)]
public class FirestoreManager : MonoBehaviour
{
    public static FirestoreManager Instance;

    public FirebaseFirestore DB { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);

            // FirebaseManager runs before this component and selects the
            // environment synchronously in its Awake method.
            DB = FirebaseFirestore.DefaultInstance;

            // Configure Firestore before any database operations.
            if (FirebaseManager.UseEmulator)
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
