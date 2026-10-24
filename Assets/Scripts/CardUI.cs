//Handles Card effect logic

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardUI : MonoBehaviour
{
    public static event Action OnCardClicked;

    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;
    public Image cardImage;
    public Button button;

    private CardType cardType;
    private BattleControl battleControl;
    private CardPanelManager cardManager;
    private QuizManager quizManager;
    private UIMain uiMain;

    public void Setup(CardType type, BattleControl battle, CardPanelManager manager, QuizManager quiz, UIMain ui)
    {
        cardType = type;
        battleControl = battle;
        quizManager = quiz;
        uiMain = ui;
        cardManager = manager;

        nameText.text = type.cardName;
        costText.text = type.cost.ToString();
        descriptionText.text = type.cardShortDesc;
        
        if (cardImage != null)
        {
            cardImage.sprite = type.cardImage;
            cardImage.color = type.cardImage != null ? Color.white : Color.clear;

            cardImage.enabled = true;
        }


        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        TutorialBattleController tutorial = FindFirstObjectByType<TutorialBattleController>();

        if (tutorial != null && !tutorial.CanSelectHandCard(this))
        {
            return;
        }

        OnCardClicked?.Invoke();
        Debug.Log("Card clicked -> sending to BattleControl");

        if (battleControl.currentstate != BattleControl.BattleState.PlayerTurn)
        {
            Debug.Log("Not your turn");
            return;
        }

        if(uiMain != null)
        {
            uiMain.ShowFullCard(this, cardType);
        }
    }

    public bool PlayCard()
    {
        Debug.Log("Played Card: " + cardType.cardName);
        Debug.Log("Target Type: " + cardType.targetType);

        return battleControl.HandleCardPlay(cardType, this);
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
