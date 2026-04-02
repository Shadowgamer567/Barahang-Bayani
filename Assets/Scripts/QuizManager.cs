using TMPro;
using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.UI;

public class QuizManager : MonoBehaviour
{
    public GameObject quizPanel;
    public GameObject cardPanel;
    public GameObject enemyPanel;
    public GameObject heroPanel;
    public GameObject imageObject;
    public UnityEngine.UI.Image questionImage;
    public AudioSource audioSource;
    public GameObject audioButton;
    public GameObject endButton;

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

        endButton.SetActive(false);
        enemyPanel.SetActive(false);
        cardPanel.SetActive(false);
        heroPanel.SetActive(false);

        Time.timeScale = 0f;

        questionText.gameObject.SetActive(true);
        questionImage.gameObject.SetActive(false);
        audioButton.SetActive(false);
        imageObject.SetActive(false);

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        questionText.text = question.Question;
        
        for(int i = 0; i < answerText.Length; i++)
        {
            answerText[i].text = question.Answer[i];
        }

        switch (question.type)
        {
            case QuizType.Text:
                break;

            case QuizType.Image:
                imageObject.SetActive(true);
                questionImage.gameObject.SetActive(true);

                Sprite sprite = Resources.Load<Sprite>("Images/" + question.imagePath);
                Debug.Log("Question Type" + question.type);
                Debug.Log("Image Question Tirggered");

                if(sprite != null)
                {
                    questionImage.sprite = sprite;
                }

                else
                {
                    Debug.LogError("Image Not Found " + question.imagePath);
                }
                    break;

            case QuizType.Audio:
                audioButton.SetActive(true);
                AudioClip clip = Resources.Load<AudioClip>("Audio/" + question.audioPath);

                if(clip != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                }

                else
                {
                    Debug.LogError("Audio Not Found " + question.audioPath);
                }
                    break;
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
        heroPanel.SetActive(true);
        endButton.SetActive(true);

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

            string typeline = lines[i + 1].Trim();

            if (typeline.StartsWith("TYPE:TEXT"))
            {
                q.type = QuizType.Text;
            }

            else if (typeline.StartsWith("TYPE:IMAGE"))
            {
                q.type = QuizType.Image;
                q.imagePath = typeline.Split(":")[2].Trim();
            }

            else if (typeline.StartsWith("TYPE:AUDIO"))
            {
                q.type = QuizType.Audio;
                q.audioPath = typeline.Split(":")[2].Trim();
            }

            q.Answer = new string[4];
            q.correctIndex = 0;

            for(int j = 0; j < 4; j++)
            {
                string line = lines[i + 2 + j].Trim();

                string answerText = line.Substring(3).Trim();

                if (answerText.Contains("\"C\""))
                {
                    q.correctIndex = j;
                    answerText = answerText.Replace("\"C\"", "").Trim();
                }

                q.Answer[j] = answerText;
            }

            questions.Add(q);
            i += 6;
        }
    }

    public QuizQuestion GetRandomQuestions()
    {
        if(questions.Count == 0)
        {
            Debug.LogError("No Questions Loaded");
            return null;
        }

        return questions[Random.Range(0, questions.Count)];
    }

    public void PlayAudio()
    {
        if(audioSource != null && audioSource.clip != null)
        {
            audioSource.Stop();
            audioSource.Play();
        }
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
