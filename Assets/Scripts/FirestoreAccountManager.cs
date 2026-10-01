using Firebase.Auth;
using Firebase.Firestore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class FirestoreAccountManager : MonoBehaviour
{
    public static async Task UploadAccount(AccountData account)
    {
        Dictionary<string, object> data = new Dictionary<string, object>();

        data["id"] = account.id;
        data["username"] = account.username;
        data["passwordHash"] = account.passwordHash;
        data["accountType"] = account.accountType.ToString();
        data["assignedStudentIDs"] = account.assignedStudentIDs;

        await FirestoreManager.Instance
            .AccountsCollection()
            .Document(account.id)
            .SetAsync(data);

        Debug.Log("Uploaded Account: " + account.username);
    }

    public async Task DownloadAccounts()
    {
        string classroomID = DatabaseManager.Instance.CurrentDatabase;

        QuerySnapshot snapshot = await FirestoreManager.Instance
            .AccountsCollection()
            .GetSnapshotAsync();

        AccountManager.Instance.registry.accounts.Clear();

        foreach(DocumentSnapshot doc in snapshot.Documents)
        {
            Dictionary<string, object> data = doc.ToDictionary();

            AccountData account = new AccountData();

            account.id = data["id"].ToString();

            account.username = data["username"].ToString();

            account.passwordHash = data["passwordHash"].ToString();

            account.accountType = (AccountType)System.Enum.Parse(typeof(AccountType), data["accountType"].ToString());

            if (data.ContainsKey("assignedStudentIDs"))
            {
                List<object> firestoreList = (List<object>)data["assignedStudentIDs"];

                account.assignedStudentIDs = new List<string>();

                foreach (object item in firestoreList)
                {
                    account.assignedStudentIDs.Add(item.ToString());
                }
            }

            AccountManager.Instance.registry.accounts.Add(account);
        }

        AccountManager.Instance.SaveAccount();

        AccountManager.Instance.RefreshUI();

        Debug.Log("Downloaded " + snapshot.Documents.Count() + " accounts.");
    }

    public async void TestDownloadAccounts()
    {
        await DownloadAccounts();
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
