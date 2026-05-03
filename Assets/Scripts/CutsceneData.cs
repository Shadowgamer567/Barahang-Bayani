using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Cutscene/CutsceneData")]
public class CutsceneData : ScriptableObject
{
    public List<CutsceneLine> line;
}

public enum SpeakerType
{
    Hero, 
    Enemy,
    Narrator
}

[System.Serializable]
public class CutsceneLine
{
    public string speakerName;
    public SpeakerType speakerType;
    public HeroType hero;
    public EnemyTypes enemy;
    public string Dialogue;
    public bool isLeftSide;
}
