using UnityEngine;
using Firebase.Firestore;
using System.Collections.Generic;

public class FirestoreTest : MonoBehaviour
{
    private FirebaseFirestore db;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
        Dictionary<string, object> data = new Dictionary<string, object>();

        data["created"] = true;

        await FirestoreManager.Instance.DB
            .Collection("Classroom")
            .Document(DatabaseManager.Instance.CurrentDatabase)
            .SetAsync(data);

        Debug.Log("Classroom Created");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
