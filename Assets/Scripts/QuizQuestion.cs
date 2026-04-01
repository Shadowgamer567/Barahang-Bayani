using System.Security;
using UnityEngine;

public enum QuizType
{
    Text,
    Audio,
    Image
}

[System.Serializable]
public class QuizQuestion
{
    public QuizType type;

    public string Question;
    public string[] Answer;
    public int correctIndex;

    public string imagePath;
    public string audioPath;
}
