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


    public string lastCampaignScene = "MapMenu";

    private string savePath;

    private AccountData currentAccount;

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

        savePath = Application.persistentDataPath + "/progress_" + account.id + ".json";

       Debug.Log("Active Save File: " + savePath);

        completedLevels = new List<string>();
        lastCampaignScene = "MapMenu";
        tutorialCompleted = false;
        playthroughId = "";
        pendingQuizResults = new List<QuizResultData>();

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

        data.pendingQuizResults = new List<QuizResultData>(pendingQuizResults);

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

        try
        {
            ProgressData cloudProgress =
                await FirestoreProgressManager.DownloadProgress(accountId);

            if (currentAccount == null || currentAccount.id != accountId)
            {
                Debug.Log("Discarding progress for an inactive account.");
                return false;
            }

            if (cloudProgress != null)
            {
                ProgressData localProgress = ReadLocalProgress();

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

        if (currentAccount == null || currentAccount.id != accountId)
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

        pendingQuizResults = data.pendingQuizResults ?? new List<QuizResultData>();

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

        //Generate New ID for this playthrough
        playthroughId = System.Guid.NewGuid().ToString();

        // Reset completed levels and tutorial progress.
        completedLevels.Clear();
        tutorialCompleted = false;
        lastCampaignScene = "MapMenu";

        // Reset account quiz metrics for the new playthrough.
        multipleChoiceCorrect = 0;
        multipleChoiceTotal = 0;

        identificationCorrect = 0;
        identificationTotal = 0;

        trueFalseCorrect = 0;
        trueFalseTotal = 0;

        // Reset current-level quiz statistics.
        QuizStats.Instance?.ResetStats();

        // Prevents bleeing of old Quiz Data into new Playthroughs
        pendingQuizResults.Clear();

        // Save the new playthrough's starting progress.
        SaveProgress(uploadToFirestore);

        Debug.Log("New Game Started. Progress and quiz metrics reset.");
    }


    public async Task<bool> UploadNewGameProgress()
    {
        if (currentAccount == null)
        {
            Debug.LogError("Cannot upload New Game: No active account.");
            return false;
        }

        string accountId = currentAccount.id;
        string expectedPlaythroughId = playthroughId;

        ProgressData data = ReadLocalProgress();

        if (data == null ||
            data.playthroughId != expectedPlaythroughId)
        {
            Debug.LogError(
                "Cannot upload New Game: Local playthrough does not match.");
            return false;
        }

        try
        {
            await FirestoreProgressManager.UploadProgressForNewGame(accountId, data);

            Debug.Log("New Game progress uploaded for playthrough: " + expectedPlaythroughId);

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to upload New Game progress: " + e.Message);

            return false;
        }
    }


    public async void SyncProgressFromFirestore()
    {
        if (currentAccount == null)
        {
            Debug.LogError("Cannot sync progress: No active account.");

            return;
        }

        try
        {
            ProgressData cloudProgress =
                await FirestoreProgressManager.DownloadProgress(
                    currentAccount.id);

            if (cloudProgress == null)
            {
                Debug.Log("No cloud progress found. " + "Keeping local progress.");

                return;
            }

            ApplyProgress(cloudProgress);
            SaveProgress(false);

            Debug.Log("Progress successfully synced from Firestore.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to download progress: " + e.Message);

            Debug.Log("Keeping local progress.");
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


    private void Update()
    {
        
    }
}