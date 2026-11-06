using Firebase.Auth;
using Firebase.Firestore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class FirestoreAccountManager : MonoBehaviour
{
    public static FirestoreAccountManager Instance;
    private static readonly Dictionary<string, HashSet<Task>> activeAccountOperations = new Dictionary<string, HashSet<Task>>();

    private static Task activeAccountDownload = Task.CompletedTask;

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

    public static async Task WaitForAccountDownloads()
    {
        while (!activeAccountDownload.IsCompleted)
        {
            try
            {
                await activeAccountDownload;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning(
                    "Account download failed while waiting: " + ex.Message
                );
            }
        }
    }

    private static void TrackAccountOperation(string accountID, Task operation)
    {
        if (string.IsNullOrWhiteSpace(accountID) || operation == null)
        {
            return;
        }

        if (!activeAccountOperations.TryGetValue(
            accountID,
            out HashSet<Task> operations))
        {
            operations = new HashSet<Task>();
            activeAccountOperations[accountID] = operations;
        }

        operations.Add(operation);

        _ = ObserveAccountOperation(accountID, operation);
    }

    private static async Task ObserveAccountOperation(string accountID, Task operation)
    {
        try
        {
            await operation;
        }

        catch (System.Exception ex)
        {
            Debug.LogWarning("Account operation failed for " + accountID + ": " + ex.Message);
        }

        finally
        {
            if (activeAccountOperations.TryGetValue(accountID, out HashSet<Task> operations))
            {
                operations.Remove(operation);

                if (operations.Count == 0)
                {
                    activeAccountOperations.Remove(accountID);
                }
            }
        }
    }

    public static async Task WaitForAccountOperations(
        string accountID)
    {
        if (string.IsNullOrWhiteSpace(accountID))
        {
            return;
        }

        while (activeAccountOperations.TryGetValue( accountID, out HashSet<Task> operations) && operations.Count > 0)
        {
            Task[] pending = operations.ToArray();

            try
            {
                await Task.WhenAll(pending);
            }

            catch (System.Exception ex)
            {
                Debug.LogWarning("An account operation failed while waiting: " + ex.Message);
            }
        }

        Debug.Log("All tracked account operations completed for: " + accountID);
    }

    public static async Task UploadAccount(AccountData account)
    {
        if (account == null)
        {
            return;
        }

        if (ProgressManager.Instance != null && ProgressManager.Instance.IsDeletingAccount(account.id))
        {
            Debug.LogWarning("Account upload blocked: Account deletion is in progress for " + account.id);
            return;
        }

        if (AccountManager.Instance != null && AccountManager.Instance.IsAccountPendingDeletion(account.id))
        {
            Debug.LogWarning("Account upload blocked: Account is pending deletion: " + account.id);
            return;
        }

        Dictionary<string, object> data = new Dictionary<string, object>();

        data["id"] = account.id;
        data["username"] = account.username;
        data["passwordHash"] = account.passwordHash;
        data["recoveryCodeHash"] = account.recoveryCodeHash ?? "";
        data["recoveryCodeEnabled"] = account.recoveryCodeEnabled;
        data["accountType"] = account.accountType.ToString();
        data["assignedStudentIDs"] = account.assignedStudentIDs;

        Task uploadTask = FirestoreManager.Instance
            .AccountsCollection()
            .Document(account.id)
            .SetAsync(data);

        TrackAccountOperation(account.id, uploadTask);

        await uploadTask;

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

    public static async Task DeleteAccountProgress(
    string accountID,
    string currentPlaythroughID = null,
    string previousPlaythroughID = null,
    string classroomID = null)
    {
        if (string.IsNullOrWhiteSpace(accountID))
            return;

        if (string.IsNullOrWhiteSpace(classroomID))
        {
            classroomID = DatabaseManager.Instance.CurrentDatabase;
        }

        DocumentReference progressRef =
            FirestoreManager.Instance
                .ProgressCollection(classroomID)
                .Document(accountID);

        CollectionReference playthroughsRef =
            progressRef.Collection("playthroughs");

        HashSet<string> playthroughIDs =
            new HashSet<string>();

        // Retrieve all existing parent documents.
        QuerySnapshot playthroughs =
            await playthroughsRef.GetSnapshotAsync(Source.Server);

        foreach (DocumentSnapshot playthrough in playthroughs.Documents)
        {
            playthroughIDs.Add(playthrough.Id);
        }

        // Include known playthrough IDs, even if their
        // parent documents do not exist.
        if (!string.IsNullOrWhiteSpace(currentPlaythroughID))
        {
            playthroughIDs.Add(currentPlaythroughID);
        }

        if (!string.IsNullOrWhiteSpace(previousPlaythroughID))
        {
            playthroughIDs.Add(previousPlaythroughID);
        }

        // Delete every discovered playthrough and its results.
        foreach (string playthroughID in playthroughIDs)
        {
            DocumentReference playthroughRef =
                playthroughsRef.Document(playthroughID);

            await DeletePlaythrough(playthroughRef);
        }

        // Delete the main progress document last.
        await progressRef.DeleteAsync();

        Debug.Log(
            "Deleted account progress from Firestore: " +
            accountID
        );
    }

    private static async Task DeletePlaythrough(
    DocumentReference playthroughRef)
    {
        CollectionReference resultsRef = playthroughRef.Collection("results");

        while (true)
        {
            QuerySnapshot results =
                await resultsRef
                    .Limit(100)
                    .GetSnapshotAsync(Source.Server);

            if (results.Count == 0)
                break;

            foreach (DocumentSnapshot result in results.Documents)
            {
                await result.Reference.DeleteAsync();
            }
        }

        await playthroughRef.DeleteAsync();

        Debug.Log("Deleted playthrough and its results: " + playthroughRef.Id);
    }

    public static async Task DeleteAccount(
    string accountID,
    string classroomID = null)
    {
        if (string.IsNullOrWhiteSpace(accountID))
            return;

        if (string.IsNullOrWhiteSpace(classroomID))
        {
            classroomID = DatabaseManager.Instance.CurrentDatabase;
        }

        await FirestoreManager.Instance
            .AccountsCollection(classroomID)
            .Document(accountID)
            .DeleteAsync();

        Debug.Log(
            "Deleted account from Firestore: " +
            accountID +
            " | Classroom: " + classroomID
        );
    }

    public Task DownloadAccounts()
    {
        if (ProgressManager.Instance != null &&
            ProgressManager.Instance.IsAccountDeletionInProgress)
        {
            Debug.LogWarning(
                "Account download blocked: Account deletion is in progress."
            );

            return Task.CompletedTask;
        }

        if (!activeAccountDownload.IsCompleted)
        {
            return activeAccountDownload;
        }

        activeAccountDownload = DownloadAccountsInternal();

        return activeAccountDownload;
    }

    public async Task DownloadAccountsInternal()
    {
        if (ProgressManager.Instance != null &&
            ProgressManager.Instance.IsAccountDeletionInProgress)
        {
            Debug.LogWarning(
                "Account download blocked: Account deletion is in progress."
            );

            return;
        }

        if (DatabaseManager.Instance == null || FirestoreManager.Instance == null)
        {
            Debug.LogWarning(
                "Account download skipped: Database managers are unavailable."
            );

            return;
        }

        string classroomID = DatabaseManager.Instance.CurrentDatabase;

        if (string.IsNullOrWhiteSpace(classroomID) ||
            classroomID == "DefaultDatabase")
        {
            Debug.LogWarning(
                "Account download skipped: No valid classroom is active."
            );

            return;
        }

        QuerySnapshot snapshot = await FirestoreManager.Instance
            .AccountsCollection()
            .GetSnapshotAsync();

        if (DatabaseManager.Instance == null || DatabaseManager.Instance.CurrentDatabase != classroomID)
        {
            Debug.LogWarning(
                "Account download cancelled: Active classroom changed " +
                "while downloading accounts."
            );

            return;
        }

        // Preserve every pending account, including accounts
        // belonging to other classrooms.
        List<AccountData> pendingLocalAccounts =
            AccountManager.Instance.registry.accounts.FindAll(
                account =>
                    account != null &&
                    AccountManager.Instance.IsAccountPendingUpload(account.id)
            );

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
            if (AccountManager.Instance.IsAccountPendingDeletion(doc.Id))
            {
                Debug.LogWarning(
                    "Skipping downloaded account because deletion is pending: " +
                    doc.Id
                );

                continue;
            }

            Dictionary<string, object> data = doc.ToDictionary();

            AccountData account = new AccountData();

            account.id = data["id"].ToString();

            account.classroomID = classroomID;

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
            if (pendingAccount == null)
                continue;

            if (AccountManager.Instance.IsAccountPendingDeletion(pendingAccount.id))
            {
                Debug.LogWarning(
                    "Skipping local pending account because deletion is pending: " +
                    pendingAccount.id
                );

                continue;
            }

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

    public static async Task UploadAssignments(string teacherID, List<string> studentIDs)
    {
        if (ProgressManager.Instance != null && ProgressManager.Instance.IsDeletingAccount(teacherID))
        {
            Debug.LogWarning("Teacher assignment upload blocked: Account deletion is in progress for " + teacherID);
            return;
        }

        if (AccountManager.Instance != null && AccountManager.Instance.IsAccountPendingDeletion(teacherID))
        {
            Debug.LogWarning("Teacher assignment upload blocked: Teacher account is pending deletion: " + teacherID);
            return;
        }

        DocumentReference teacherRef =
            FirestoreManager.Instance
                .AccountsCollection()
                .Document(teacherID);

        Task uploadTask = teacherRef.UpdateAsync(
            "assignedStudentIDs",
            studentIDs
);

        TrackAccountOperation(teacherID, uploadTask);

        await uploadTask;

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
