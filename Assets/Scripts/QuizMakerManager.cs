using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SFB;

public class QuizMakerManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject QuizMakerPanel;
    public GameObject backButton;
    public GameObject accountButton;
    public GameObject mainMenuImage;

    [Header("Quiz Selection")]
    public GameObject currentQuizPanel;
    public TMP_Text currentQuizText;

    [Header("New Quiz")]
    public GameObject nameQuizPanel;
    public TMP_InputField quizNameInputField;

    [Header("Question")]
    public TMP_InputField quizQuestionInputField;

    public TMP_Dropdown questionTypeDropdown;
    public TMP_Dropdown inputTypeDropdown;

    [Header("Question Type Panels")]
    public GameObject identificationPanel;
    public GameObject multipleChoicePanel;
    public GameObject trueFalsePanel;

    [Header("Media Button")]
    public GameObject imageButton;
    public GameObject audioButton;

    [Header("Multiple Choice")]
    public TMP_InputField AnswerA;
    public TMP_InputField AnswerB;
    public TMP_InputField AnswerC;
    public TMP_InputField AnswerD;

    [Header("Identification")]
    public TMP_InputField identificationAnswer;

    [Header("Multiple Choice Correct Answer Buttons")]
    public Image[] correctAnswerButtons;

    [Header("True or False Buttons")]
    public Image trueButtonImage;
    public Image falseButtonImage;

    [Header("Remove Question Panel")]
    public GameObject removeQuestionPanel;

    public Transform questionGridContent;

    public GameObject multipleChoiceQuestionTemplate;
    public GameObject trueFalseQuestionTemplate;

    private System.Collections.Generic.List<int> selectedQuestion = new System.Collections.Generic.List<int>();

    private bool trueFalseAnswer = true;
    private int correctAnswerIndex = 0;

    private string selectedImagePath;
    private string selectedAudioPath;

    private string selectedQuizPath;

    public static string SelectedQuizName = "questions";
    public string SelectedQuizPath => selectedQuizPath;

    public void OpenQuizMakerPanel()
    {
        QuizMakerPanel.SetActive(true);
        accountButton.SetActive(false);
        backButton.SetActive(true);
        mainMenuImage.SetActive(false);
    }

    public void OpenNewQuizPanel()
    {
        nameQuizPanel.SetActive(true);
        quizNameInputField.text = "";
    }

    public void CancelNewQuiz()
    {
        nameQuizPanel.SetActive(false);
    }
    
    public void CancelRemoveQuestion()
    {
        selectedQuestion.Clear();

        removeQuestionPanel.SetActive(false);
    }

    public void CloseQuizMakerPanel()
    {
        QuizMakerPanel.SetActive(false);
        accountButton.SetActive(true);
        backButton.SetActive(false);
        mainMenuImage.SetActive(true);
    }

    public void ConfirmNewQuiz()
    {
        string quizName = quizNameInputField.text.Trim();

        if (string.IsNullOrEmpty(quizName))
        {
            Debug.LogWarning("Quiz Name is Empty");
            return;
        }

        string folderPath = Path.Combine(Application.dataPath, "Resources");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string filePath = Path.Combine(folderPath, quizName + ".txt");

        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "");
        }

        SelectQuiz(filePath);

        nameQuizPanel.SetActive(false);

        Debug.Log("Successfully created new quiz");
    }

    public void RemoveQuiz()
    {
        if (string.IsNullOrEmpty(selectedQuizPath))
        {
            Debug.LogWarning("No Quiz Selected");
            return;
        }

        string fileName = Path.GetFileNameWithoutExtension(selectedQuizPath);

        // Don't allow the default quiz to be deleted
        if (fileName.ToLower() == "questions")
        {
            Debug.LogWarning("Questions.txt cannot be deleted.");
            return;
        }

        if (File.Exists(selectedQuizPath))
        {
            File.Delete(selectedQuizPath);
        }

        selectedQuizPath = null;
        SelectedQuizName = "";

        currentQuizText.text = "No quiz selected";

        Debug.Log("Quiz deleted: " + fileName);
    }

    public void ConfirmRemoveQuestion()
    {
        if(selectedQuestion.Count == 0)
        {
            Debug.LogWarning("No questions selected for deletion");
            return;
        }

        if (string.IsNullOrEmpty(selectedQuizPath))
        {
            Debug.LogWarning("Selected Quiz File Does Not Exist");
            return;
        }

        string[] lines = File.ReadAllLines(selectedQuizPath);

        System.Collections.Generic.List<string> newLines = new System.Collections.Generic.List<string>();

        int questionsIndex = 0;
        int i = 0;

        while (i < lines.Length)
        {
            //Skip Blank Lines
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                i++;
                continue;
            }

            //Check if ID line, if not, skip
            if (!lines[i].StartsWith("ID:"))
            {
                i++;
                continue;
            }

            //Find End of Question
            int questionstart = i;

            i++;

            //Question
            i++;

            //Question Type
            i++;

            //Input Type
            string inputType = lines[i].Trim();
            i++;

            if (inputType.StartsWith("INPUT:TRUE_OR_FALSE"))
            {
                //True or False only has one Answer line
                i++;
            }

            else
            {
                //Multiple Choice & Identification have four Answer lines
                i += 4;
            }

            int quesetionend = i;

            //Keep this question if not selected
            if (!selectedQuestion.Contains(questionsIndex))
            {
                for(int j = questionstart; j < quesetionend; j++)
                {
                    newLines.Add(lines[j]);
                }

                //Add Spaces between questions
                newLines.Add("");
            }

            questionsIndex++;
        }

        //Renumber the remaning questions
        int newQuestionNumber = 1;

        for (int j = 0; j < newLines.Count; j++)
        {
            if (newLines[j].StartsWith("ID:"))
            {
                //Change Old ID
                string oldID = newLines[j].Substring(3).Trim();

                string quizName = Path.GetFileNameWithoutExtension(selectedQuizPath).ToUpper();

                newLines[j] = "ID:" + quizName + newQuestionNumber;

                //Change question number
                if(j + 1 < newLines.Count)
                {
                    string questionText = newLines[j + 1];

                    int dotIndex = questionText.IndexOf(".");

                    if(dotIndex != -1)
                    {
                        string actualQuestion = questionText.Substring(dotIndex + 1).Trim();

                        newLines[j + 1] = newQuestionNumber + ". " + actualQuestion;
                    }
                }

                newQuestionNumber++;
            }
        }

        File.WriteAllLines(selectedQuizPath, newLines);

        Debug.Log("Deleted " + selectedQuestion.Count + " question(s).");

        selectedQuestion.Clear();

        removeQuestionPanel.SetActive(false);

        RefreshRemoveQuestionPanel();
    }

    public void OpenFileExplorer()
    {
        string folderPath = Path.Combine(Application.dataPath, "Resources");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        Debug.Log("Opening Quiz File Explorer");
        Debug.Log("Starting Folder: " + folderPath);

        ExtensionFilter[] extensions =
        {
        new ExtensionFilter("Quiz Files", "txt")
    };

        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Select Quiz",
            folderPath,
            extensions,
            false
        );

        Debug.Log("Number of paths returned: " + paths.Length);

        if (paths.Length > 0)
        {
            Debug.Log("Selected file path: " + paths[0]);

            SelectQuiz(paths[0]);
        }
        else
        {
            Debug.LogWarning("No file was selected.");
        }
    }

    public void OpenImageFileExplorer()
    {
        ExtensionFilter[] extensions =
        {
        new ExtensionFilter("Image Files", "png", "jpg", "jpeg")
    };

        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Select Image",
            "",
            extensions,
            false
        );

        if (paths.Length > 0)
        {
            selectedImagePath = Path.GetFileNameWithoutExtension(paths[0]);

            Debug.Log("Selected Image: " + selectedImagePath);
        }
    }

    public void OpenAudioFileExplorer()
    {
        ExtensionFilter[] extensions =
        {
        new ExtensionFilter("Audio Files", "mp3", "wav", "ogg")
    };

        string[] paths = StandaloneFileBrowser.OpenFilePanel(
            "Select Audio",
            "",
            extensions,
            false
        );

        if (paths.Length > 0)
        {
            selectedAudioPath = Path.GetFileNameWithoutExtension(paths[0]);

            Debug.Log("Selected Audio: " + selectedAudioPath);
        }
    }

    public void OpenRemoveQuestionPanel()
    {
        Debug.Log("Remove Question using quiz: " + selectedQuizPath);
        
        removeQuestionPanel.SetActive(true);

        RefreshRemoveQuestionPanel();
    }

    public void SelectQuiz(string path)
    {
        selectedQuizPath = path;

        string fileName = Path.GetFileNameWithoutExtension(path);

        currentQuizText.text = fileName;

        SelectedQuizName = fileName;

        Debug.Log("Selected Quiz:" + fileName);
    }

    public void OnInputTypeChanged()
    {
        Debug.Log("Input Type: " + inputTypeDropdown.value);
        identificationPanel.SetActive(false);
        multipleChoicePanel.SetActive(false);
        trueFalsePanel.SetActive(false);

        switch (inputTypeDropdown.value)
        {
            case 0: //Identification
                identificationPanel.SetActive(true);
                break;

            case 1: //Multiple Choice
                multipleChoicePanel.SetActive(true);
                break;

            case 2: //True or False
                trueFalsePanel.SetActive(true);
                break;
        }
    }
    public void OnQuestionTypeChanged()
    {
        Debug.Log("Question Type:" + questionTypeDropdown.value);

        imageButton.SetActive(false);
        audioButton.SetActive(false);

        switch (questionTypeDropdown.value)
        {
            case 0: //Text
                break;

            case 1: //Image
                imageButton.SetActive(true);
                break;

            case 2: //Audio
                audioButton.SetActive(true);
                break;
        }
    }

    private string BuildQuestionEntry(string id, int number)
    {
        string entry = "";

        //ID
        entry += "ID:" + id + "\n";
        
        //Question
        entry += number + ". " + quizQuestionInputField.text.Trim() + "\n";

        //Question Types
        switch (questionTypeDropdown.value)
        {
            case 0:
                entry += "TYPE:TEXT\n";
                break;

            case 1:
                entry += "TYPE:IMAGE:" + selectedImagePath + "\n";
                break;

            case 2:
                entry += "TYPE:AUDIO:" + selectedAudioPath + "\n";
                break;
        }

        //Input Types
        switch (inputTypeDropdown.value)
        {
            case 0: //Indentification
                entry += "INPUT:IDENTIFICATION\n";

                entry += "A. " + identificationAnswer.text.Trim() + " \"C\"\n";
                entry += "B.\n";
                entry += "C.\n";
                entry += "D.\n";
                break;

            case 1: //Multiple Choice
                entry += "INPUT:MULTIPLE_CHOICE\n";

                string[] answers =
                {
                    AnswerA.text.Trim(),
                    AnswerB.text.Trim(),
                    AnswerC.text.Trim(),
                    AnswerD.text.Trim(),
                };

                char letter = 'A';

                for(int i = 0; i < answers.Length; i++)
                {
                    entry += letter + ". " + answers[i];
                    if(i == correctAnswerIndex)
                    {
                        entry += " \"C\"";
                    }

                    entry += "\n";
                    letter++;
                }
                break;

            case 2: //True or False
                entry += "INPUT:TRUE_OR_FALSE\n";

                entry += "ANSWER:" + (trueFalseAnswer ? "TRUE" : "FALSE") + "\n";
                break;
        }

        entry += "\n";

        return entry;
    }

    public void SetCorrectAnswer(int index)
    {
        correctAnswerIndex = index;

        for(int i = 0; i < correctAnswerButtons.Length; i++)
        {
            if(i == index)
            {
                correctAnswerButtons[i].color = Color.green;
            }

            else
            {
                correctAnswerButtons[i].color = Color.white;
            }
        }

        Debug.Log("Correct Answer Set To: " + index);
    }

    public void SetTrueAnswer()
    {
        Debug.Log("True Selected");

        trueFalseAnswer = true;

        trueButtonImage.color = Color.green;
        falseButtonImage.color = Color.red;
    }

    public void SetFalseAnswer()
    {
        Debug.Log("False Selected");

        trueFalseAnswer = false;

        falseButtonImage.color = Color.green;
        trueButtonImage.color = Color.red;
    }

    public void AddQuestion()
    {
        //Validate Quiz file is selected
        if (string.IsNullOrEmpty(SelectedQuizPath))
        {
            Debug.LogWarning("No Quiz Selected");
            return;
        }

        //Validate Quiz file exists
        if (!File.Exists(selectedQuizPath))
        {
            Debug.LogWarning("Selected Quiz File Does not exist");
            return;
        }

        string question = quizQuestionInputField.text.Trim();

        //Check if Question is empty or null
        if (string.IsNullOrEmpty(question))
        {
            Debug.LogWarning("Question is Empty");
            return;
        }

        switch (inputTypeDropdown.value)
        {
            case 0: //Identification
                if (string.IsNullOrEmpty(identificationAnswer.text.Trim()))
                {
                    Debug.LogWarning("Identification Answer is empty or Null");
                    return;
                }

                break;

            case 1: // Multiple Choice

                if (string.IsNullOrEmpty(AnswerA.text.Trim()) ||
                    string.IsNullOrEmpty(AnswerB.text.Trim()) ||
                    string.IsNullOrEmpty(AnswerC.text.Trim()) ||
                    string.IsNullOrEmpty(AnswerD.text.Trim()))
                {
                    Debug.LogWarning("All multiple choice answers must be filled.");
                    return;
                }

                break;

            case 2: // True or False
                break;
        }

        int questionCount = 0;

        string[] lines = File.ReadAllLines(selectedQuizPath);

        foreach(string line in lines)
        {
            if (line.StartsWith("ID:"))
            {
                questionCount++;
            }
        }

        //Question Number is the next following number
        int questionNumber = questionCount + 1;

        //ID line is QuizName + ID Number together
        string quizName = Path.GetFileNameWithoutExtension(selectedQuizPath).ToUpper();
        string id = quizName + questionNumber;

        //Build the Question based on the format
        string entry = BuildQuestionEntry(id, questionNumber);

        //separeate previous question from new question
        if(new FileInfo(selectedQuizPath).Length > 0)
        {
            File.AppendAllText(selectedQuizPath, "");
        }

        //Add question to the file
        File.AppendAllText(selectedQuizPath, entry);
    }

    private class RemoveQuestionData
    {
        public string id;
        public string question;
        public string questionType;
        public string inputType;

        public string answerA;
        public string answerB;
        public string answerC;
        public string answerD;

        public string trueFalseAnswer;


    }
    private System.Collections.Generic.List<RemoveQuestionData> ParseQuestionsForRemoval()
    {
        System.Collections.Generic.List<RemoveQuestionData> result = new System.Collections.Generic.List<RemoveQuestionData>();

        if (string.IsNullOrEmpty(selectedQuizPath))
        {
            return result;
        }

        if (!File.Exists(selectedQuizPath))
        {
            return result;
        }

        string[] lines = File.ReadAllLines(selectedQuizPath);

        int i = 0;

        while (i < lines.Length)
        {
            // Skip blank lines
            if (string.IsNullOrWhiteSpace(lines[i]))
            {
                i++;
                continue;
            }

            // Every question must begin with ID:
            if (!lines[i].StartsWith("ID:"))
            {
                i++;
                continue;
            }

            RemoveQuestionData data = new RemoveQuestionData();

            //ID
            data.id = lines[i].Trim();
            i++;

            //Question
            data.question = lines[i].Trim();

            int dotIndex = data.question.IndexOf(".");

            if (dotIndex != -1)
            {
                data.question =
                    data.question.Substring(dotIndex + 1).Trim();
            }

            i++;

            //QUestion Type
            data.questionType = lines[i].Trim();
            i++;

            //Input Type
            data.inputType = lines[i].Trim();
            i++;

            //True or False
            if (data.inputType.StartsWith("INPUT:TRUE_OR_FALSE"))
            {
                data.trueFalseAnswer = lines[i].Trim();

                i++;
            }
            else
            {

                //A
                data.answerA = lines[i].Trim();
                i++;

                //B
                data.answerB = lines[i].Trim();
                i++;

                //C
                data.answerC = lines[i].Trim();
                i++;

                //D
                data.answerD = lines[i].Trim();
                i++;
            }

            result.Add(data);
        }

        return result;
    }


    private void RefreshRemoveQuestionPanel()
    {
        Debug.Log("REFRESH REMOVE QUESTION PANEL");
        Debug.Log("Children before cleanup: " + questionGridContent.childCount);
        Debug.Log("Selected quiz: " + selectedQuizPath);

        //Remove Old Templates
        for (int i = questionGridContent.childCount - 1; i >= 0; i--)
        {
            Destroy(questionGridContent.GetChild(i).gameObject);
        }

        selectedQuestion.Clear();

        System.Collections.Generic.List<RemoveQuestionData> question = ParseQuestionsForRemoval();

        for(int i = 0; i < question.Count; i++)
        {
            RemoveQuestionData data = question[i];

            GameObject template;

            //True or False template
            if (data.inputType.StartsWith("INPUT:TRUE_OR_FALSE"))
            {
                template = trueFalseQuestionTemplate;
            }

            //Multiple Choice / Identification
            else
            {
                template = multipleChoiceQuestionTemplate;
            }

            GameObject questionObject = Instantiate(template, questionGridContent);

            questionObject.SetActive(true);

            QuestionPlaceholderTemplate item = questionObject.GetComponent<QuestionPlaceholderTemplate>();

            item.Setup(
                this,
                i,
                data.id,
                data.question,
                data.questionType,
                data.inputType,
                data.answerA,
                data.answerB,
                data.answerC,
                data.answerD,
                data.trueFalseAnswer
            );
        }
    }

    public void ToggleQuestionSelection(int index, bool selected)
    {
        if (selected)
        {
            if (!selectedQuestion.Contains(index))
            {
                selectedQuestion.Add(index);
            }
        }

        else
        {
            selectedQuestion.Remove(index);
        }

        Debug.Log("Selected Question: " + selectedQuestion.Count);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string defaultQuizPath = Path.Combine(
            Application.dataPath,
            "Resources",
            "questions.txt"
        );

        if (File.Exists(defaultQuizPath))
        {
            SelectQuiz(defaultQuizPath);
        }
        else
        {
            Debug.LogWarning("Default Questions.txt was not found.");
        }

        SetCorrectAnswer(0);
        SetTrueAnswer();
    }

    // Update is called once per frame
    void Update()
    {

    }
}

