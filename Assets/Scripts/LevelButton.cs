using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    public LevelData levelData;
    public Button button;

    private Graphic[] graphics;
    private Color[] originalGraphicColors;

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

        graphics = GetComponentsInChildren<Graphic>(true);
        originalGraphicColors = new Color[graphics.Length];

        for (int i = 0; i < graphics.Length; i++)
        {
            originalGraphicColors[i] = graphics[i].color;

            Color color = originalGraphicColors[i];
            color.a *= isUnlocked ? 1f : 0.5f;
            graphics[i].color = color;
        }

    }

    public void OnClick()
    {
        // Restrict level selection during the tutorial.
        if (TutorialManager.Instance != null && TutorialManager.Instance.tutorialActive &&
            SceneManager.GetActiveScene().name == "LevelSelect_Tutorial")
            {
                // Only allow level selection after the introduction
                // has reached the interactive level-selection stage.
                if (TutorialManager.Instance.currentStage != TutorialStage.ExploreScrollableContent)
                {
                    Debug.Log("Tutorial: Level selection is not allowed yet.");
                    return;
                }
            }

        GameManager.Instance.selectedLevel = levelData;

        if (TutorialManager.Instance != null && TutorialManager.Instance.tutorialActive &&
            TutorialManager.Instance.currentStage ==TutorialStage.ExploreScrollableContent &&
            SceneManager.GetActiveScene().name == "LevelSelect_Tutorial")
            {
                TutorialManager.Instance.AdvanceStage(
                    TutorialStage.BattleTutorial
                );
            }

        ProgressManager.Instance.lastCampaignScene = SceneManager.GetActiveScene().name;

        Debug.Log("Last Campaign Scene: " + ProgressManager.Instance.lastCampaignScene);

        ProgressManager.Instance.SaveProgress();

        SceneManager.LoadScene("Level");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
