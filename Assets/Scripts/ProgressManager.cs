using UnityEngine;
using System.IO;
using System.Collections.Generic;

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

        savePath = Application.persistentDataPath + "/progress_" + account.id + ".json";

        Debug.Log("Active Save File: " + savePath);

        LoadProgress();
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

    public void SaveProgress()
    {
        if (currentAccount == null)
        {
            Debug.LogError("No active account selected!");
            return;
        }

        ProgressData data = new ProgressData();

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

            completedLevels =
                data.completedLevel ?? new List<string>();

            lastCampaignScene = data.lastCampaignScene;

            QuizStats.Instance.ResetStats();

            QuizStats.Instance.typeCorrect[InputType.MultipleChoice] = data.multipleChoiceCorrect;

            QuizStats.Instance.typeTotal[InputType.MultipleChoice] = data.multipleChoiceTotal;

            QuizStats.Instance.typeCorrect[InputType.Identification] = data.identificationCorrect;

            QuizStats.Instance.typeTotal[InputType.Identification] = data.identificationTotal;

            QuizStats.Instance.typeCorrect[InputType.TrueOrFalse] = data.trueFalseCorrect;

            QuizStats.Instance.typeTotal[InputType.TrueOrFalse] = data.trueFalseTotal;

            Debug.Log("Loaded Progress For: " + currentAccount.username);
        }
        else
        {
            completedLevels = new List<string>();

            lastCampaignScene = "MapMenu";

            SaveProgress();

            Debug.Log("Created New Save For: " + currentAccount.username);
        }
    }

    public void NewGame()
    {
        if (currentAccount == null)
        {
            Debug.LogError("No active account selected!");
            return;
        }

        completedLevels.Clear();

        lastCampaignScene = "MapMenu";

        SaveProgress();

        Debug.Log("New Game Started");
    }

    private void Update()
    {
        
    }
}