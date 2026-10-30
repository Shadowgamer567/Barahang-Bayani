//Handles Player hand logic

using System.Collections;
using TMPro;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CardPoolEntry
{
    public CardType card;
    public HeroType owner;

    public CardPoolEntry(CardType card, HeroType owner)
    {
        this.card = card;
        this.owner = owner;
    }
}

public class CardPanelManager : MonoBehaviour
{
    public static event System.Action OnHandRefilled;

    public GameObject cardTemplate;
    public Transform cardContainer;
    public BattleControl battle;
    public QuizManager quizManager;
    public UIMain uiMain;

    private List<CardPoolEntry> cardPool = new List<CardPoolEntry>();

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
        if(cardPool == null || cardPool.Count == 0)
        {
            return;
        }

        CardPoolEntry entry = cardPool[Random.Range(0, cardPool.Count)];

        GameObject obj = Instantiate(cardTemplate, cardContainer);
        obj.SetActive(true);

        CardUI ui = obj.GetComponent<CardUI>();

        SetupCardUI(ui, entry);
    }

    //THis is for Tutorial
    // Ensures the first tutorial battle starts with non-Quiz Cards.
    public bool ForceNonQuizStartingHand()
    {
        List<CardPoolEntry> nonQuizCards = new List<CardPoolEntry>();

        // Find all available non-Quiz Cards.
        foreach (CardPoolEntry entry in cardPool)
        {
            if (entry.card != null && !entry.card.quizCard)
            {
                nonQuizCards.Add(entry);
            }
        }

        if (nonQuizCards.Count == 0)
        {
            Debug.LogWarning("CardPanelManager: No non-Quiz Cards available!");
            return false;
        }

        // Ensure the hand contains five cards.
        RefillToMax();

        // Replace each card with a randomly selected non-Quiz Card.
        for (int i = 0; i < cardContainer.childCount && i < cardCount; i++)
        {
            CardUI cardUI = cardContainer.GetChild(i).GetComponent<CardUI>();

            if (cardUI == null)
            {
                Debug.LogError("CardPanelManager: Card slot " + i + " has no CardUI!");
                return false;
            }

            CardPoolEntry selectedEntry = nonQuizCards[Random.Range(0, nonQuizCards.Count)];

            SetupCardUI(cardUI, selectedEntry);
        }

        Debug.Log("CardPanelManager: First tutorial hand contains only non-Quiz Cards.");

        return true;
    }

    //This if fot the Tutorial
    public bool ForceQuizCardInFifthSlot()
    {
        // Find the first available Quiz Card.
        CardPoolEntry quizEntry = null;

        foreach (CardPoolEntry entry in cardPool)
        {
            if (entry.card != null && entry.card.quizCard)
            {
                quizEntry = entry;
                break;
            }
        }

        if (quizEntry == null)
        {
            Debug.LogWarning("CardPanelManager: No Quiz Card found!");
            return false;
        }

        // Make sure the hand contains five cards.
        RefillToMax();

        if (cardContainer.childCount < 5)
        {
            Debug.LogWarning("CardPanelManager: Hand has fewer than five slots!");
            return false;
        }

        // Get the fifth card (index 4).
        Transform fifthSlot = cardContainer.GetChild(4);

        CardUI cardUI = fifthSlot.GetComponent<CardUI>();

        if (cardUI == null)
        {
            Debug.LogError("CardPanelManager: Fifth card has no CardUI!");
            return false;
        }

        // Replace its card data with the scripted Quiz Card.
        SetupCardUI(cardUI, quizEntry);

        Debug.Log("CardPanelManager: Scripted Quiz Card loaded into slot 5.");

        return true;
    }

    public void RefillToMax()
    {
        int currentCards = cardContainer.childCount;
        int needed = cardCount - currentCards;

        for (int i = 0; i < needed; i++)
        {
            DrawCard();
        }

        if (cardContainer.childCount > currentCards)
        {
            OnHandRefilled?.Invoke();
        }

        Debug.Log("Hand Refilled to Max. Current: " + cardContainer.childCount);
    }

    public void InitializeCards(LevelData levelData)
    {
        List<CardType> cards = new List<CardType>();

        cardPool.Clear();

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
                    cardPool.Add(new CardPoolEntry(card, null));

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
                        cardPool.Add(new CardPoolEntry(card, hero));

                        Debug.Log("Added CUSTOM card: " + card.cardName + " | Hero: " + hero.name);
                    }
                }
            }
        }

        availableCards = cards.ToArray();

        Debug.Log("=== CARD POOL COMPLETE: " + availableCards.Length + " cards ===");
    }
    private void SetupCardUI(CardUI ui, CardPoolEntry entry)
    {
        Sprite background = entry.owner != null ? entry.owner.customCardBackground : null;

        ui.Setup(entry.card, battle, this, quizManager, uiMain, background);
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
