//Handles Primary UI display and displaly logic

using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System;

[System.Serializable]
public class EnemyPanel
{
    public GameObject enemyPanel;
    public TextMeshProUGUI EnemyName;
    public Image hpFill;
    public TextMeshProUGUI hpText;
    public Image shieldFill;
    public TextMeshProUGUI shieldText;

}

[System.Serializable]
public class HeroPanel
{
    public GameObject heroPanel;
    public TextMeshProUGUI HeroName;
    public Image hpFill;
    public TextMeshProUGUI hpText;
    public Image shieldFill;
    public TextMeshProUGUI shieldText;
}

public class UIMain : MonoBehaviour
{
    public event Action OnFullCardOpened;

    [Header("Full CardPanel")]
    public GameObject fullCardPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;
    public Image fullCardImage;

    [Header("Full Card Appearance")]
    public Image fullCardBackground;
    public Outline fullCardOutline;

    [Header("Card Transition")]
    public CardTransitionAnimator cardTransitionAnimator;

    private bool isCardTransitioning = false;

    private CardUI selectedCardUI;
    private CardType selectedCardType;

    [Header("Stat Panels")]
    public EnemyPanel[] enemypanel;
    public EnemyStats[] enemies;
    public HeroPanel[] heropanel;
    public HeroStats[] heroes;

    [Header("Tutorial Reference")]
    public TutorialBattleController tutorialBattleController;

    public Transform heroPanelContainer;
    public Transform enemyPanelContainer;

    public GameObject heroPanelTemplate;
    public GameObject enemyPanelTemplate;

    private float[] enemydisplayedHP;
    private float[] herodisplayedHP;
    private float[] enemydisplayedShield;
    private float[] herodisplayedShield;

