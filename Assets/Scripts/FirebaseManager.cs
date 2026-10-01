using Firebase;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
        var result = await FirebaseApp.CheckAndFixDependenciesAsync();

        if(result == DependencyStatus.Available)
        {
            Debug.Log("Firebase Ready");
        }

        else
        {
            Debug.LogError("Firebase Error: " + result);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
