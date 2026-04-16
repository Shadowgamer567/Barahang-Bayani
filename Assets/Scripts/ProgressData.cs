using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ProgressData
{
    public List<string> completedLevel = new List<string>();
    public List<string> unlockedLevel = new List<string>();
}
