using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ProgressData
{
    public string accountId;
    public string username;
    public string accountType;

    public List<string> completedLevel = new List<string>();
    public bool tutorialCompleted = false;

    public string lastCampaignScene;

    public string playthroughId;

    public int multipleChoiceCorrect;
    public int multipleChoiceTotal;

    public int identificationCorrect;
    public int identificationTotal;

    public int trueFalseCorrect;
    public int trueFalseTotal;

    public List<QuizResultData> pendingQuizResults = new List<QuizResultData>();

    public bool quizResultLedgerInitialized = false;

    public bool pendingNewGameReset = false;
    public string previousPlaythroughId = "";
}