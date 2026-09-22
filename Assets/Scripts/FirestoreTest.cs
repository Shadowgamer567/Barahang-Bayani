using UnityEngine;
using Firebase.Firestore;

public class FirestoreTest : MonoBehaviour
{
    private FirebaseFirestore db;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
        db = FirebaseFirestore.DefaultInstance;

        DocumentReference docRef = db.Collection("test").Document("unity");

        await docRef.SetAsync(new { messsage = "Hello Firestore", score = 100 });

        Debug.Log("Firestore Write Successful");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
