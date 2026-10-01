using UnityEngine;
using Firebase.Firestore;

public class FirestoreManager : MonoBehaviour
{
    public static FirestoreManager Instance;

    public FirebaseFirestore DB { get; private set; }

    private void Awake()
    {
        if (Instance  == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);

            DB = FirebaseFirestore.DefaultInstance;
        }

        else
        {
            Destroy(gameObject);
        }
    }

    public CollectionReference AccountsCollection()
    {
        return DB.Collection("Classroom").Document(DatabaseManager.Instance.CurrentDatabase).Collection("accounts");
    }

    public CollectionReference ProgressCollection()
    {
        return DB.Collection("Classroom").Document(DatabaseManager.Instance.CurrentDatabase).Collection("progress");
    }

    public CollectionReference QuizCollection()
    {
        return DB.Collection("Classroom").Document(DatabaseManager.Instance.CurrentDatabase).Collection("quizzes");
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
