//Handles Player hand logic

using System.Collections;
using UnityEngine;

public class CardPanelManager : MonoBehaviour
{
    public GameObject cardTemplate;
    public Transform cardContainer;
    public BattleControl battle;
    public QuizManager quizManager;

    public CardType[] availableCards;
    public int cardCount = 5;
    private bool isRefilling = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RefillToMax();
    }

    void GenerateCards()
    {
        for(int i = 0; i < cardCount; i++)
        {
            DrawCard();
        }
    }

    public void DrawCard()
    {
        if(availableCards.Length == 0)
        {
            return;
        }

        CardType randomCard = availableCards[Random.Range(0, availableCards.Length)];
        GameObject obj = Instantiate(cardTemplate, cardContainer);
        obj.SetActive(true);

        CardUI ui = obj.GetComponent<CardUI>();
        ui.Setup(randomCard, battle, this, quizManager);
    }

    public void RefillToMax()
    {
        int currentCards = cardContainer.childCount;
        int needed = cardCount - currentCards;

        for (int i = 0; i < needed; i++)
        {
            DrawCard();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SyncToData(GameData data)
    {
        data.cards = this.cardCount;
    }
}
