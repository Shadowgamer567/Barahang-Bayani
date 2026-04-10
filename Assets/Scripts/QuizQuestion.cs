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
    Identification
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

    public string imagePath;
    public string audioPath;
}
