using System;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public LevelData levelData;
    public BattleControl battleControl;
    public LevelData debugLevelData;

    private int battlesCompleted = 0;

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.selectedLevel != null)
        {
            levelData = GameManager.Instance.selectedLevel;
        }
        else
        {
            Debug.LogWarning("Using DEBUG LevelData");
            levelData = debugLevelData;
        }

        if (levelData == null)
        {
            Debug.LogError("No LevelData assigned!");
            return;
        }

        battleControl = FindFirstObjectByType<BattleControl>();

        if (battleControl == null)
        {
            Debug.LogError("BattleControl not assigned!");
            return;
        }

        battleControl.OnBattleWon += HandleBattleWon;

        Debug.Log("Loaded Level: " + levelData.levelName);
        Debug.Log("Battles Required: " + levelData.battlesRequired);
    }

    void HandleBattleWon()
    {
        Debug.Log("HandleBattleWon Called");

        if (levelData == null)
        {
            Debug.LogError("levelData is Null");
            return;
        }
        battlesCompleted++;

        Debug.Log($"Progress: {battlesCompleted}/{levelData.battlesRequired}");

        if (battlesCompleted >= levelData.battlesRequired)
        {
            LevelComplete();
        }
    }

    void OnDestroy()
    {
        if (battleControl != null)
        {
            battleControl.OnBattleWon -= HandleBattleWon;
        }
    }

    void LevelComplete()
    {
        Debug.Log("LEVEL COMPLETE");

        if (ProgressManager.Instance == null)
        {
            Debug.LogError("ProgressManager is Null");
            return;
        }

        if (levelData == null)
        {
            Debug.LogError("levelData is Null in LevelComplete");
            return;
        }

        ProgressManager.Instance.CompleteLevel(levelData.levelName);

        UnityEngine.SceneManagement.SceneManager.LoadScene("LevelSelect_Rizal");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
