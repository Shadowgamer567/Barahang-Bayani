using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance;

    public List<string> completedLevels = new();

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

       savePath = Application.persistentDataPath +
           "/progress_" + account.id + ".json";

       Debug.Log("Active Save File: " + savePath);

    completedLevels = new List<string>();
    lastCampaignScene = "MapMenu";
    QuizStats.Instance.ResetStats();

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

    public void SaveProgress(bool uploadToFirestore = true)
    {
        if (currentAccount == null)
        {
            Debug.LogError("No active account selected!");
            return;
        }

        ProgressData data = new ProgressData();

        data.username = currentAccount.username;
        data.accountType = currentAccount.accountType.ToString();

        data.completedLevel = completedLevels;
        data.lastCampaignScene = lastCampaignScene;

        data.multipleChoiceCorrect = QuizStats.Instance.GetTypeCorrect(InputType.MultipleChoice);

        data.multipleChoiceTotal = QuizStats.Instance.GetTotalType(InputType.MultipleChoice);

        data.identificationCorrect = QuizStats.Instance.GetTypeCorrect(InputType.Identification);

        data.identificationTotal = QuizStats.Instance.GetTotalType(InputType.Identification);

        data.trueFalseCorrect = QuizStats.Instance.GetTypeCorrect(InputType.TrueOrFalse);

        data.trueFalseTotal = QuizStats.Instance.GetTotalType(InputType.TrueOrFalse);

        string json = JsonUtility.ToJson(data, true);

        File.WriteAllText(savePath, json);

        if (uploadToFirestore)
        {
            _ = FirestoreProgressManager.UploadProgress(currentAccount.id, data);
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
                ApplyProgress(cloudProgress);
                SaveProgress(false);

                Debug.Log("Loaded progress from Firestore for: " + currentAccount.username);
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

        QuizStats.Instance.ResetStats();
        QuizStats.Instance.typeCorrect[InputType.MultipleChoice] = data.multipleChoiceCorrect;
        QuizStats.Instance.typeTotal[InputType.MultipleChoice] = data.multipleChoiceTotal;
        QuizStats.Instance.typeCorrect[InputType.Identification] = data.identificationCorrect;
        QuizStats.Instance.typeTotal[InputType.Identification] = data.identificationTotal;
        QuizStats.Instance.typeCorrect[InputType.TrueOrFalse] = data.trueFalseCorrect;
        QuizStats.Instance.typeTotal[InputType.TrueOrFalse] = data.trueFalseTotal;
    }

    public void NewGame(bool uploadToFirestore = true)
    {
        if (currentAccount == null)
        {
            Debug.LogError("No active account selected!");
            return;
        }

        completedLevels.Clear();

        lastCampaignScene = "MapMenu";

        SaveProgress(uploadToFirestore);

        Debug.Log("New Game Started");
    }

    public async void SyncProgressFromFirestore()
{
    if (currentAccount == null)
    {
        Debug.LogError(
            "Cannot sync progress: No active account."
        );

        return;
    }

    try
    {
        ProgressData cloudProgress =
            await FirestoreProgressManager.DownloadProgress(
                currentAccount.id
            );

        if (cloudProgress == null)
        {
            Debug.Log(
                "No cloud progress found. " +
                "Keeping local progress."
            );

            return;
        }

        ApplyProgress(cloudProgress);
        SaveProgress(false);

        Debug.Log(
            "Progress successfully synced from Firestore."
        );
    }
    catch (System.Exception e)
    {
        Debug.LogError(
            "Failed to download progress: " +
            e.Message
        );

        Debug.Log(
            "Keeping local progress."
        );
    }
}

    private void Update()
    {
        
    }
}