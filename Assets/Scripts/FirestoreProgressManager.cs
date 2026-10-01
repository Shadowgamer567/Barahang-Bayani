using Firebase.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FirestoreProgressManager : MonoBehaviour
{
    public static async Task UploadProgress(
        string accountID,
        ProgressData progress)
    {
        Dictionary<string, object> data =
            new Dictionary<string, object>();

        data["username"] = progress.username;
        data["accountType"] = progress.accountType;

        data["completedLevels"] =
            progress.completedLevel;

        data["lastCampaignScene"] =
            progress.lastCampaignScene;

        data["multipleChoiceCorrect"] =
            progress.multipleChoiceCorrect;

        data["multipleChoiceTotal"] =
            progress.multipleChoiceTotal;

        data["identificationCorrect"] =
            progress.identificationCorrect;

        data["identificationTotal"] =
            progress.identificationTotal;

        data["trueFalseCorrect"] =
            progress.trueFalseCorrect;

        data["trueFalseTotal"] =
            progress.trueFalseTotal;

        Debug.Log(
            "Uploading Progress | Account ID: " +
            accountID +
            " | Username: " +
            progress.username
        );

        await FirestoreManager.Instance
            .ProgressCollection()
            .Document(accountID)
            .SetAsync(data);

        Debug.Log(
            "Uploaded Progress for account: " +
            accountID
        );
    }


    public static async Task<ProgressData> DownloadProgress(
        string accountID)
    {
        DocumentReference document =
            FirestoreManager.Instance
                .ProgressCollection()
                .Document(accountID);

        DocumentSnapshot snapshot =
            await document.GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            Debug.Log(
                "No Firestore progress found for account: " +
                accountID
            );

            return null;
        }

        ProgressData progress = new ProgressData();

        if (snapshot.ContainsField("username"))
        {
            progress.username =
                snapshot.GetValue<string>("username");
        }

        if (snapshot.ContainsField("accountType"))
        {
            progress.accountType =
                snapshot.GetValue<string>("accountType");
        }

        if (snapshot.ContainsField("completedLevels"))
        {
            progress.completedLevel =
                snapshot.GetValue<List<string>>("completedLevels");
        }

        if (snapshot.ContainsField("lastCampaignScene"))
        {
            progress.lastCampaignScene =
                snapshot.GetValue<string>("lastCampaignScene");
        }

        if (snapshot.ContainsField("multipleChoiceCorrect"))
        {
            progress.multipleChoiceCorrect =
                snapshot.GetValue<int>("multipleChoiceCorrect");
        }

        if (snapshot.ContainsField("multipleChoiceTotal"))
        {
            progress.multipleChoiceTotal =
                snapshot.GetValue<int>("multipleChoiceTotal");
        }

        if (snapshot.ContainsField("identificationCorrect"))
        {
            progress.identificationCorrect =
                snapshot.GetValue<int>("identificationCorrect");
        }

        if (snapshot.ContainsField("identificationTotal"))
        {
            progress.identificationTotal =
                snapshot.GetValue<int>("identificationTotal");
        }

        if (snapshot.ContainsField("trueFalseCorrect"))
        {
            progress.trueFalseCorrect =
                snapshot.GetValue<int>("trueFalseCorrect");
        }

        if (snapshot.ContainsField("trueFalseTotal"))
        {
            progress.trueFalseTotal =
                snapshot.GetValue<int>("trueFalseTotal");
        }

        Debug.Log(
            "Downloaded Progress for: " +
            progress.username
        );

        return progress;
    }
}