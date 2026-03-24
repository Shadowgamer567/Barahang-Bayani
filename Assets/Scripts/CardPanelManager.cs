using UnityEngine;

public class CardPanelManager : MonoBehaviour
{
    public GameObject cardTemplate;
    public Transform cardContainer;
    public BattleControl battle;

    public CardType[] availableCards;
    public int cardCount = 5;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GenerateCards();
    }

    void GenerateCards()
    {
        for(int i = 0; i < cardCount; i++)
        {
            CardType randomCard = availableCards[Random.Range(0, availableCards.Length)];
            GameObject obj = Instantiate(cardTemplate, cardContainer);
            obj.SetActive(true);

            CardUI ui = obj.GetComponent<CardUI>();
            ui.Setup(randomCard, battle);
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
