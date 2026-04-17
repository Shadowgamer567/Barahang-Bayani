using System;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public LevelData levelData;
    public BattleControl battleControl;

    private int battlesCompleted = 0;

    void Start()
    {
        levelData = GameManager.Instance.selectedLevel;

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
        battlesCompleted++;

        Debug.Log($"Progress: {battlesCompleted}/{levelData.battlesRequired}");

        if (battlesCompleted >= levelData.battlesRequired)
        {
            LevelComplete();
        }
    }

    void OnDestroy()
    {
        if (battleControl == null)
        {
            battleControl.OnBattleWon -= HandleBattleWon;
        }
    }

    void LevelComplete()
    {
        Debug.Log("LEVEL COMPLETE");

        ProgressManager.Instance.CompleteLevel(levelData.levelName);

        UnityEngine.SceneManagement.SceneManager.LoadScene("LevelSelect_Rizal");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
