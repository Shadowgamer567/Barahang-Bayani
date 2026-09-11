using System.IO;
using TMPro;
using UnityEngine;

public class QuizMakerManager : MonoBehaviour
{
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

    private bool trueFalseAnswer = true;

    private string selectedImagePath;
    private string selectedAudioPath;

    private string selectedQuizPath;

    public string SelectedQuizPath => selectedQuizPath;

    public void OpenNewQuizPanel()
    {
        nameQuizPanel.SetActive(true);
        quizNameInputField.text = "";
    }

    public void CancelNewQuiz()
    {
        nameQuizPanel.SetActive(false);
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

    public void SelectQuiz(string path)
    {
        selectedQuizPath = path;

        string fileName = Path.GetFileNameWithoutExtension(path);

        currentQuizText.text = fileName;

        Debug.Log("Selected Quiz:" + fileName);
    }

    public void OnInputTypeChanged()
    {
        Debug.Log("Input Type: " + inputTypeDropdown.value);
        identificationPanel.SetActive(false);
        multipleChoicePanel.SetActive(false);
        trueFalsePanel.SetActive(false);

        switch (questionTypeDropdown.value)
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

        switch (inputTypeDropdown.value)
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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
