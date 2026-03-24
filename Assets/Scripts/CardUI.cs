using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public Button button;

    private CardType cardType;
    private BattleControl battleControl;

    public void Setup(CardType type, BattleControl battle)
    {
        cardType = type;
        battleControl = battle;

        nameText.text = type.cardName;
        costText.text = type.cost.ToString();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        Debug.Log("Played Card: " + cardType.cardName);

        if(battleControl != null)
        {
            battleControl.DealDamageToAll(cardType.damage);
        }

    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
