using System.Security;
using UnityEngine;

public enum QuizType
{
    Text,
    Audio,
    Image
}

public enum InputType
{
    MultipleChoice,
    Identification,
    TrueOrFalse
}

[System.Serializable]
public class QuizQuestion
{
    public QuizType type;
    public InputType inputType;

    public string Question;
    public string[] Answer;
    public string correctAnswer;
    public int correctIndex;
    public bool correctBool;
    public string questionID;

    public string imagePath;
    public string audioPath;

    [Header("Info Panel")]
    public string infoTitle;
    public string infoImagePath;
    [TextArea(5, 10)]
    public string infoDescription;
}
