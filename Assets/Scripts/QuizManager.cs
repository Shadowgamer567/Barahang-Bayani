using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Networking;
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

                string selectedQuiz = QuizMakerManager.SelectedQuizFilePath;

                if (!string.IsNullOrEmpty(selectedQuiz) &&
                    question.imagePath.StartsWith("Images/", StringComparison.OrdinalIgnoreCase))
                {
                    // Load image from the selected Quiz Package.
                    StartCoroutine(LoadPackageImage(question.imagePath));
                }
                else
                {
                    // Existing campaign / legacy image loading.
                    Sprite sprite = Resources.Load<Sprite>("Images/" + question.imagePath);

                    if (sprite != null)
                    {
                        questionImage.sprite = sprite;
                    }
                    else
                    {
                        Debug.LogError("Image Not Found: " + question.imagePath);
                    }
                }

                break;


            case QuizType.Audio:
                audioButton.SetActive(true);

                // Clear the previous question's audio.
                audioSource.Stop();
                audioSource.clip = null;

                string selectedAudioQuiz = QuizMakerManager.SelectedQuizFilePath;

                if (!string.IsNullOrEmpty(selectedAudioQuiz) &&
                    !string.IsNullOrEmpty(question.audioPath) &&
                    question.audioPath.StartsWith(
                        "Sounds/",
                        StringComparison.OrdinalIgnoreCase))
                {
                    // Load audio from the selected Quiz Package.
                    StartCoroutine(LoadPackageAudio(question.audioPath));
                }
                else
                {
                    // Preserve existing campaign audio loading.
                    AudioClip clip = Resources.Load<AudioClip>(
                        "Audio/" + question.audioPath
                    );

                    if (clip != null)
                    {
                        audioSource.clip = clip;
                        audioSource.Play();
                    }
                    else
                    {
                        Debug.LogError("Audio Not Found: " + question.audioPath);
                    }
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

        Debug.Log("Loaded Quiz Info");
        LoadQuizInfo();
    }

    public void LoadQuestions(BackgroundSet campaign)
    {
        questions.Clear();

        string quizText;

        // Custom quiz selected in Quiz Maker overrides campaign files.
        string customPath = QuizMakerManager.SelectedQuizFilePath;

        if (!string.IsNullOrEmpty(customPath))
        {
            if (!File.Exists(customPath))
            {
                Debug.LogError("Selected custom quiz file not found: " + customPath);
                return;
            }

            quizText = File.ReadAllText(customPath);

            Debug.Log("Loading custom quiz file: " + customPath);
        }
        else
        {
            // No custom selection: use the current campaign's file.
            if (campaign == null || campaign.campaignQuizFile == null)
            {
                Debug.LogError("No quiz file assigned to the current campaign.");
                return;
            }

            quizText = campaign.campaignQuizFile.text;

            Debug.Log("Loading campaign quiz: " + campaign.campaignName);
        }

        string[] lines = quizText.Split('\n');

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
                    string line = lines[i + 3 + j].Trim();

                    bool isCorrect = line.Contains("\"C\"");

                    // Remove the answer label (A., B., C., D.)
                    // without assuming the line contains an answer.
                    int answerDotIndex = line.IndexOf('.');

                    string answerText = answerDotIndex >= 0
                        ? line.Substring(answerDotIndex + 1).Trim()
                        : line;

                    answerText = answerText.Replace("\"C\"", "").Trim();

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
        infoDatabase.Clear();

        string infoContent;
        string customQuizPath = QuizMakerManager.SelectedQuizFilePath;

        if (!string.IsNullOrEmpty(customQuizPath))
        {
            // Custom Quiz Package: locate its companion Info file.
            string folder = Path.GetDirectoryName(customQuizPath);
            string quizName = Path.GetFileNameWithoutExtension(customQuizPath);

            string infoPath = Path.Combine(folder, quizName + "_info.txt");

            if (!File.Exists(infoPath))
            {
                Debug.LogWarning("Quiz Info file not found: " + infoPath);
                return;
            }

            infoContent = File.ReadAllText(infoPath);

            Debug.Log("Loading custom Quiz Info: " + infoPath);
        }
        else
        {
            // Default campaign Quiz Info.
            TextAsset file = Resources.Load<TextAsset>("quizinfo");

            if (file == null)
            {
                Debug.LogWarning("Default Quiz Info file not found.");
                return;
            }

            infoContent = file.text;

            Debug.Log("Loading default Quiz Info.");
        }

        string[] lines = infoContent.Split('\n');
        QuizInfoData current = null;
        bool readingDescription = false;

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (line.StartsWith("[ID:") && line.EndsWith("]"))
            {
                current = new QuizInfoData
                {
                    id = line.Substring(4, line.Length - 5).Trim(),
                    title = "",
                    imagePath = "",
                    description = ""
                };

                readingDescription = false;
            }
            else if (line == "[END]")
            {
                if (current != null && !string.IsNullOrEmpty(current.id))
                {
                    current.description = current.description.TrimEnd();
                    infoDatabase[current.id] = current;

                    Debug.Log("Successfully Loaded Info for: " + current.id);
                }

                current = null;
                readingDescription = false;
            }
            else if (current != null)
            {
                if (line.StartsWith("TITLE:") && !readingDescription)
                {
                    current.title = line.Substring(6).Trim();
                }
                else if (line.StartsWith("IMAGE:") && !readingDescription)
                {
                    current.imagePath = line.Substring(6).Trim();
                }
                else if (line == "DESC:")
                {
                    readingDescription = true;
                }
                else if (readingDescription)
                {
                    if (current.description.Length > 0)
                    {
                        current.description += "\n";
                    }

                    current.description += rawLine.TrimEnd('\r');
                }
            }
        }

        Debug.Log("Total Quiz Info entries loaded: " + infoDatabase.Count);
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
        infoName.text = "";
        infoText.text = "";
        infoImage.sprite = null;
        infoImage.gameObject.SetActive(false);

        quizInfoPanel.SetActive(true);

        OnQuizInfoOpened?.Invoke();

        if (infoDatabase.ContainsKey(currentQuestion.questionID))
        {
            QuizInfoData info = infoDatabase[currentQuestion.questionID];

            infoName.text = info.title;
            infoText.text = info.description;

            // Clear the previous Quiz Info image.
            infoImage.sprite = null;
            infoImage.gameObject.SetActive(false);

            if (!string.IsNullOrEmpty(info.imagePath))
            {
                string selectedQuiz = QuizMakerManager.SelectedQuizFilePath;

                if (!string.IsNullOrEmpty(selectedQuiz) &&
                    info.imagePath.StartsWith("Images/", StringComparison.OrdinalIgnoreCase))
                {
                    // Load the image from the selected Quiz Package.
                    yield return StartCoroutine(LoadPackageInfoImage(info.imagePath));
                }
                else
                {
                    // Preserve the original Resources image loading.
                    Sprite infoSprite = Resources.Load<Sprite>("Images/" + info.imagePath);

                    if (infoSprite != null)
                    {
                        infoImage.sprite = infoSprite;
                        infoImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        Debug.LogWarning("Quiz Info image not found: " + info.imagePath);
                    }
                }
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

    private IEnumerator LoadPackageImage(string imagePath)
    {
        string quizPath = QuizMakerManager.SelectedQuizFilePath;

        if (string.IsNullOrEmpty(quizPath))
            yield break;

        string packageFolder = Path.GetDirectoryName(quizPath);

        string fullImagePath = Path.GetFullPath(
            Path.Combine(packageFolder, imagePath)
        );

        // Prevent media paths from escaping the Quiz Package.
        string fullPackageFolder = Path.GetFullPath(packageFolder);
        string packagePrefix = fullPackageFolder.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        ) + Path.DirectorySeparatorChar;

        if (!fullImagePath.StartsWith(
            packagePrefix,
            StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("Invalid Quiz Package image path: " + imagePath);
            yield break;
        }

        if (!File.Exists(fullImagePath))
        {
            Debug.LogError("Quiz Package image not found: " + fullImagePath);
            yield break;
        }

        string fileUrl = new Uri(fullImagePath).AbsoluteUri;

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(fileUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load quiz image: " + request.error);
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            questionImage.sprite = sprite;

            Debug.Log("Loaded Quiz Package image: " + imagePath);
        }
    }


    private IEnumerator LoadPackageInfoImage(string imagePath)
    {
        string quizPath = QuizMakerManager.SelectedQuizFilePath;

        if (string.IsNullOrEmpty(quizPath))
            yield break;

        string packageFolder = Path.GetFullPath(
            Path.GetDirectoryName(quizPath)
        );

        string fullImagePath = Path.GetFullPath(
            Path.Combine(packageFolder, imagePath)
        );

        // Prevent paths from escaping the Quiz Package folder.
        string packagePrefix = packageFolder.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        ) + Path.DirectorySeparatorChar;

        if (!fullImagePath.StartsWith(
            packagePrefix,
            StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("Invalid Quiz Info image path: " + imagePath);
            yield break;
        }

        if (!File.Exists(fullImagePath))
        {
            Debug.LogWarning("Quiz Info image not found: " + fullImagePath);
            yield break;
        }

        string fileUrl = new Uri(fullImagePath).AbsoluteUri;

        using (UnityWebRequest request =
            UnityWebRequestTexture.GetTexture(fileUrl))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "Failed to load Quiz Info image: " + request.error
                );
                yield break;
            }

            Texture2D texture = DownloadHandlerTexture.GetContent(request);

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            infoImage.sprite = sprite;
            infoImage.gameObject.SetActive(true);

            Debug.Log("Loaded Quiz Info image: " + imagePath);
        }
    }


    private IEnumerator LoadPackageAudio(string audioPath)
    {
        string quizPath = QuizMakerManager.SelectedQuizFilePath;

        if (string.IsNullOrEmpty(quizPath))
            yield break;

        string packageFolder = Path.GetDirectoryName(quizPath);

        string fullAudioPath = Path.GetFullPath(
            Path.Combine(packageFolder, audioPath)
        );

        // Prevent paths from escaping the Quiz Package.
        string fullPackageFolder = Path.GetFullPath(packageFolder);

        string packagePrefix = fullPackageFolder.TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar
        ) + Path.DirectorySeparatorChar;

        if (!fullAudioPath.StartsWith(
            packagePrefix,
            StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError("Invalid Quiz Package audio path: " + audioPath);
            yield break;
        }

        if (!File.Exists(fullAudioPath))
        {
            Debug.LogError("Quiz Package audio not found: " + fullAudioPath);
            yield break;
        }

        // Determine audio format from file extension.
        string extension = Path.GetExtension(fullAudioPath).ToLowerInvariant();

        AudioType audioType;

        switch (extension)
        {
            case ".wav":
                audioType = AudioType.WAV;
                break;

            case ".mp3":
                audioType = AudioType.MPEG;
                break;

            case ".ogg":
                audioType = AudioType.OGGVORBIS;
                break;

            default:
                Debug.LogError("Unsupported audio format: " + extension);
                yield break;
        }

        string fileUrl = new Uri(fullAudioPath).AbsoluteUri;

        using (UnityWebRequest request =
            UnityWebRequestMultimedia.GetAudioClip(fileUrl, audioType))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(
                    "Failed to load Quiz Package audio: " + request.error
                );
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(request);

            if (clip == null)
            {
                Debug.LogError("Loaded audio clip is null: " + audioPath);
                yield break;
            }

            // Avoid playing audio if the question was closed or replaced
            // while the file was loading.
            if (!quizPanel.activeInHierarchy ||
                currentQuestion == null ||
                currentQuestion.type != QuizType.Audio ||
                currentQuestion.audioPath != audioPath)
            {
                Destroy(clip);
                yield break;
            }

            audioSource.clip = clip;
            audioSource.Play();

            Debug.Log("Loaded Quiz Package audio: " + audioPath);
        }
    }

}
