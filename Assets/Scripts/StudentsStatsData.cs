using NUnit.Framework;
using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class StudentStatsData
{
    public List<string> completedLevels = new();

    public string lastCampaignScene;

    public int multipleChoiceCorrect;
    public int multipleChoiceTotal;

    public int identificationCorrect;
    public int identificationTotal;

    public int trueFalseCorrect;
    public int trueFalseTotal;
}
