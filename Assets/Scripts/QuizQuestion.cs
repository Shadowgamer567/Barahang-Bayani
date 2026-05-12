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

    public string imagePath;
    public string audioPath;

    public string infoTitle;
    public Sprite infoImage;
    [TextArea(5, 10)]
    public string infoText;
}
