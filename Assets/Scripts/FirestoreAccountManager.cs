using Firebase.Auth;
using Firebase.Firestore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class FirestoreAccountManager : MonoBehaviour
{
    public static FirestoreAccountManager Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public static async Task UploadAccount(AccountData account)
    {
        Dictionary<string, object> data = new Dictionary<string, object>();

        data["id"] = account.id;
        data["username"] = account.username;
        data["passwordHash"] = account.passwordHash;
        data["recoveryCodeHash"] = account.recoveryCodeHash ?? "";
        data["recoveryCodeEnabled"] = account.recoveryCodeEnabled;
        data["accountType"] = account.accountType.ToString();
        data["assignedStudentIDs"] = account.assignedStudentIDs;

        await FirestoreManager.Instance
            .AccountsCollection()
            .Document(account.id)
            .SetAsync(data);

        Debug.Log("Uploaded Account: " + account.username);
    }

    public static async Task<bool> IsAccountUploaded(string accountID)
    {
        DocumentReference accountRef = FirestoreManager.Instance
            .AccountsCollection()
            .Document(accountID);

        DocumentSnapshot snapshot =
            await accountRef.GetSnapshotAsync(Source.Server);

        return snapshot.Exists;
    }

    public static async Task DeleteAccount(string accountID)
    {
        if (string.IsNullOrWhiteSpace(accountID))
            return;

        await FirestoreManager.Instance
            .AccountsCollection()
            .Document(accountID)
            .DeleteAsync();

        Debug.Log("Deleted account from Firestore: " + accountID);
    }

    public async Task DownloadAccounts()
    {
        string classroomID = DatabaseManager.Instance.CurrentDatabase;

        QuerySnapshot snapshot = await FirestoreManager.Instance
            .AccountsCollection()
            .GetSnapshotAsync();

        // Preserve accounts created locally but not yet uploaded.
        List<AccountData> pendingLocalAccounts = AccountManager.Instance.registry.accounts.FindAll(
            account => AccountManager.Instance.IsAccountPendingUpload(account.id));

        // Preserve unsynchronized teacher assignments.
        Dictionary<string, List<string>> pendingAssignments =
            new Dictionary<string, List<string>>();

        foreach (AccountData account in AccountManager.Instance.registry.accounts)
        {
            if (account.accountType == AccountType.Teacher && AccountManager.Instance.IsAssignmentPendingUpload(account.id))
            {
                pendingAssignments[account.id] = new List<string>(account.assignedStudentIDs);
            }
        }

        // Rebuild the registry from Firestore.
        AccountManager.Instance.registry.accounts.Clear();

        foreach(DocumentSnapshot doc in snapshot.Documents)
        {
            Dictionary<string, object> data = doc.ToDictionary();

            AccountData account = new AccountData();

            account.id = data["id"].ToString();

            account.username = data["username"].ToString();

            account.passwordHash = data["passwordHash"].ToString();

            // Load recovery information, supporting older accounts.
            account.recoveryCodeHash = data.ContainsKey("recoveryCodeHash") ? data["recoveryCodeHash"]?.ToString() ?? "" : "";

            account.recoveryCodeEnabled =
                data.ContainsKey("recoveryCodeEnabled") &&
                System.Convert.ToBoolean(data["recoveryCodeEnabled"]);

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

            // Local unsynchronized assignments take priority over
            // older Firestore assignments.
            if (pendingAssignments.TryGetValue(account.id, out List<string> localAssignments))
            {
                account.assignedStudentIDs = new List<string>(localAssignments);
            }
        }

        // Restore pending local accounts that were not in Firestore.
        foreach (AccountData pendingAccount in pendingLocalAccounts)
        {
            bool alreadyExists =
                AccountManager.Instance.registry.accounts.Exists(
                    account => account.id == pendingAccount.id
                );

            if (!alreadyExists)
            {
                AccountManager.Instance.registry.accounts.Add(pendingAccount);
            }
        }

        AccountManager.Instance.SaveAccount();

        AccountManager.Instance.RefreshUI();

        Debug.Log("Downloaded " + snapshot.Documents.Count() + " accounts.");

        await AccountManager.Instance.SynchronizePendingAccounts();
        await AccountManager.Instance.SynchronizePendingAssignments();
    }

    public static async Task UploadAssignments(
    string teacherID,
    List<string> studentIDs)
    {
        DocumentReference teacherRef =
            FirestoreManager.Instance
                .AccountsCollection()
                .Document(teacherID);

        await teacherRef.UpdateAsync(
            "assignedStudentIDs",
            studentIDs
        );

        Debug.Log("Uploaded assignments for teacher: " + teacherID);
    }

    public static async Task<bool> VerifyAssignments(
        string teacherID,
        List<string> expectedIDs)
    {
        DocumentSnapshot snapshot =
            await FirestoreManager.Instance
                .AccountsCollection()
                .Document(teacherID)
                .GetSnapshotAsync(Source.Server);

        if (!snapshot.Exists ||
            !snapshot.ContainsField("assignedStudentIDs"))
        {
            return false;
        }

        List<object> storedIDs =
            snapshot.GetValue<List<object>>("assignedStudentIDs");

        HashSet<string> actual =
            new HashSet<string>(
                storedIDs.ConvertAll(item => item.ToString())
            );

        return actual.SetEquals(expectedIDs);
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
