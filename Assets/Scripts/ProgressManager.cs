using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.SocialPlatforms;

public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance;

    public List<string> completedLevels = new();
    public bool tutorialCompleted = false;

    [Header("Playthrough Tracking")]
    public string playthroughId = "";

    [Header("Lifetime Quiz Statistics")]
    public int multipleChoiceCorrect = 0;
    public int multipleChoiceTotal = 0;

    public int identificationCorrect = 0;
    public int identificationTotal = 0;

    public int trueFalseCorrect = 0;
    public int trueFalseTotal = 0;

    [Header("Pending Quiz Synchronization")]
    public List<QuizResultData> pendingQuizResults = new();
    public bool quizResultLedgerInitialized = false;

    [Header("New Game Synchronization")]
    public bool pendingNewGameReset = false;
    public string previousPlaythroughId = "";

    public string lastCampaignScene = "MapMenu";

    private string savePath;
    private AccountData currentAccount;
    private bool isSyncingQuizResults = false;
    private bool isReconcilingProgress = false;
    private bool isResettingPlaythrough = false;
    private int progressGeneration = 0;


    private void Start()
    {
        
    }

    void Awake()
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

    public void SetActiveAccount(AccountData account)
    {
        progressGeneration++;

        currentAccount = account;

        // Clear temporary statistics from the previous account/level.
        QuizStats.Instance?.ResetStats();

        // Clear lifetime statistics from the previous account.
        // The selected account's saved values will be loaded afterward.
        multipleChoiceCorrect = 0;
        multipleChoiceTotal = 0;

        identificationCorrect = 0;
        identificationTotal = 0;

        trueFalseCorrect = 0;
        trueFalseTotal = 0;

        pendingNewGameReset = false;
        previousPlaythroughId = "";

        savePath = Application.persistentDataPath + "/progress_" + account.id + ".json";

        Debug.Log("Active Save File: " + savePath);

        completedLevels = new List<string>();
        lastCampaignScene = "MapMenu";
        tutorialCompleted = false;
        playthroughId = "";
        pendingQuizResults = new List<QuizResultData>();
        quizResultLedgerInitialized = false;

    }

    public void CompleteLevel(string levelName)
    {
        if (!completedLevels.Contains(levelName))
        {
            completedLevels.Add(levelName);

            SaveProgress();

            Debug.Log("Completed: " + levelName);
        }
    }

    public bool IsLevelCompleted(string levelName)
    {
        return completedLevels.Contains(levelName);
    }


    public void RegisterLifetimeQuestion(InputType type)
    {
        if (currentAccount == null)
            return;

        switch (type)
        {
            case InputType.MultipleChoice:
                multipleChoiceTotal++;
                break;

            case InputType.Identification:
                identificationTotal++;
                break;

            case InputType.TrueOrFalse:
                trueFalseTotal++;
                break;
        }
    }

    public void RegisterLifetimeCorrect(InputType type)
    {
        if (currentAccount == null)
            return;

        switch (type)
        {
            case InputType.MultipleChoice:
                multipleChoiceCorrect++;
                break;

            case InputType.Identification:
                identificationCorrect++;
                break;

            case InputType.TrueOrFalse:
                trueFalseCorrect++;
                break;
        }
    }


    public void SaveProgress(bool uploadToFirestore = true)
    {
        if (currentAccount == null)
        {
            Debug.LogError("No active account selected!");
            return;
        }

        ProgressData data = new ProgressData();
        
        data.tutorialCompleted = tutorialCompleted;

        data.username = currentAccount.username;
        data.accountType = currentAccount.accountType.ToString();

        data.completedLevel = completedLevels;
        data.lastCampaignScene = lastCampaignScene;

        data.playthroughId = playthroughId;

        data.pendingNewGameReset = pendingNewGameReset;
        data.previousPlaythroughId = previousPlaythroughId;

        data.pendingQuizResults = new List<QuizResultData>(pendingQuizResults);

        data.quizResultLedgerInitialized = quizResultLedgerInitialized;

        data.multipleChoiceCorrect = multipleChoiceCorrect;
        data.multipleChoiceTotal = multipleChoiceTotal;

        data.identificationCorrect = identificationCorrect;
        data.identificationTotal = identificationTotal;

        data.trueFalseCorrect = trueFalseCorrect;
        data.trueFalseTotal = trueFalseTotal;

        string json = JsonUtility.ToJson(data, true);

        File.WriteAllText(savePath, json);

        if (uploadToFirestore)
        {
            Debug.Log("Local progress saved. Automatic Firestore upload " + "is temporarily disabled until safe synchronization is implemented.");
        }

        Debug.Log("Saved Progress For: " + currentAccount.username);
    }

    public void LoadProgress()
    {
        if (currentAccount == null)
        {
            Debug.LogError("No active account selected!");
            return;
        }

        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);

            ProgressData data =
                JsonUtility.FromJson<ProgressData>(json);

            ApplyProgress(data);

            Debug.Log("Loaded Progress For: " + currentAccount.username);
        }
        else
        {
            completedLevels = new List<string>();

            lastCampaignScene = "MapMenu";

            SaveProgress(false);

            Debug.Log("Created New Save For: " + currentAccount.username);
        }
    }

    public async Task<bool> LoadProgressForActiveAccount()
    {
        if (currentAccount == null)
        {
            Debug.LogError("No active account selected!");
            return false;
        }

        string accountId = currentAccount.id;

        int operationGeneration = progressGeneration;

        try
        {
            ProgressData cloudProgress =
                await FirestoreProgressManager.DownloadProgress(accountId);

            if (currentAccount == null ||
                currentAccount.id != accountId ||
                progressGeneration != operationGeneration)
                {
                    Debug.LogWarning("Progress loading cancelled: Account session changed.");
                    return false;
                }


            ProgressData localProgress = ReadLocalProgress();

            if (localProgress != null &&
                localProgress.pendingNewGameReset &&
                !string.IsNullOrEmpty(localProgress.playthroughId))
            {
                Debug.LogWarning(
                    "Local New Game reset is awaiting Firestore confirmation. " +
                    "Preserving local progress."
                );

                ApplyProgress(localProgress);
                return true;
            }


            if (cloudProgress != null)
            {
                //ProgressData localProgress = ReadLocalProgress();

                if (localProgress != null &&
                    !string.IsNullOrEmpty(localProgress.playthroughId) &&
                    localProgress.playthroughId == cloudProgress.playthroughId &&
                    localProgress.pendingQuizResults != null &&
                    localProgress.pendingQuizResults.Count > 0)
                {
                    Debug.LogWarning(
                        "Local unsynchronized quiz results detected. " +
                        "Keeping local progress until safe reconciliation is available."
                    );

                    ApplyProgress(localProgress);
                    return true;
                }

                ApplyProgress(cloudProgress);

                RestorePendingQuizResults(localProgress, cloudProgress);

                SaveProgress(false);

                Debug.Log("Loaded progress from Firestore and restored local " + "pending results for: " + currentAccount.username);

                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to load progress from Firestore: " + e.Message);
        }

        if (currentAccount == null ||
            currentAccount.id != accountId ||
            progressGeneration != operationGeneration)
            {
                return false;
            }

        LoadProgress();
        return true;
    }

    private void ApplyProgress(ProgressData data)
    {
        completedLevels = data.completedLevel ?? new List<string>();
        lastCampaignScene = string.IsNullOrEmpty(data.lastCampaignScene)
            ? "MapMenu"
            : data.lastCampaignScene;

        playthroughId = data.playthroughId;

        pendingNewGameReset = data.pendingNewGameReset;
        previousPlaythroughId = data.previousPlaythroughId;

        pendingQuizResults = data.pendingQuizResults ?? new List<QuizResultData>();

        quizResultLedgerInitialized = data.quizResultLedgerInitialized;

        multipleChoiceCorrect = data.multipleChoiceCorrect;
        multipleChoiceTotal = data.multipleChoiceTotal;

        identificationCorrect = data.identificationCorrect;
        identificationTotal = data.identificationTotal;

        trueFalseCorrect = data.trueFalseCorrect;
        trueFalseTotal = data.trueFalseTotal;

        tutorialCompleted = data.tutorialCompleted;
    }


    public void NewGame(bool uploadToFirestore = true)
    {
        if (currentAccount == null)
        {
            Debug.LogError("No active account selected!");
            return;
        }

        progressGeneration++;

        //Remember which playthrough is being replaced
        string accountId = currentAccount.id;
        previousPlaythroughId = playthroughId;

        //Generate New ID for playthrough
        playthroughId = System.Guid.NewGuid().ToString();
        pendingNewGameReset = true;
        string newPlaythroughId = playthroughId;

        // Reset completed levels and tutorial progress.
        completedLevels.Clear();
        tutorialCompleted = false;
        lastCampaignScene = "MapMenu";

        // Reset cumulative quiz metrics.
        multipleChoiceCorrect = 0;
        multipleChoiceTotal = 0;

        identificationCorrect = 0;
        identificationTotal = 0;

        trueFalseCorrect = 0;
        trueFalseTotal = 0;

        // Clear results of previous playthrough
        pendingQuizResults.Clear();

        quizResultLedgerInitialized = true;

        // Reset current-level quiz statistics.
        QuizStats.Instance?.ResetStats();

        // Save the new playthrough's starting progress.
        SaveProgress(false);

        if (uploadToFirestore)
        {
            _ = UploadNewGameProgress(accountId, previousPlaythroughId, newPlaythroughId, progressGeneration);
        }

        Debug.Log("New Game started. New playthrough ID: " + newPlaythroughId);
    }

    private void ConfirmNewGameReset(
        string accountId,
        string expectedPlaythroughId,
        int operationGeneration)
    {
        if (currentAccount == null ||
            currentAccount.id != accountId ||
            playthroughId != expectedPlaythroughId ||
            progressGeneration != operationGeneration)
        {
            Debug.LogWarning(
                "New Game confirmation ignored: Account or playthrough changed."
            );
            return;
        }

        pendingNewGameReset = false;
        previousPlaythroughId = "";

        SaveProgress(false);

        Debug.Log(
            "New Game reset confirmed and saved locally."
        );
    }

    private async Task UploadNewGameProgress(
        string accountId,
        string oldPlaythroughId,
        string newPlaythroughId,
        int operationGeneration)
    {
        try
        {
            ProgressData newProgress = ReadLocalProgress();

            if (newProgress == null ||
                newProgress.playthroughId != newPlaythroughId)
            {
                Debug.LogWarning(
                    "New Game upload cancelled: Local playthrough changed."
                );
                return;
            }

            bool success =
                await FirestoreProgressManager.ResetPlaythrough(
                    accountId,
                    newProgress,
                    oldPlaythroughId
                );

            if (success)
            {
                Debug.Log(
                    "New Game synchronized with Firestore: " +
                    newPlaythroughId
                );

                ConfirmNewGameReset(
                    accountId,
                    newPlaythroughId,
                    operationGeneration
                );
            }
            else
            {
                Debug.LogWarning(
                    "New Game reset was not confirmed by Firestore. " +
                    "It remains pending locally."
                );
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(
                "New Game reset upload failed. It remains pending locally: " +
                e.Message
            );
        }
    }

    public async Task<bool> RetryPendingNewGameReset()
    {
        if (isResettingPlaythrough)
        {
            Debug.LogWarning(
                "New Game reset is already being synchronized."
            );
            return false;
        }

        isResettingPlaythrough = true;

        if (currentAccount == null ||
            !pendingNewGameReset ||
            string.IsNullOrEmpty(playthroughId))
        {
            return false;
        }

        string accountId = currentAccount.id;
        string newPlaythroughId = playthroughId;
        string oldPlaythroughId = previousPlaythroughId;
        int operationGeneration = progressGeneration;

        try
        {
            ProgressData data = ReadLocalProgress();

            if (data == null ||
                !data.pendingNewGameReset ||
                data.playthroughId != newPlaythroughId)
            {
                Debug.LogWarning(
                    "New Game retry cancelled: Local reset data is invalid."
                );
                return false;
            }

            bool success =
                await FirestoreProgressManager.ResetPlaythrough(
                    accountId,
                    data,
                    oldPlaythroughId
                );

            if (!success)
            {
                Debug.LogWarning(
                    "New Game retry rejected. Local reset remains pending."
                );
                return false;
            }

            if (currentAccount == null ||
                currentAccount.id != accountId ||
                playthroughId != newPlaythroughId ||
                progressGeneration != operationGeneration)
            {
                Debug.LogWarning(
                    "New Game retry completed, but the active session changed."
                );
                return false;
            }

            ConfirmNewGameReset(
                accountId,
                newPlaythroughId,
                operationGeneration
            );

            Debug.Log("Pending New Game reset successfully recovered.");
            return true;
        }

        catch (System.Exception e)
        {
            Debug.LogWarning(
                "New Game retry failed: " + e.Message
            );

            return false;
        }

        finally
        {
            isResettingPlaythrough = false;
        }
    }

    public async void SyncProgressFromFirestore()
    {
        if (currentAccount == null)
        {
            Debug.LogWarning("Cannot synchronize progress: No active account.");
            return;
        }

        try
        {
            bool loaded = await LoadProgressForActiveAccount();

            if (!loaded)
            {
                Debug.LogWarning("Progress synchronization was not completed.");
                return;
            }

            Debug.Log("Progress loaded using the account-safe loading method.");
        }

        catch (System.Exception e)
        {
            Debug.LogError("Progress synchronization failed: " + e.Message);
        }
    }


    public void RecordQuizResult(InputType type, bool isCorrect)
    {
        if (currentAccount == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(playthroughId))
        {
            Debug.LogWarning("Quiz Result not recorded: No Active Playthrough ID");
            return;
        }

        QuizResultData result = new QuizResultData()
        {
            resultId = System.Guid.NewGuid().ToString(),
            playthroughId = playthroughId,
            questionType = type.ToString(),
            isCorrect = isCorrect
        };

        pendingQuizResults.Add(result);
        SaveProgress(false);

        Debug.Log($"Pending quiz result recorded: {type}, Correct: {isCorrect}");
    }

    private ProgressData ReadLocalProgress()
    {
        if (string.IsNullOrEmpty(savePath) || !File.Exists(savePath))
            return null;

        try
        {
            string json = File.ReadAllText(savePath);
            return JsonUtility.FromJson<ProgressData>(json);
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Failed to read local progress: " + e.Message);
            return null;
        }
    }


    private void RestorePendingQuizResults(
        ProgressData localProgress,
        ProgressData cloudProgress)
    {
        if (localProgress == null || cloudProgress == null)
            return;

        // Only restore results from the same playthrough.
        if (string.IsNullOrEmpty(localProgress.playthroughId) ||
            localProgress.playthroughId != cloudProgress.playthroughId)
        {
            Debug.Log("Local pending results belong to a different playthrough.");
            return;
        }

        if (localProgress.pendingQuizResults == null)
            return;

        HashSet<string> existingIds = new HashSet<string>();

        foreach (QuizResultData result in pendingQuizResults)
        {
            if (result != null && !string.IsNullOrEmpty(result.resultId))
                existingIds.Add(result.resultId);
        }

        foreach (QuizResultData result in localProgress.pendingQuizResults)
        {
            if (result == null ||
                string.IsNullOrEmpty(result.resultId) ||
                result.playthroughId != cloudProgress.playthroughId)
                continue;

            if (existingIds.Add(result.resultId))
            {
                pendingQuizResults.Add(result);
            }
        }

        Debug.Log(
            "Pending quiz results after local restoration: " + pendingQuizResults.Count);
    }

    private List<QuizResultData> GetValidPendingResults(
        ProgressData localProgress,
        string expectedPlaythroughId)
    {
        List<QuizResultData> validResults =
            new List<QuizResultData>();

        if (localProgress == null ||
            localProgress.pendingQuizResults == null ||
            localProgress.playthroughId != expectedPlaythroughId)
        {
            return validResults;
        }

        HashSet<string> seenIds = new HashSet<string>();

        foreach (QuizResultData result in localProgress.pendingQuizResults)
        {
            if (result == null ||
                string.IsNullOrWhiteSpace(result.resultId) ||
                result.playthroughId != expectedPlaythroughId)
            {
                continue;
            }

            if (seenIds.Add(result.resultId))
            {
                validResults.Add(result);
            }
        }

        return validResults;
    }

    private void AddPendingResultsToCounters(
        List<QuizResultData> results)
    {
        foreach (QuizResultData result in results)
        {
            if (result == null)
                continue;

            switch (result.questionType)
            {
                case "MultipleChoice":
                    multipleChoiceTotal++;

                    if (result.isCorrect)
                        multipleChoiceCorrect++;

                    break;

                case "Identification":
                    identificationTotal++;

                    if (result.isCorrect)
                        identificationCorrect++;

                    break;

                case "TrueOrFalse":
                    trueFalseTotal++;

                    if (result.isCorrect)
                        trueFalseCorrect++;

                    break;
            }
        }
    }

    private void ApplyCloudQuizCounters(ProgressData cloudProgress)
    {
        if (cloudProgress == null)
            return;

        multipleChoiceCorrect = cloudProgress.multipleChoiceCorrect;
        multipleChoiceTotal = cloudProgress.multipleChoiceTotal;

        identificationCorrect = cloudProgress.identificationCorrect;
        identificationTotal = cloudProgress.identificationTotal;

        trueFalseCorrect = cloudProgress.trueFalseCorrect;
        trueFalseTotal = cloudProgress.trueFalseTotal;
    }


    private async Task<List<QuizResultData>> GetUnuploadedPendingResults(
        string accountId,
        string activePlaythroughId,
        List<QuizResultData> validPendingResults)
    {
        HashSet<string> uploadedIds =
            await FirestoreProgressManager.GetUploadedQuizResultIds(
                accountId,
                activePlaythroughId,
                validPendingResults
            );

        List<QuizResultData> unuploadedResults =
            new List<QuizResultData>();

        foreach (QuizResultData result in validPendingResults)
        {
            if (!uploadedIds.Contains(result.resultId))
            {
                unuploadedResults.Add(result);
            }
        }

        return unuploadedResults;
    }


    public async Task<bool> ReconcileQuizProgress()
    {
        if (isReconcilingProgress || isSyncingQuizResults)
        {
            Debug.LogWarning("Quiz reconciliation skipped: Another operation is running.");
            return false;
        }

        if (currentAccount == null ||string.IsNullOrEmpty(playthroughId) || !quizResultLedgerInitialized)
        {
            Debug.LogWarning("Quiz reconciliation skipped: No initialized playthrough.");
            return false;
        }

        isReconcilingProgress = true;

        string accountId = currentAccount.id;
        string activePlaythroughId = playthroughId;
        int operationGeneration = progressGeneration;

        try
        {
            // Take a snapshot of the current pending results.
            ProgressData localSnapshot = new ProgressData
            {
                playthroughId = activePlaythroughId,
                pendingQuizResults =
                    new List<QuizResultData>(pendingQuizResults)
            };

            List<QuizResultData> validPending =
                GetValidPendingResults(
                    localSnapshot,
                    activePlaythroughId
                );

            // Read cloud counters and result IDs consistently.
            QuizReconciliationSnapshot snapshot =
                await FirestoreProgressManager
                    .GetQuizReconciliationSnapshot(
                        accountId,
                        activePlaythroughId,
                        validPending
                    );

            // Reject results if the active account or playthrough changed.
            if(currentAccount == null ||
            currentAccount.id != accountId ||
            playthroughId != activePlaythroughId ||
            progressGeneration != operationGeneration)
{
                Debug.LogWarning(
                    "Reconciliation cancelled: Account or playthrough session changed."
                );
                return false;
            }

            if (snapshot == null ||
                snapshot.cloudProgress == null ||
                !snapshot.cloudProgress.quizResultLedgerInitialized ||
                snapshot.cloudProgress.playthroughId != activePlaythroughId)
            {
                Debug.LogWarning("Reconciliation skipped: Invalid cloud snapshot.");
                return false;
            }

            // Find results that have not reached Firestore.
            List<QuizResultData> unuploadedResults = new List<QuizResultData>();

            foreach (QuizResultData result in validPending)
            {
                if (!snapshot.uploadedResultIds.Contains(result.resultId))
                {
                    unuploadedResults.Add(result);
                }
            }

            // Identify results recorded while awaiting Firestore.
            HashSet<string> snapshotIds = new HashSet<string>();

            foreach (QuizResultData result in validPending)
            {
                snapshotIds.Add(result.resultId);
            }

            List<QuizResultData> newResults = new List<QuizResultData>();

            HashSet<string> newResultIds = new HashSet<string>();

            foreach (QuizResultData result in pendingQuizResults)
            {
                if (result == null ||
                    string.IsNullOrEmpty(result.resultId) ||
                    result.playthroughId != activePlaythroughId ||
                    snapshotIds.Contains(result.resultId) ||
                    !newResultIds.Add(result.resultId))
                {
                    continue;
                }

                newResults.Add(result);
            }

            // Begin with authoritative Firestore counters.
            ApplyCloudQuizCounters(snapshot.cloudProgress);

            // Add only results not yet reflected in Firestore.
            AddPendingResultsToCounters(unuploadedResults);

            // Preserve answers recorded during reconciliation.
            AddPendingResultsToCounters(newResults);

            // Retain results that still need synchronization.
            pendingQuizResults =
                new List<QuizResultData>(unuploadedResults);

            pendingQuizResults.AddRange(newResults);

            SaveProgress(false);

            Debug.Log("Quiz reconciliation completed. Pending results: " + pendingQuizResults.Count);

            return true;
        }

        catch (System.Exception e)
        {
            Debug.LogError("Quiz reconciliation failed: " + e.Message);

            return false;
        }

        finally
        {
            isReconcilingProgress = false;
        }
    }


    public async Task SyncPendingQuizResults()
    {
        if (currentAccount == null)
        {
            Debug.LogWarning("Cannot sync: No active account.");
            return;
        }

        if (!quizResultLedgerInitialized ||
            string.IsNullOrEmpty(playthroughId))
        {
            Debug.LogWarning("Cannot sync: Quiz result ledger is not initialized.");
            return;
        }

        if (isSyncingQuizResults || isReconcilingProgress)
        {
            Debug.LogWarning("Quiz synchronization skipped: Another operation is running.");
            return;
        }

        isSyncingQuizResults = true;

        try
        {
            string accountId = currentAccount.id;
            string activePlaythroughId = playthroughId;
            int operationGeneration = progressGeneration;

            // Copy the list so gameplay can add new results during synchronization.
            List<QuizResultData> resultsToSync = new List<QuizResultData>(pendingQuizResults);

            int synchronizedCount = 0;

            foreach (QuizResultData result in resultsToSync)
            {
                if (currentAccount == null ||
                    currentAccount.id != accountId ||
                    playthroughId != activePlaythroughId || 
                    progressGeneration != operationGeneration)
                {
                    Debug.LogWarning("Synchronization stopped: Active account or playthrough changed.");
                    return;
                }

                if (result == null ||
                    result.playthroughId != activePlaythroughId)
                {
                    continue;
                }

                try
                {
                    bool success =
                        await FirestoreProgressManager.UploadQuizResult(
                            accountId, result
                        );

                    if (!success)
                    {
                        Debug.LogWarning("Quiz result was not synchronized: " + result.resultId
                        );
                        continue;
                    }

                    // Confirm the active account/playthrough is still unchanged.
                    if (currentAccount == null ||
                        currentAccount.id != accountId ||
                        playthroughId != activePlaythroughId || 
                        progressGeneration != operationGeneration)
                    {
                        return;
                    }

                    // Remove only the result Firestore confirmed.
                    pendingQuizResults.RemoveAll(
                        pending => pending != null &&
                                   pending.resultId == result.resultId
                    );

                    // Persist the updated pending list locally.
                    SaveProgress(false);

                    synchronizedCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError(
                        "Failed to synchronize quiz result " +
                        result.resultId + ": " + e.Message
                    );
                }
            }

            Debug.Log(
                "Quiz synchronization finished. Results confirmed: " +
                synchronizedCount +
                " | Remaining pending: " +
                pendingQuizResults.Count
            );
        }

        finally
        {
            isSyncingQuizResults = false;
        }
    }

    //To be used in Testing
    [ContextMenu("Test Sync Pending Quiz Results")]
    private void TestSyncPendingQuizResults()
    {
        _ = SyncPendingQuizResults();
    }


    //To be used in Testing
    [ContextMenu("Test Duplicate Quiz Result")]
    private async void TestDuplicateQuizResult()
    {
        if (currentAccount == null ||
            !quizResultLedgerInitialized ||
            string.IsNullOrEmpty(playthroughId))
        {
            Debug.LogWarning(
                "Duplicate test cancelled: No valid active playthrough."
            );
            return;
        }

        string accountId = currentAccount.id;
        string testPlaythroughId = playthroughId;

        // Create one test result with a fixed ID for both uploads.
        QuizResultData testResult = new QuizResultData()
        {
            resultId = System.Guid.NewGuid().ToString(),
            playthroughId = testPlaythroughId,
            questionType = InputType.TrueOrFalse.ToString(),
            isCorrect = true
        };

        try
        {
            Debug.Log("Duplicate test: Uploading result for the first time.");

            bool firstUpload =
                await FirestoreProgressManager.UploadQuizResult(
                    accountId, testResult
                );

            if (!firstUpload)
            {
                Debug.LogError("Duplicate test: First upload failed.");
                return;
            }

            Debug.Log("Duplicate test: Uploading the SAME result ID again.");

            bool secondUpload =
                await FirestoreProgressManager.UploadQuizResult(
                    accountId, testResult
                );

            if (!secondUpload)
            {
                Debug.LogError("Duplicate test: Second upload failed.");
                return;
            }

            Debug.Log(
                "Duplicate test completed. Both uploads were acknowledged. " +
                "Check Firestore to confirm the counters increased only once."
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError(
                "Duplicate test failed: " + e.Message
            );
        }
    }

    //To be used in testing
    [ContextMenu("Test Reconcile Quiz Progress")]
    private async void TestReconcileQuizProgress()
    {
        bool success = await ReconcileQuizProgress();

        Debug.Log(
            "Manual quiz reconciliation test: " +
            (success ? "SUCCESS" : "FAILED")
        );
    }

    [ContextMenu("Test Retry Pending New Game Reset")]
    private async void TestRetryPendingNewGameReset()
    {
        bool success = await RetryPendingNewGameReset();

        Debug.Log(
            "Manual New Game retry test: " +
            (success ? "SUCCESS" : "NOT COMPLETED")
        );
    }

    private void Update()
    {
        
    }
}