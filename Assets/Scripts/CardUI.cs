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

    [Header("Card Appearnace")]
    public Outline cardOutline;
    public Image cardBackground;

    public Sprite defaultCardBackground;

    private CardType cardType;
    private BattleControl battleControl;
    private CardPanelManager cardManager;
    private QuizManager quizManager;
    private UIMain uiMain;

    public Sprite CurrentBackground => cardBackground != null ? cardBackground.sprite : null;

    public Color CurrentOutlineColor => cardOutline != null ? cardOutline.effectColor : Color.white;

    public void Awake()
    {
        if (cardBackground == null)
        {
            cardBackground = GetComponent<Image>();
        }

        if (cardBackground != null)
        {
            defaultCardBackground = cardBackground.sprite;
        }       
    }

    public void Setup(CardType type, BattleControl battle, CardPanelManager manager, QuizManager quiz, UIMain ui, Sprite customBackground = null)
    {
        cardType = type;
        battleControl = battle;
        quizManager = quiz;
        uiMain = ui;
        cardManager = manager;

        nameText.text = type.cardName;
        costText.text = type.cost.ToString();
        descriptionText.text = type.cardShortDesc;
        UpdateCardOutline(type.effectType);

        if (cardBackground != null)
        {
            cardBackground.sprite = customBackground != null ? customBackground : defaultCardBackground;
        }

        if (cardImage != null)
        {
            cardImage.sprite = type.cardImage;
            cardImage.color = type.cardImage != null ? Color.white : Color.clear;

            cardImage.enabled = true;
        }


        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);
    }
    private void UpdateCardOutline(CardEffectType effectType)
    {
        if (cardOutline == null)
            return;

        switch (effectType)
        {
            case CardEffectType.Damage:
                cardOutline.effectColor = new Color32(229, 57, 53, 255);
                break;

            case CardEffectType.Heal:
                cardOutline.effectColor = new Color32(40, 184, 74, 255);
                break;

            case CardEffectType.Shield:
                cardOutline.effectColor = new Color32(138, 152, 168, 255);
                break;

            case CardEffectType.Buff:
                cardOutline.effectColor = new Color32(245, 179, 24, 255);
                break;

            case CardEffectType.Debuff:
                cardOutline.effectColor = new Color32(166, 77, 224, 255);
                break;
        }
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
