using System;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public LevelData levelData;
    public BattleControl battleControl;
    public LevelData debugLevelData;

    public GameObject mainUI;
    public GameObject victoryUI;
    private CutsceneManager cutsceneManager;

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

        BackgroundLoop bg = FindFirstObjectByType<BackgroundLoop>();

        if (bg != null)
        {
            bg.currentLevel = levelData.levelIndex;
            bg.currentCampaign = levelData.campaign;

            Debug.Log("Background set to level: " + bg.currentLevel + " Campaign set to: " + bg.currentCampaign.name);

            bg.InitializeTiles();
        }

        else
        {
            Debug.LogWarning("BackgroundLoop not found");
        }

        GroundLoop gl = FindFirstObjectByType<GroundLoop>();

        if (gl != null)
        {
            Material groundMat = levelData.campaign.GetGroundMaterial(levelData.levelIndex);

            if (groundMat != null)
            {
                gl.ApplyGroundMaterial(groundMat);
            }
        }


        battleControl = FindFirstObjectByType<BattleControl>();

        if (battleControl == null)
        {
            Debug.LogError("BattleControl not assigned!");
            return;
        }

        battleControl.Initialize(levelData);

        battleControl.OnBattleWon += HandleBattleWon;

        Debug.Log("Loaded Level: " + levelData.levelName);
        Debug.Log("Battles Required: " + levelData.battlesRequired);

        QuizStats.Instance?.ResetStats();

        cutsceneManager = FindFirstObjectByType<CutsceneManager>();

        TriggerCutscene(CutsceneTriggerType.Start, 0, () => 
        { 
            battleControl.StartBattleSequence();
        });
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

        else
        {
            TriggerCutscene(CutsceneTriggerType.AfterBattle, battlesCompleted, () =>
            {
                battleControl.StartBattleSequence();
            });
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

        TriggerCutscene(CutsceneTriggerType.End, 0, ShowVictoryScreen);
    }

    public void ShowVictoryScreen()
    {
        Time.timeScale = 0f;

        battleControl = FindFirstObjectByType<BattleControl>();

        if (battleControl != null)
        {
            battleControl.isLevelFinished = true;
            battleControl.StopAllCoroutines();
        }

        if (mainUI != null)
        {
            mainUI.SetActive(false);
        }

        if (victoryUI != null)
        {
            victoryUI.SetActive(true);
        }
    }

    void TriggerCutscene(CutsceneTriggerType type, int battleIndex, System.Action onFinished = null)
    {
        if (levelData.Cutscenes == null || cutsceneManager == null)
        {
            onFinished?.Invoke();
            return;
        }

        foreach (var trigger in levelData.Cutscenes)
        {
            if (trigger.triggerType == type)
            {
                if (type == CutsceneTriggerType.AfterBattle && trigger.battleIndex != battleIndex)
                {
                    continue;
                }

                cutsceneManager.OnCutsceneFinished = () =>
                {
                    onFinished?.Invoke();
                };

                cutsceneManager.PlayCutscene(trigger.Cutscene);

                return;
            }
        }

        onFinished?.Invoke();
    }

    void StartFirstBattle()
    {
        Debug.Log("Starting First Battle");

        if (battleControl != null)
        {
            battleControl.StartBattleSequence();
        }
    }
    void ResumeNextBattle()
    {
        Debug.Log("Cutscene finished => starting next battle");

        if (battleControl != null)
        {
            battleControl.StartNextBattleManually();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
