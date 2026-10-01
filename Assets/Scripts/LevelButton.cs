using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    public LevelData levelData;
    public Button button;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InitializeButton();
    }

    void InitializeButton()
    {
        if (button == null || levelData == null)
        {
            Debug.LogError("LevelButton missing reference");
            return;
        }

        if (levelData.previousLevel == null)
        {
            button.interactable = true;
            return;
        }

        if (ProgressManager.Instance == null)
        {
            Debug.LogWarning("ProgressManager not found, defaulting to locked");
            button.interactable = false;
            return;
        }

        button.interactable = ProgressManager.Instance.IsLevelCompleted(levelData.previousLevel.levelName);
    }

    public void OnClick()
    {
        GameManager.Instance.selectedLevel = levelData;

        ProgressManager.Instance.lastCampaignScene = "LevelSelect_Rizal";

        ProgressManager.Instance.SaveProgress();
        SceneManager.LoadScene("Level");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
