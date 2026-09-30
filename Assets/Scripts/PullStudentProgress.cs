using Firebase.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class PullStudentProgress : MonoBehaviour
{
    public async void PullProgress()
    {
        // Make sure an account is selected
        if (AccountManager.Instance == null)
        {
            Debug.LogError("AccountManager instance not found.");
            return;
        }

        AccountData account = AccountManager.Instance.selectedAccount;

        if (account == null)
        {
            Debug.LogError("No account is currently selected.");
            return;
        }

        // Make sure the selected account is a student
        if (account.accountType != AccountType.Student)
        {
            Debug.LogError("The selected account is not a student.");
            return;
        }

        Debug.Log("Pulling progress for: " + account.username);

        try
        {
            DocumentSnapshot snapshot =
                await FirestoreManager.Instance
                    .ProgressCollection()
                    .Document(account.id)
                    .GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                Debug.LogWarning(
                    "No progress found in Firestore for: " +
                    account.username
                );

                return;
            }

            Dictionary<string, object> data =
                snapshot.ToDictionary();

            Debug.Log("Progress data pulled from Firestore for: " + account.username);

            ProgressData progress = new ProgressData();

            progress.accountId = account.id;
            progress.username = GetString(data, "username");
            progress.accountType = GetString(data, "accountType");

            progress.lastCampaignScene =
                GetString(data, "lastCampaignScene");

            progress.multipleChoiceCorrect =
                GetInt(data, "multipleChoiceCorrect");

            progress.multipleChoiceTotal =
                GetInt(data, "multipleChoiceTotal");

            progress.identificationCorrect =
                GetInt(data, "identificationCorrect");

            progress.identificationTotal =
                GetInt(data, "identificationTotal");

            progress.trueFalseCorrect =
                GetInt(data, "trueFalseCorrect");

            progress.trueFalseTotal =
                GetInt(data, "trueFalseTotal");

            progress.completedLevel =
                GetStringList(data, "completedLevels");

            Debug.Log(
                "Successfully pulled progress for: " +
                account.username
            );

            Debug.Log(
                "Completed Levels: " +
                progress.completedLevel.Count
            );

            Debug.Log(
                "Multiple Choice: " +
                progress.multipleChoiceCorrect +
                "/" +
                progress.multipleChoiceTotal
            );

            Debug.Log(
                "Identification: " +
                progress.identificationCorrect +
                "/" +
                progress.identificationTotal
            );

            Debug.Log(
                "True/False: " +
                progress.trueFalseCorrect +
                "/" +
                progress.trueFalseTotal
            );

            // Apply the downloaded progress to ProgressManager
            ApplyProgress(progress);
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Failed to pull student progress: " +
                e.Message
            );
        }
    }

    private void ApplyProgress(ProgressData data)
    {
        if (ProgressManager.Instance == null)
        {
            Debug.LogError("ProgressManager instance not found.");
            return;
        }

        ProgressManager progressManager =
            ProgressManager.Instance;

        progressManager.completedLevels =
            data.completedLevel ?? new List<string>();

        progressManager.lastCampaignScene =
            data.lastCampaignScene;

        Debug.Log("Firestore progress applied locally.");
    }

    private string GetString(
        Dictionary<string, object> data,
        string key)
    {
        if (data.ContainsKey(key) && data[key] != null)
        {
            return data[key].ToString();
        }

        return "";
    }

    private int GetInt(
        Dictionary<string, object> data,
        string key)
    {
        if (data.ContainsKey(key) && data[key] != null)
        {
            return System.Convert.ToInt32(data[key]);
        }

        return 0;
    }

    private List<string> GetStringList(
        Dictionary<string, object> data,
        string key)
    {
        List<string> result = new List<string>();

        if (!data.ContainsKey(key) || data[key] == null)
            return result;

        if (data[key] is List<object> list)
        {
            foreach (object item in list)
            {
                if (item != null)
                    result.Add(item.ToString());
            }
        }

        return result;
    }
}