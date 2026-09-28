using Firebase.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FirestoreProgressManager : MonoBehaviour
{
    public static async Task UploadProgress(string accountID, ProgressData progress)
    {
        Dictionary<string, object> data = new Dictionary<string, object>();

        data["username"] = progress.username;
        data["accountType"] = progress.accountType;
        data["completedLevels"] = progress.completedLevel;
        data["lastCampaignScene"] = progress.lastCampaignScene;
        data["multipleChoiceCorrect"] = progress.multipleChoiceCorrect;
        data["multipleChoiceTotal"] = progress.multipleChoiceTotal;
        data["identificationCorrect"] = progress.identificationCorrect;
        data["identificationTotal"] = progress.identificationTotal;
        data["trueFalseCorrect"] = progress.trueFalseCorrect;
        data["trueFalseTotal"] = progress.trueFalseTotal;

        await FirestoreManager.Instance.ProgressCollection().Document().SetAsync(data);

        Debug.Log("Uploaded Progress");
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
