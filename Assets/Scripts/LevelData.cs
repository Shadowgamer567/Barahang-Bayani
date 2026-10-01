using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelData", menuName = "Levels/LevelData")]
public class LevelData : ScriptableObject
{
    public string levelName;
    public string sceneName;
    public int battlesRequired;
    public int levelIndex;
    public List<CutsceneTrigger> Cutscenes;

    public BackgroundSet campaign;
    public LevelData previousLevel;
    public LevelData nextLevel;

    public List<HeroType> heroesInLevel;
    public int maxHeroes = 4;

    public List<EnemyTypes> possibleEnemies;
    public int minEnemies = 1;
    public int maxEnemies = 4;

    //unused for now
    public bool isBossLevel;
}
