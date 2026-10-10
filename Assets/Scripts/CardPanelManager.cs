//Handles Player hand logic

using System.Collections;
using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class CardPanelManager : MonoBehaviour
{
    public GameObject cardTemplate;
    public Transform cardContainer;
    public BattleControl battle;
    public QuizManager quizManager;
    public UIMain uiMain;

    public CardType[] availableCards;
    public int cardCount = 5;
    //private bool isRefilling = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("=== CARD PANEL START CALLED ===");

        LevelManager levelManager = FindFirstObjectByType<LevelManager>();

        if (levelManager == null)
        {
            Debug.LogError("CardPanelManager: LevelManager not found!");
            return;
        }

        if (levelManager.levelData == null)
        {
            Debug.LogError("CardPanelManager: LevelData is NULL!");
            return;
        }

        InitializeCards(levelManager.levelData);
    }

    /*
    void GenerateCards()
    {
        for(int i = 0; i < cardCount; i++)
        {
            DrawCard();
        }
    }
    */

    public void DrawCard()
    {
        if(availableCards == null || availableCards.Length == 0)
        {
            return;
        }

        CardType randomCard = availableCards[Random.Range(0, availableCards.Length)];
        GameObject obj = Instantiate(cardTemplate, cardContainer);
        obj.SetActive(true);

        CardUI ui = obj.GetComponent<CardUI>();
        ui.Setup(randomCard, battle, this, quizManager, uiMain);
    }

    public void RefillToMax()
    {
        int currentCards = cardContainer.childCount;
        int needed = cardCount - currentCards;

        for (int i = 0; i < needed; i++)
        {
            DrawCard();
        }

        Debug.Log("Hand Refilled to Max. Current: " + cardContainer.childCount);
    }

    public void InitializeCards(LevelData levelData)
    {
        List<CardType> cards = new List<CardType>();

        Debug.Log("=== INITIALIZING CARD POOL ===");
        Debug.Log("Level: " + levelData.levelName);

        // General cards from Campaign
        if (levelData.campaign != null &&
            levelData.campaign.generalCards != null)
        {
            Debug.Log("Campaign: " + levelData.campaign.name + " | General Cards: " + levelData.campaign.generalCards.Length);

            foreach (CardType card in levelData.campaign.generalCards)
            {
                if (card != null)
                {
                    cards.Add(card);
                    Debug.Log("Added GENERAL card: " + card.cardName);
                }
            }
        }
        else
        {
            Debug.LogWarning("Campaign or General Cards array is missing!");
        }

        // Custom cards from Heroes
        if (levelData.heroesInLevel != null)
        {
            foreach (HeroType hero in levelData.heroesInLevel)
            {
                if (hero == null)
                {
                    continue;
                }
                    

                Debug.Log("Hero: " + hero.name + " | Custom Cards: " + (hero.customCards != null ? hero.customCards.Length : 0));

                if (hero.customCards == null)
                {
                    continue;
                } 

                foreach (CardType card in hero.customCards)
                {
                    if (card != null)
                    {
                        cards.Add(card);

                        Debug.Log("Added CUSTOM card: " + card.cardName + " | Hero: " + hero.name);
                    }
                }
            }
        }

        availableCards = cards.ToArray();

        Debug.Log("=== CARD POOL COMPLETE: " + availableCards.Length + " cards ===");
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
