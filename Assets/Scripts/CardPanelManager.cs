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
        GenerateCards();
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

    // Update is called once per frame
    void Update()
    {
        //Debug.Log("Cards in hand: " + cardContainer.childCount);

        if(cardContainer.childCount == 0 && !isRefilling)
        {
            StartCoroutine(RefillHand());
        }
    }

    public void SyncToData(GameData data)
    {
        data.cards = this.cardCount;
    }

    IEnumerator RefillHand()
    {
        isRefilling = true;

        yield return new WaitForSeconds(0.5f);

        GenerateCards();

        isRefilling = false;
    }
}
