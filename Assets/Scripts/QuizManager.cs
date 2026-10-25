using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class QuizManager : MonoBehaviour
{
    public event Action OnQuizOpened;
    public event Action OnQuizInfoOpened;
    public event Action<bool> OnAnswerEvaluated;

    public GameObject quizPanel;
    public GameObject cardPanel;
    public GameObject enemyPanel;
    public GameObject heroPanel;
    public GameObject imageObject;
    public UnityEngine.UI.Image questionImage;
    public AudioSource audioSource;
    public GameObject audioButton;
    public GameObject submitButton;
    public GameObject endButton;
    public GameObject inputPanel;
    public GameObject truthPanel;
    public GameObject falsePanel;

    [Header("Info Panel")]
    public GameObject quizInfoPanel;

    public TextMeshProUGUI infoName;
    public Image infoImage;
    public TextMeshProUGUI infoText;

    public Image[] answerBackgrounds;
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;
    public Color normalColor = Color.white;

    public TextMeshProUGUI questionText;
    public TextMeshProUGUI[] answerText;
    public TMP_InputField inputField;

    private QuizQuestion currentQuestion;
    private BattleControl battleControl;

    private bool waitingForClick = false;
    private bool answerSubmitted = false;
    public bool blockInfoPanelDismissal = false;
    private int pendingDamage;
    private CardType pendingQuizCard;

    public List<QuizQuestion> questions = new List<QuizQuestion>();
    public Dictionary<string, QuizInfoData> infoDatabase = new Dictionary<string, QuizInfoData>();

    public void StartQuiz(QuizQuestion question, CardType card, BattleControl battle)
    {
        currentQuestion = question;
        answerSubmitted = false;
        pendingQuizCard = card;
        pendingDamage = card.damage;
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
        inputPanel.SetActive(false);
        truthPanel.SetActive(false);
        falsePanel.SetActive(false);

        if (audioSource != null)
        {
            audioSource.Stop();
        }

        questionText.text = question.Question;

        for (int i = 0; i < answerText.Length; i++)
        {
            if (i < question.Answer.Length)
            {
                answerText[i].text = question.Answer[i];
            }

            else
            {
                answerText[i].text = "";
            }
        }

        quizInfoPanel.SetActive(false);

        foreach (Image bg in answerBackgrounds)
        {
            bg.color = normalColor;
        }

        Image inputImage = inputPanel.GetComponent<Image>();

        if (inputImage != null)
        {
            inputImage.color = normalColor;
        }

        Image trueImage = truthPanel.GetComponent<Image>();
        Image falseImage = falsePanel.GetComponent<Image>();

        if (trueImage != null)
        {
            trueImage.color = normalColor;
        }

        if (falseImage != null)
        {
            falseImage.color = normalColor;
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

                if (sprite != null)
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

                if (clip != null)
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

        switch (question.inputType)
        {
            case InputType.MultipleChoice:
                inputPanel.SetActive(false);

                for (int i = 0; i < answerText.Length; i++)
                {
                    answerText[i].transform.parent.gameObject.SetActive(true);
                }
                break;

            case InputType.Identification:
                inputPanel.SetActive(true);

                inputField.text = "";
                inputField.ActivateInputField();

                for (int i = 0; i < answerText.Length; i++)
                {
                    answerText[i].transform.parent.gameObject.SetActive(false);
                }
                break;

            case InputType.TrueOrFalse:
                inputPanel.SetActive(false);

                for (int i = 0; i < answerText.Length; i++)
                {
                    answerText[i].transform.parent.gameObject.SetActive(false);
                }

                truthPanel.SetActive(true);
                falsePanel.SetActive(true);
                break;
        }

        QuizStats.Instance.RegisterQuestion(question.inputType);

        OnQuizOpened?.Invoke();
    }

    public void Answers(int index)
    {
        if (answerSubmitted || currentQuestion == null)
        {
            return;
        }

        answerSubmitted = true;

        bool correct = index == currentQuestion.correctIndex;

        OnAnswerEvaluated?.Invoke(correct);
        Debug.Log(correct ? "Correct!" : "Incorrect");

        if (correct)
        {
            QuizStats.Instance.RegisterCorrect(currentQuestion.inputType);
        }

        ProgressManager.Instance.RecordQuizResult(currentQuestion.inputType, correct);

        StartCoroutine(ShowAnswerRoutine(correct, index));
    }

    public void SubmitIdentification()
    {
        if (answerSubmitted || currentQuestion == null)
            return;

        answerSubmitted = true;

        string userAnswer = inputField.text.Trim().ToLower();
        string correct = currentQuestion.correctAnswer.Trim().ToLower();

        bool isCorrect = userAnswer == correct;

        OnAnswerEvaluated?.Invoke(isCorrect);
        Debug.Log(isCorrect ? "Correct" : "Incorrect");
        if (isCorrect)
        {
            QuizStats.Instance.RegisterCorrect(currentQuestion.inputType);
        }

        ProgressManager.Instance.RecordQuizResult(currentQuestion.inputType, isCorrect);

        StartCoroutine(ShowAnswerRoutine(isCorrect));
    }

    public void AnswerTrueorFalse(bool playerAnswer)
    {
        if (answerSubmitted || currentQuestion == null)
            return;

        answerSubmitted = true;

        bool correct = playerAnswer == currentQuestion.correctBool;

        OnAnswerEvaluated?.Invoke(correct);
        Debug.Log(correct ? "Correct" : "Incorrect");

        if (correct)
        {
            QuizStats.Instance.RegisterCorrect(currentQuestion.inputType);
        }

        ProgressManager.Instance.RecordQuizResult(currentQuestion.inputType, correct);
        
        StartCoroutine(ShowAnswerRoutine(correct));
    }

    public void EndQuiz()
    {
        ProgressManager.Instance.SaveProgress();

        quizPanel.SetActive(false);

        enemyPanel.SetActive(true);
        cardPanel.SetActive(true);
        heroPanel.SetActive(true);
        endButton.SetActive(true);

        Time.timeScale = 1f;

    }
    void Awake()
    {
        if (audioSource != null)
        {
            SoundVolumeControl.RegisterSource(audioSource, AudioVolumeGroup.Sfx, audioSource.volume);
        }

        LoadQuestions();
        Debug.Log("Loaded Quiz Info");
        LoadQuizInfo();
    }

    void LoadQuestions()
    {
        TextAsset file = Resources.Load<TextAsset>(QuizMakerManager.SelectedQuizName);

        if (file == null)
        {
            Debug.LogError("questions.txt not FOUND");
            return;
        }

        Debug.Log("File loaded successfully");
        Debug.Log(file.text);
        string[] lines = file.text.Split('\n');

        for (int i = 0; i < lines.Length;)
        {

            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                i++;
                continue;
            }

            QuizQuestion q = new QuizQuestion();

            // READ QUESTION ID
            q.questionID = lines[i]
                .Replace("ID:", "")
                .Trim();

            i++;

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

            string inputLine = lines[i + 2].Trim();

            if (inputLine.StartsWith("INPUT:MULTIPLE_CHOICE"))
            {
                q.inputType = InputType.MultipleChoice;
            }

            else if (inputLine.StartsWith("INPUT:IDENTIFICATION"))
            {
                q.inputType = InputType.Identification;
            }

            else if (inputLine.StartsWith("INPUT:TRUE_OR_FALSE"))
            {
                q.inputType = InputType.TrueOrFalse;
            }

            if (q.inputType == InputType.TrueOrFalse)
            {
                q.Answer = new string[0];

                string answerLine = lines[i + 3].Trim();

                if (answerLine.StartsWith("ANSWER:TRUE"))
                {
                    q.correctBool = true;
                }

                else if (answerLine.StartsWith("ANSWER:FALSE"))
                {
                    q.correctBool = false;
                }

                q.Answer = new string[0];

                questions.Add(q);

                i += 4;
                continue;
            }

            else
            {
                q.Answer = new string[4];
                q.correctIndex = 0;

                for (int j = 0; j < 4; j++)
                {
                    string line = lines[i + 3 + j];

                    bool isCorrect = line.Contains("\"C\"");

                    string answerText = line.Substring(3).Replace("\"C\"", "").Trim();

                    if (isCorrect)
                    {
                        q.correctIndex = j;

                        q.correctAnswer = answerText;
                    }

                    q.Answer[j] = answerText;
                }
                questions.Add(q);
                i += 7;
            }
        }
    }

    void LoadQuizInfo()
    {
        TextAsset file = Resources.Load<TextAsset>("quizinfo");
        if (file == null) return;

        string[] lines = file.text.Split('\n');
        QuizInfoData current = null;

        foreach (string rawline in lines)
        {
            string line = rawline.Trim();

            // Don't skip empty lines if we are currently recording a description
            if (string.IsNullOrWhiteSpace(line))
            {
                if (current != null) current.description += "\n";
                continue;
            }

            if (line.StartsWith("[ID:"))
            {
                current = new QuizInfoData();
                current.id = line.Replace("[ID:", "").Replace("]", "").Trim();
                current.description = "";
            }
            else if (line.StartsWith("TITLE:"))
            {
                current.title = line.Substring(6).Trim();
            }
            else if (line.StartsWith("IMAGE:"))
            {
                current.imagePath = line.Substring(6).Trim();
            }
            else if (line.StartsWith("DESC:"))
            {
                // Just a marker, we can ignore the "DESC:" line itself
                continue;
            }
            else if (line.StartsWith("[END]")) // Removed colon to match your file
            {
                if (current != null)
                {
                    infoDatabase[current.id] = current;
                    Debug.Log("Successfully Loaded Info for: " + current.id);
                }
            }
            else
            {
                if (current != null)
                {
                    current.description += line + " ";
                }
            }
        }
    }

    public QuizQuestion GetRandomQuestions()
    {
        if (questions.Count == 0)
        {
            Debug.LogError("No Questions Loaded");
            return null;
        }

        return questions[Random.Range(0, questions.Count)];
    }

    public void PlayAudio()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Stop();
            audioSource.Play();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (waitingForClick && !blockInfoPanelDismissal && Mouse.current.leftButton.wasPressedThisFrame)
        {
            waitingForClick = false;
        }
    }

    IEnumerator ShowAnswerRoutine(bool correct, int selectedIndex = -1)
    {
        // ===== MULTIPLE CHOICE =====
        if (currentQuestion.inputType == InputType.MultipleChoice)
        {
            // reset colors first
            for (int i = 0; i < answerBackgrounds.Length; i++)
            {
                answerBackgrounds[i].color = normalColor;
            }

            if (correct)
            {
                answerBackgrounds[selectedIndex].color = correctColor;
            }
            else
            {
                answerBackgrounds[selectedIndex].color = wrongColor;
                answerBackgrounds[currentQuestion.correctIndex].color = correctColor;
            }
        }

        // ===== IDENTIFICATION =====
        else if (currentQuestion.inputType == InputType.Identification)
        {
            Image inputImage = inputPanel.GetComponent<Image>();

            if (inputImage != null)
            {
                inputImage.color = correct ? correctColor : wrongColor;
            }
        }

        // ===== TRUE OR FALSE =====
        else if (currentQuestion.inputType == InputType.TrueOrFalse)
        {
            Image trueImage = truthPanel.GetComponent<Image>();
            Image falseImage = falsePanel.GetComponent<Image>();

            if (currentQuestion.correctBool)
            {
                trueImage.color = correctColor;
                falseImage.color = wrongColor;
            }
            else
            {
                falseImage.color = correctColor;
                trueImage.color = wrongColor;
            }
        }

        // wait before info panel
        yield return new WaitForSecondsRealtime(2f);

        // ===== SHOW INFO PANEL =====
        quizInfoPanel.SetActive(true);

        OnQuizInfoOpened?.Invoke();

        if (infoDatabase.ContainsKey(currentQuestion.questionID))
        {
            QuizInfoData info = infoDatabase[currentQuestion.questionID];

            infoName.text = info.title;
            infoText.text = info.description;

            Sprite infoSprite = Resources.Load<Sprite>("Images/" + info.imagePath);

            if (infoSprite != null)
            {
                infoImage.sprite = infoSprite;
                infoImage.gameObject.SetActive(true);
            }
            else
            {
                infoImage.gameObject.SetActive(false);
            }
        }

        // wait for LEFT CLICK
        waitingForClick = true;

        yield return new WaitUntil(() => waitingForClick == false);

        quizInfoPanel.SetActive(false);

        // APPLY DAMAGE AFTER INFO PANEL
        // Close the quiz and restore normal gameplay first
        EndQuiz();

        // Apply the card effect only if the answer was correct
        if (battleControl != null && pendingQuizCard != null)
        {
            if (correct)
            {
                // Correct answer: execute the quiz card normally.
                battleControl.ResolveQuizCard(pendingQuizCard);
            }
            else
            {
                // Incorrect answer: enemies take a penalty turn.
                // This does not refill AP or cards.
                battleControl.StartQuizPenaltyTurn();
            }
        }

        pendingQuizCard = null;
    }
}
