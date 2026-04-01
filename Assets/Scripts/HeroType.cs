using UnityEngine;

[CreateAssetMenu(fileName = "HeroType", menuName = "Heroes/HeroTypes")]
public class HeroType : ScriptableObject
{
    public string heroName;
    public int maxHp;
}
