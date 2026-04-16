using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Levels/LevelData")]
public class LevelData : ScriptableObject
{
    public string levelName;
    public string sceneName;

    public int battlesRequired;
    public LevelData nextLevel;

    //unused for now
    public bool isBossLevel;
}
