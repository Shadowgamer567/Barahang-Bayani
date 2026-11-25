//Handles Card effect logic

using System;
using System.Collections.Generic;
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
    private GameObject cardTypeIndicator;
    private static readonly Dictionary<CardEffectType, Sprite> effectIconCache = new Dictionary<CardEffectType, Sprite>();

    public CardType Type => cardType;
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
        descriptionText.richText = true;
        descriptionText.text = CombatTextFormatter.Colorize(type.cardShortDesc);
        UpdateCardOutline(type.effectType);
        UpdateTypeIndicator(type.effectType);

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

    private void UpdateTypeIndicator(CardEffectType effectType)
    {
        if (cardTypeIndicator == null)
        {
            cardTypeIndicator = new GameObject("Card Type Indicator", typeof(RectTransform), typeof(Image));
            RectTransform indicatorRect = cardTypeIndicator.GetComponent<RectTransform>();
            indicatorRect.SetParent(transform, false);
            indicatorRect.anchorMin = new Vector2(0.5f, 1f);
            indicatorRect.anchorMax = new Vector2(0.5f, 1f);
            indicatorRect.pivot = new Vector2(0.5f, 0f);
            indicatorRect.anchoredPosition = new Vector2(0f, 5f);
            indicatorRect.sizeDelta = new Vector2(136f, 28f);

            Image background = cardTypeIndicator.GetComponent<Image>();
            background.color = new Color(0.12f, 0.1f, 0.08f, 0.94f);
            background.raycastTarget = false;

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(indicatorRect, false);
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(5f, 0f);
            iconRect.sizeDelta = new Vector2(22f, 22f);
            Image iconImage = iconObject.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            GameObject labelObject = new GameObject("Type Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(indicatorRect, false);
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(32f, 0f);
            labelRect.offsetMax = new Vector2(-28f, 0f);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 13f;
            label.fontStyle = FontStyles.Bold;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Midline;
            label.raycastTarget = false;

            GameObject questionObject = new GameObject("Question Type Icon", typeof(RectTransform), typeof(Image));
            RectTransform questionRect = questionObject.GetComponent<RectTransform>();
            questionRect.SetParent(indicatorRect, false);
            questionRect.anchorMin = new Vector2(1f, 0.5f);
            questionRect.anchorMax = new Vector2(1f, 0.5f);
            questionRect.pivot = new Vector2(1f, 0.5f);
            questionRect.anchoredPosition = new Vector2(-4f, 0f);
            questionRect.sizeDelta = new Vector2(20f, 20f);
            Image questionIconImage = questionObject.GetComponent<Image>();
            questionIconImage.preserveAspect = true;
            questionIconImage.raycastTarget = false;
        }

        Image icon = cardTypeIndicator.transform.Find("Icon").GetComponent<Image>();
        TextMeshProUGUI typeLabel = cardTypeIndicator.transform.Find("Type Label").GetComponent<TextMeshProUGUI>();
        Image questionIcon = cardTypeIndicator.transform.Find("Question Type Icon").GetComponent<Image>();
        Sprite typeIcon = LoadEffectIcon(effectType);
        icon.sprite = typeIcon;
        icon.enabled = typeIcon != null;
        Sprite questionSprite = Resources.Load<Sprite>("CardTypeIcons/Question");
        questionIcon.sprite = questionSprite;
        questionIcon.enabled = cardType != null && cardType.quizCard && questionSprite != null;
        typeLabel.text = cardType != null && cardType.quizCard ? "QUESTION" : GetEffectLabel(effectType);
        cardTypeIndicator.transform.SetAsLastSibling();
    }

    private static Sprite LoadEffectIcon(CardEffectType effectType)
    {
        if (!effectIconCache.TryGetValue(effectType, out Sprite icon))
        {
            icon = Resources.Load<Sprite>("CardTypeIcons/" + effectType.ToString());
            effectIconCache[effectType] = icon;
        }
        return icon;
    }

    private static string GetEffectLabel(CardEffectType effectType)
    {
        switch (effectType)
        {
            case CardEffectType.Damage: return "ATTACK";
            case CardEffectType.Heal: return "HEAL";
            case CardEffectType.Shield: return "SHIELD";
            case CardEffectType.Buff: return "BUFF";
            case CardEffectType.Debuff: return "DEBUFF";
            case CardEffectType.Piercing: return "PIERCING";
            case CardEffectType.Bleed: return "BLEED";
            default: return effectType.ToString().ToUpperInvariant();
        }
    }

    private void UpdateCardOutline(CardEffectType effectType)
    {
        if (cardOutline == null)
            return;

        switch (effectType)
        {
            case CardEffectType.Damage:
            case CardEffectType.Piercing:
                cardOutline.effectColor = new Color32(229, 57, 53, 255);
                break;

            case CardEffectType.Bleed:
                cardOutline.effectColor = new Color32(150, 35, 45, 255);
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
