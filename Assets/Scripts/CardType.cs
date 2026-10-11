using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "NewCard", menuName = "Card")]
public class CardType : ScriptableObject
{
    [Header("Card Information")]
    public string cardName;

    [TextArea(3, 10)]
    public string cardShortDesc;
    [TextArea(3, 10)]
    public string cardFullDesc;

    [Header("Card Artwork")]
    public Sprite cardImage;

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