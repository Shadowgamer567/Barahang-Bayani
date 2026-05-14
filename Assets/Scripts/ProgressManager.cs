using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance;

    public List<string> completedLevels = new List<string>();

    private string savePath;
    public string lastCampaignScene = "MapMenu";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            savePath = Application.persistentDataPath + "/progress_" + CurrentAccount.ActiveAccount.id + ".json" ;

            LoadProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void CompleteLevel(string levelName)
    {
        if (!completedLevels.Contains(levelName))
        {
            completedLevels.Add(levelName);
            Debug.Log("Completed: " + levelName);

            SaveProgress();
        }
    }

    public bool IsLevelCompleted(string levelName)
    {
        return completedLevels.Contains(levelName);
    }

    public void SaveProgress()
    {
        ProgressData data = new ProgressData();
        data.completedLevel = completedLevels;
        data.lastCampaignScene = lastCampaignScene;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);

        Debug.Log("Progress saved to: " + savePath);
    }

    public void LoadProgress()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            ProgressData data = JsonUtility.FromJson<ProgressData>(json);

            completedLevels = data.completedLevel ?? new List<string>();
            lastCampaignScene = data.lastCampaignScene;

            Debug.Log("Progress Loaded");
        }

        else
        {
            Debug.Log("No save file found, starting fresh");
            completedLevels = new List<string>();
        }
    }

    public void NewGame()
    {
        completedLevels.Clear();
        lastCampaignScene = "MapMenu";

        SaveProgress();

        Debug.Log("Saved Data Cleared");
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
