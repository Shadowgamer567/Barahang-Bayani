using UnityEngine;

[CreateAssetMenu(fileName = "EnemyTypes", menuName = "Enemies/EnemyTypes")]
public class EnemyTypes : ScriptableObject
{
    public string enemyName;
    public int maxHp;
    public int damage;
    public int maxShield;
    public GameObject prefab;
    public Vector3 modelRotationOffset;
}

public enum EnemyActionType
{
    Attack,
    Heal,
    Shield,
    Buff,
    Debuff
}

[System.Serializable]
public class EnemyAction
{
    public EnemyActionType actionType;
    public int value;
}