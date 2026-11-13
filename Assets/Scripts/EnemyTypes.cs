using UnityEngine;

[CreateAssetMenu(fileName = "EnemyTypes", menuName = "Enemies/EnemyTypes")]
public class EnemyTypes : ScriptableObject
{
    public string enemyName;
    public int maxHp;
    public int damage;
    public int shield;
    public int maxShield;
    public GameObject prefab;
    public EnemyAction[] actions;

    [Header("Enemy AI - Base Action Weights")]
    [Min(0)] public float attackWeight = 5f;
    [Min(0)] public float healWeight = 3f;
    [Min(0)] public float shieldWeight = 3f;
    [Min(0)] public float buffWeight = 4f;
    [Min(0)] public float debuffWeight = 4f;

    public Vector3 modelRotationOffset;
    public Vector3 modelPositionOffset;
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