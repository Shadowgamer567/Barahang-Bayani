using UnityEngine;

[CreateAssetMenu(fileName = "EnemyTypes", menuName = "Enemies/EnemyTypes")]
public class EnemyTypes : ScriptableObject
{
    public string enemyName;
    public int maxHp;
    public int damage;
}
