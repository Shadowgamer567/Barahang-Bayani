using UnityEngine;

[CreateAssetMenu(fileName = "NewCard", menuName = "Card")]
public class CardType : ScriptableObject
{
    public string cardName;
    public string cardDesc;
    public int damage;
    public int cost;
    public bool quizCard;
}
