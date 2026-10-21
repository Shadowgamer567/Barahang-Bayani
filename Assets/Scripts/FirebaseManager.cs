using Firebase;
using Firebase.Auth;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static bool IsAuthenticated { get; private set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private async void Start()
    {
        IsAuthenticated = false;

        try
        {
            DependencyStatus result =
                await FirebaseApp.CheckAndFixDependenciesAsync();

            if (result != DependencyStatus.Available)
            {
                Debug.LogError("Firebase Error: " + result);
                return;
            }

            Debug.Log("Firebase Ready");

            FirebaseAuth auth = FirebaseAuth.DefaultInstance;

            if (auth.CurrentUser == null)
            {
                await auth.SignInAnonymouslyAsync();
            }

            if (auth.CurrentUser != null)
            {
                IsAuthenticated = true;

                Debug.Log(
                    "Firebase Anonymous Authentication Successful. UID: " +
                    auth.CurrentUser.UserId
                );
            }
            else
            {
                Debug.LogError("Firebase Authentication Failed.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Firebase Initialization/Authentication Error: " +
                e.Message
            );
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