    private List<EnemyStats> activeEnemies = new List<EnemyStats>();
    private List<HeroStats> activeHeroes = new List<HeroStats>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
    }

    public void GenerateHeroPanels(HeroStats[] heroArray)
    {
        Debug.Log("Spawning Hero Panel");

        heroes = heroArray;

        activeHeroes.Clear();

        List<HeroPanel> generatedPanels = new List<HeroPanel>();

        foreach (Transform child in heroPanelContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < heroes.Length; i++)
        { 

            activeHeroes.Add(heroes[i]);
            GameObject panelObj = Instantiate(heroPanelTemplate, heroPanelContainer);

            panelObj.SetActive(true);

            HeroPanel panel = new HeroPanel();

            panel.heroPanel = panelObj;

            panel.HeroName = panelObj.transform.Find("Name")
                .GetComponent<TextMeshProUGUI>();

            panel.hpFill = panelObj.transform.Find("HP_Background/HP_Fill")
                .GetComponent<Image>();

            panel.hpText = panelObj.transform.Find("HP_Background/HP_Text")
                .GetComponent<TextMeshProUGUI>();

            panel.shieldFill = panelObj.transform.Find("Shield_Background/Shield_Fill")
                .GetComponent<Image>();

            panel.shieldText = panelObj.transform.Find("Shield_Background/Shield_Text")
                .GetComponent<TextMeshProUGUI>();

            generatedPanels.Add(panel);
        }

        heropanel = generatedPanels.ToArray();

        herodisplayedHP = new float[heropanel.Length];
        herodisplayedShield = new float[heropanel.Length];

        for (int i = 0; i < heropanel.Length; i++)
        {
            herodisplayedHP[i] = 1f;
            herodisplayedShield[i] = 1f;
        }
    }

    public void GenerateEnemyPanels(EnemyStats[] enemyArray)
    {
        Debug.Log("UIMain Object: " + gameObject.name);

        Debug.Log("spawnig Enemy Panel");

        enemies = enemyArray;

        activeEnemies.Clear();

        List<EnemyPanel> generatedPanels = new List<EnemyPanel>();

        foreach (Transform child in enemyPanelContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < enemies.Length; i++)
        {

            activeEnemies.Add(enemies[i]);

            GameObject panelObj = Instantiate(enemyPanelTemplate, enemyPanelContainer);

            panelObj.SetActive(true);

            EnemyPanel panel = new EnemyPanel();

            panel.enemyPanel = panelObj;

            panel.EnemyName = panelObj.transform.Find("Name")
                .GetComponent<TextMeshProUGUI>();

            panel.hpFill = panelObj.transform.Find("HP_Background/HP_Fill")
                .GetComponent<Image>();

            panel.hpText = panelObj.transform.Find("HP_Background/HP_Text")
                .GetComponent<TextMeshProUGUI>();

            panel.shieldFill = panelObj.transform.Find("Shield_Background/Shield_Fill")
                .GetComponent<Image>();

            panel.shieldText = panelObj.transform.Find("Shield_Background/Shield_Text")
                .GetComponent<TextMeshProUGUI>();

            generatedPanels.Add(panel);
        }

        enemypanel = generatedPanels.ToArray();

        enemydisplayedHP = new float[enemypanel.Length];
        enemydisplayedShield = new float[enemypanel.Length];

        for (int i = 0; i < enemypanel.Length; i++)
        {
            enemydisplayedHP[i] = 1f;
            enemydisplayedShield[i] = 1f;
        }
    }
    void UpdateEnemyUI()
    {
        if (enemypanel == null || enemies == null)
        {
            return;
        }

        for (int i = 0; i < enemypanel.Length; i++)
        {
            if (enemypanel[i] == null)
            {
                continue;
            }

            EnemyStats enemy = activeEnemies[i];

            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemypanel[i].enemyPanel.SetActive(true);
                enemypanel[i].EnemyName.text = enemy.enemyType.enemyName;

                float targetHP = (float)enemy.currentHP / enemy.enemyType.maxHp;
                enemydisplayedHP[i] = Mathf.Lerp(enemydisplayedHP[i], targetHP, Time.deltaTime * 8f);

                enemypanel[i].hpFill.fillAmount = enemydisplayedHP[i];
                enemypanel[i].hpText.text = $"{enemy.currentHP} / {enemy.enemyType.maxHp}";

                float maxShield = Mathf.Max(1, enemy.enemyType.maxShield);
                float targetShield = (float)enemy.shield / maxShield;

                enemydisplayedShield[i] = Mathf.Lerp(enemydisplayedShield[i], targetShield, Time.deltaTime * 8f);

                enemypanel[i].shieldFill.fillAmount = enemydisplayedShield[i];
                enemypanel[i].shieldText.text = $"{enemy.shield} / {enemy.enemyType.maxShield}";
            }
            else
            {
                enemypanel[i].enemyPanel.SetActive(false);
            }
        }
    }

    void UpdateHeroUI()
    {
        if (heropanel == null || heroes == null)
        {
            return;
        }

        for (int i = 0; i < heropanel.Length; i++)
        {
            if (heropanel[i] == null)
            {
                continue;
            }

            HeroStats hero = activeHeroes[i];

            if (hero != null && hero.gameObject.activeInHierarchy)
            {
                heropanel[i].heroPanel.SetActive(true);
                heropanel[i].HeroName.text = hero.heroType.heroName;

                // HP
                float targetHP = (float)hero.currentHP / hero.heroType.maxHp;
                herodisplayedHP[i] = Mathf.Lerp(herodisplayedHP[i], targetHP, Time.deltaTime * 8f);

                heropanel[i].hpFill.fillAmount = herodisplayedHP[i];
                heropanel[i].hpText.text = $"{hero.currentHP} / {hero.heroType.maxHp}";

                // SHIELD
                float maxShield = Mathf.Max(1, hero.heroType.maxShield);
                float targetShield = (float)hero.shield / maxShield;

                herodisplayedShield[i] = Mathf.Lerp(herodisplayedShield[i], targetShield, Time.deltaTime * 8f);

                heropanel[i].shieldFill.fillAmount = herodisplayedShield[i];
                heropanel[i].shieldText.text = $"{hero.shield} / {hero.heroType.maxShield}";
            }
            else
            {
                heropanel[i].heroPanel.SetActive(false);
            }
        }
    }

    public void ShowFullCard(CardUI cardUI, CardType cardType)
    {
        if (isCardTransitioning || selectedCardUI != null)
        {
            return;
        }

        selectedCardUI = cardUI;
        selectedCardType = cardType;

        // Prepare the FullCard content.
        nameText.text = cardType.cardName;
        costText.text = cardType.cost.ToString();
        descriptionText.text = cardType.cardFullDesc;

        if (fullCardImage != null)
        {
            fullCardImage.sprite = cardType.cardImage;
            fullCardImage.enabled = cardType.cardImage != null;
        }

        if (fullCardBackground != null)
        {
            fullCardBackground.sprite = cardUI.CurrentBackground;
        }

        if (fullCardOutline != null)
        {
            fullCardOutline.effectColor = cardUI.CurrentOutlineColor;
        }

        if (cardTransitionAnimator != null)
        {
            StartCoroutine(ShowFullCardAnimated(cardUI));
        }

        else
        {
            // Fallback to the original behavior.
            fullCardPanel.SetActive(true);
            cardUI.gameObject.SetActive(false);
            OnFullCardOpened?.Invoke();
        }
    }

    public void ConfirmCardPlay()
    {
        if (selectedCardUI == null || selectedCardType == null)
        {
            return;
        }

        if (isCardTransitioning)
        {
            return;
        }

        bool played = selectedCardUI.PlayCard();

        fullCardPanel.SetActive(false);

        if (!played && selectedCardUI != null)
        {
            selectedCardUI.gameObject.SetActive(true);
        }
        selectedCardUI = null;
        selectedCardType = null;
    }

    public void CancelCardPreview()
    {
        if (isCardTransitioning)
        { 
            return; 
        }

        if (tutorialBattleController != null && tutorialBattleController.BlockCardPreviewCancellation)
        {
            return;
        }

        if (selectedCardUI == null)
        {
            return;
        }

        if (cardTransitionAnimator != null)
        {
            StartCoroutine(CancelCardPreviewAnimated());
        }

        else
        {
            selectedCardUI.gameObject.SetActive(true);
            fullCardPanel.SetActive(false);

            selectedCardUI = null;
            selectedCardType = null;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (enemypanel != null && enemies != null)
        {
            UpdateEnemyUI();
        }

        if (heropanel != null && heroes != null)
        {
            UpdateHeroUI();
        }
    }

    private IEnumerator ShowFullCardAnimated(CardUI cardUI)
    {
        isCardTransitioning = true;

        RectTransform source = cardUI.GetComponent<RectTransform>();

        RectTransform destination = cardTransitionAnimator.fullCardTarget;

        // Keep FullCard hidden while the animation plays.
        fullCardPanel.SetActive(false);

        // Animate the visual copy from the hand to FullCard.
        yield return StartCoroutine(cardTransitionAnimator.AnimateCard(source, destination));

        // The original card is hidden only after the animation.
        if (cardUI != null)
        {
            cardUI.gameObject.SetActive(false);
        }

        // Reveal the actual FullCard.
        fullCardPanel.SetActive(true);

        isCardTransitioning = false;

        // Notify existing tutorial and UI listeners.
        OnFullCardOpened?.Invoke();
    }

    private IEnumerator CancelCardPreviewAnimated()
    {
        isCardTransitioning = true;

        CardUI cardUI = selectedCardUI;

        RectTransform source = cardUI.GetComponent<RectTransform>();

        RectTransform destination = cardTransitionAnimator.fullCardTarget;

        // Hide FullCard while the animated copy returns.
        fullCardPanel.SetActive(false);

        // Animate from FullCard back to the original hand position.
        yield return StartCoroutine(cardTransitionAnimator.AnimateCard(source, destination, true));

        // Restore the original card.
        if (cardUI != null)
        {
            cardUI.gameObject.SetActive(true);
        }

        selectedCardUI = null;
        selectedCardType = null;

        isCardTransitioning = false;
    }
}