using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ProgressData
{
    public string accountId;
    public string username;
    public string accountType;

    public List<string> completedLevel = new List<string>();

    public string lastCampaignScene;

    public int multipleChoiceCorrect;
    public int multipleChoiceTotal;

    public int identificationCorrect;
    public int identificationTotal;

    public int trueFalseCorrect;
    public int trueFalseTotal;
}