using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Levels/LevelData")]
public class LevelData : ScriptableObject
{
    public string levelName;
    public string sceneName;
    public int battlesRequired;
    public int levelIndex;

    public BackgroundSet campaign;
    public LevelData previousLevel;
    public LevelData nextLevel;

    //unused for now
    public bool isBossLevel;
}
