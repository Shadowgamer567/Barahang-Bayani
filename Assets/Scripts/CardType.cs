using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "NewCard", menuName = "Card")]
public class CardType : ScriptableObject
{
    public string cardName;
    public string cardShortDesc;
    public string cardFullDesc;
    public TargetType targetType;
    public int damage;
    public int cost;
    public bool quizCard;
}

public enum TargetType
{
    AllEnemies,
    SingleTarget
}