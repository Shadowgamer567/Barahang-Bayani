using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "NewCard", menuName = "Card")]
public class CardType : ScriptableObject
{
    [Header("Card Information")]
    public string cardName;
    public string cardShortDesc;
    public string cardFullDesc;

    [Header("Card Effect")]
    public CardEffectType effectType = CardEffectType.Damage;
    public TargetType targetType = TargetType.AllEnemies;

    public int damage;
    public int cost;
    public bool quizCard;
}

public enum TargetType
{
    AllEnemies,
    SingleTarget,
    AllHeroes,
    Self
}

public enum CardEffectType
{
    Damage,
    Heal,
    Shield,
    Buff,
    Debuff
}