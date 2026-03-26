using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class QuizManager : MonoBehaviour
{
    public GameObject quizPanel;
    public GameObject cardPanel;
    public GameObject enemyPanel;

    public TextMeshProUGUI questionText;
    public TextMeshProUGUI[] answerText;

    private QuizQuestion currentQuestion;
    private BattleControl battleControl;

    private int pendingDamage;

    public List<QuizQuestion> questions = new List<QuizQuestion> ();

    public void StartQuiz(QuizQuestion question, int damage, BattleControl battle)
    {
        currentQuestion = question;
        pendingDamage = damage;
        battleControl = battle;

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

        if (correct && battleControl != null)
        {
            battleControl.DealDamageToAll(pendingDamage);
        }

        EndQuiz();
    }

    public void EndQuiz()
    {
        quizPanel.SetActive(false);

        enemyPanel.SetActive(true);
        cardPanel.SetActive(true);

        Time.timeScale = 1f;

    }

    void Awake()
    {
        LoadQuestions();
    }

    void LoadQuestions()
    {
        TextAsset file = Resources.Load<TextAsset>("questions");

        if (file == null)
        {
            Debug.LogError("questions.txt not FOUND");
            return;
        }

        Debug.Log("File loaded successfully");
        Debug.Log(file.text);
        string[] lines = file.text.Split('\n');

        for(int i = 0; i < lines.Length;)
        {
        
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                i++;
                continue;
            }

            QuizQuestion q = new QuizQuestion();

            q.Question = lines[i].Trim();
            int dotIndex = q.Question.IndexOf(".");

            if (dotIndex != -1)
            {
                q.Question = q.Question.Substring(dotIndex + 1).Trim();
            }
            
            
            q.Answer = new string[4];
            q.correctIndex = 0;

            for(int j = 0; j < 4; j++)
            {
                string line = lines[i + 1 + j].Trim();

                string answerText = line.Substring(3).Trim();

                if (answerText.Contains("\"C\""))
                {
                    q.correctIndex = j;
                    answerText = answerText.Replace("\"C\"", "").Trim();
                }

                q.Answer[j] = answerText;
            }

            questions.Add(q);
            i += 5;
        }
    }

    public QuizQuestion GetRandomQuestions()
    {
        return questions[Random.Range(0, questions.Count)];
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
