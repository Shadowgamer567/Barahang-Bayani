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
