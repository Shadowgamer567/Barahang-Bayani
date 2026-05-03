using UnityEngine;

[CreateAssetMenu(fileName = "HeroType", menuName = "Heroes/HeroTypes")]
public class HeroType : ScriptableObject
{
    public string heroName;
    public int maxHp;
    public int shield;
    public int maxShield;
    public GameObject prefab;
    public Vector3 modelRotationOffset;
}
