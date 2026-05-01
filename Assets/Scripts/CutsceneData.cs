using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Cutscene/CutsceneData")]
public class CutsceneData : ScriptableObject
{
    public List<CutsceneLine> line;
}

[System.Serializable]
public class CutsceneLine
{
    public string speakerName;
    public GameObject characterPrefab;
    public string Dialogue;
    public bool isLeftSide;
}
