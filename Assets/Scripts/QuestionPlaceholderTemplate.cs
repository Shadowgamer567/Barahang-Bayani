using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestionPlaceholderTemplate : MonoBehaviour
{
    [Header("Question Information")]
    public TMP_Text idText;
    public TMP_Text questionText;
    public TMP_Text questionTypeText;
    public TMP_Text inputTypeText;

    [Header("Multiple Choice / Identification")]
    public TMP_Text answerAText;
    public TMP_Text answerBText;
    public TMP_Text answerCText;
    public TMP_Text answerDText;

    [Header("True / False")]
    public TMP_Text answerText;

    [Header("Selection")]
    public Image backgroundImage;

    private QuizMakerManager manager;
    private int questionIndex;

    private bool selected;

    public void Setup(QuizMakerManager quizManager, int index, string id, string question, string questionType, string inputType, string answerA, string answerB, string answerC, string answerD, string trueFalseAnswer)
    {
        manager = quizManager;
        questionIndex = index;

        idText.text = id;
        questionText.text = question;
        questionTypeText.text = questionType;
        inputTypeText.text = inputType;

        //Multiple Choice / Identification
        if(answerAText != null)
        {
            answerAText.text = answerA;
        }

        if (answerBText != null)
        {
            answerBText.text = answerB;
        }

        if (answerCText != null)
        {
            answerCText.text = answerC;
        }

        if (answerDText != null)
        {
            answerDText.text = answerD;
        }

        //True or False 
        if (answerText != null)
        {
            answerText.text = trueFalseAnswer;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = Color.white;
        }
    }

    public void ToggleSelected()
    {
        selected = !selected;
        if (backgroundImage != null)
        {
            if (selected)
            {
                backgroundImage.color = Color.green;
            }

            else
            {
                backgroundImage.color = Color.white;
            }
        }

        manager.ToggleQuestionSelection(questionIndex, selected);
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
