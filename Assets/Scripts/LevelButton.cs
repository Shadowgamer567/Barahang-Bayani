using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelButton : MonoBehaviour
{
    public LevelData levelData;
    public Button button;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void OnEnable()
    {
        if (levelData.previousLevel == null)
        {
            button.interactable = true;
        }

        else
        {
            button.interactable = ProgressManager.Instance.IsLevelCompleted(levelData.previousLevel.levelName);
        }
    }

    public void OnClick()
    {
        GameManager.Instance.selectedLevel = levelData;
        SceneManager.LoadScene("Level");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
