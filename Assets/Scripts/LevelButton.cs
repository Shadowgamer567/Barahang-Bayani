using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    public LevelData levelData;
    public Button button;

    private TMP_Text[] buttonTexts;
    private Color[] originalTextColors;

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

        buttonTexts = button.GetComponentsInChildren<TMP_Text>(true);
        originalTextColors = new Color[buttonTexts.Length];
        for (int i = 0; i < buttonTexts.Length; i++)
        {
            originalTextColors[i] = buttonTexts[i].color;
        }

        bool isUnlocked = levelData.previousLevel == null;

        if (!isUnlocked && ProgressManager.Instance == null)
        {
            Debug.LogWarning("ProgressManager not found, defaulting to locked");
        }
        else if (!isUnlocked)
        {
            isUnlocked = ProgressManager.Instance.IsLevelCompleted(levelData.previousLevel.levelName);
        }

        button.interactable = isUnlocked;

        for (int i = 0; i < buttonTexts.Length; i++)
        {
            Color textColor = originalTextColors[i];
            textColor.a *= isUnlocked ? 1f : 0.5f;
            buttonTexts[i].color = textColor;
        }

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
