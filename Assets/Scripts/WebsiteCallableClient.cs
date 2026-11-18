using System;
using System.Text;
using System.Threading.Tasks;
using Firebase.Auth;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Small authenticated client for the website's Firebase callable functions.
/// Uses the callable HTTP protocol so the Unity project does not need the
/// Firebase Functions Unity package.
/// </summary>
public static class WebsiteCallableClient
{
#if UNITY_EDITOR
    private const bool USE_EMULATOR = true;
#else
    private const bool USE_EMULATOR = false;
#endif

    private const string PROJECT_ID = "barahangbayani";
    private const string REGION = "us-central1";

    [Serializable]
    private class CallableError
    {
        public string message;
        public string status;
    }

    [Serializable]
    private class CallableSuccessEnvelope<T>
    {
        public T result;
        public T data;
    }

    [Serializable]
    private class CallableFailureEnvelope
    {
        public CallableError error;
    }

    [Serializable]
    private class EmptyRequest
    {
        public EmptyData data = new EmptyData();
    }

    [Serializable]
    private class EmptyData { }

    public static async Task<T> CallAsync<T>(string functionName, string dataJson = "{}") where T : class
    {
        if (string.IsNullOrWhiteSpace(functionName))
            throw new ArgumentException("A callable function name is required.", nameof(functionName));

        if (FirebaseManager.Auth == null || FirebaseManager.Auth.CurrentUser == null ||
            FirebaseManager.Auth.CurrentUser.IsAnonymous)
        {
            throw new InvalidOperationException("A signed-in website account is required.");
        }

        string idToken = await FirebaseManager.Auth.CurrentUser.TokenAsync(false);
        string endpoint = BuildEndpoint(functionName);
        string body = "{\"data\":" + (string.IsNullOrWhiteSpace(dataJson) ? "{}" : dataJson) + "}";

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] payload = Encoding.UTF8.GetBytes(body);
            request.uploadHandler = new UploadHandlerRaw(payload);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + idToken);

            Debug.Log(
                "[Website Callable] Sending request\n" +
                "Function: " + functionName + "\n" +
                "Endpoint: " + endpoint + "\n" +
                "Payload: " + body
            );

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield();

            string responseText = request.downloadHandler != null
                ? request.downloadHandler.text
                : string.Empty;


            if (request.result != UnityWebRequest.Result.Success)
            {
                string message = request.responseCode + " " + request.error;

                try
                {
                    CallableFailureEnvelope errorEnvelope =
                        JsonUtility.FromJson<CallableFailureEnvelope>(responseText);

                    if (errorEnvelope != null && errorEnvelope.error != null)
                    {
                        message =
                            errorEnvelope.error.message ??
                            errorEnvelope.error.status ??
                            message;
                    }
                }
                catch { }

                Debug.LogError(
                    "[Website Callable] HTTP Failure\n" +
                    "Function: " + functionName + "\n" +
                    "Endpoint: " + endpoint + "\n" +
                    "Status: " + request.responseCode + "\n" +
                    "Error: " + message + "\n" +
                    "Response: " + responseText
                );

                throw new Exception(
                    "Website function '" + functionName + "' failed: " + message
                );
            }

            Debug.Log(
                "[Website Callable] Response received\n" +
                "Function: " + functionName + "\n" +
                "Status: " + request.responseCode +
                "\nResponse: " + responseText
            );

            CallableSuccessEnvelope<T> envelope =
                JsonUtility.FromJson<CallableSuccessEnvelope<T>>(responseText);

            if (envelope == null)
            {
                throw new Exception(
                    "Website function returned an unreadable response: " +
                    responseText
                );
            }

            T result = envelope.result != null
                ? envelope.result
                : envelope.data;

            if (result == null)
            {
                throw new Exception(
                    "Website function returned no result: " +
                    responseText
                );
            }

            return result;
        }
    }

    public static string EmptyDataJson()
    {
        return "{}";
    }
    
    private static string BuildEndpoint(string functionName)
    {
        if (USE_EMULATOR)
        {
            return "http://127.0.0.1:5001/" +
                   PROJECT_ID + "/" + REGION + "/" + functionName;
        }

        return "https://" + REGION + "-" + PROJECT_ID +
               ".cloudfunctions.net/" + functionName;
    }
}
