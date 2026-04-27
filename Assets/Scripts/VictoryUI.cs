using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryUI : MonoBehaviour
{
    public TextMeshProUGUI multiplechoiceText;
    public TextMeshProUGUI identificationText;
    public TextMeshProUGUI trueorfalseText;
    public TextMeshProUGUI totalText;

    private void OnEnable()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        var stats = QuizStats.Instance;

        multiplechoiceText.text = $"{stats.GetTypeCorrect(InputType.MultipleChoice)} / {stats.GetTotalType(InputType.MultipleChoice)}";

        identificationText.text = $"{stats.GetTypeCorrect(InputType.Identification)} / {stats.GetTotalType(InputType.Identification)}";

        trueorfalseText.text = $"{stats.GetTypeCorrect(InputType.TrueOrFalse)} / {stats.GetTotalType(InputType.TrueOrFalse)}";

        totalText.text = $"{stats.totalCorrect} / {stats.totalQuestions}";
    }

    public void OnReturn()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("LevelSelect_Rizal");
    }

    public void OnRetry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Level");
    }

    public void OnContinue()
    {
        Time.timeScale = 1f;

        var currentLevel = GameManager.Instance.selectedLevel;

        if (currentLevel != null && currentLevel.nextLevel != null)
        {
            GameManager.Instance.selectedLevel = currentLevel.nextLevel;

            SceneManager.LoadScene("Level");
        }

        else
        {
            Debug.Log("No next level, returning to level select");
            SceneManager.LoadScene("LevelSelect_Rizal");
        }
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
