using TMPro;
using UnityEngine;

public class QuizManager : MonoBehaviour
{
    public GameObject quizPanel;
    public GameObject cardPanel;
    public GameObject enemyPanel;

    public TextMeshProUGUI questionText;
    public TextMeshProUGUI[] answerText;

    private QuizQuestion currentQuestion;

    public void StartQuiz(QuizQuestion question)
    {
        currentQuestion = question;

        quizPanel.SetActive(true);

        enemyPanel.SetActive(false);
        cardPanel.SetActive(false);

        Time.timeScale = 0f;

        questionText.text = question.Question;
        
        for(int i = 0; i < answerText.Length; i++)
        {
            answerText[i].text = question.Answer[i];
        }
    }

    public void Answers(int index)
    {
        bool correct = index == currentQuestion.correctIndex;

        Debug.Log(correct ? "Correct!" : "Incorrect");

        EndQuiz();
    }

    public void EndQuiz()
    {
        quizPanel.SetActive(false);

        enemyPanel.SetActive(true);
        cardPanel.SetActive(true);

        Time.timeScale = 1f;

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
