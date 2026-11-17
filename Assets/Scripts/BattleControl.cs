//Handles Battle Logic and sequence

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using System.Linq;
using TMPro;
using UnityEngine.UI;

public class BattleControl : MonoBehaviour
{
    public Transform enemyController;
    public Transform heroController;
    public GroundLoop groundloop;
    public GameObject endButton;
    public GameObject cardPanel;
    public GameObject selectTargetPanel;
    public GameObject notEnoughAPPanel;
    public UIMain uiMain;

    [Header("Battle Log")]
    public BattleLogManager battleLog;

    [Header("Turn Order UI")]
    [SerializeField] private Color activeTurnColor = new Color(1f, 0.83f, 0.36f, 1f);
    [SerializeField] private string turnOrderTitle = "TURN ORDER";
    [SerializeField] private RectTransform turnOrderPanel;
    [SerializeField] private TextMeshProUGUI turnOrderLabel;
    [SerializeField] private Image turnOrderArrowImage;
    [SerializeField] private Vector2 turnOrderArrowOffset = new Vector2(18f, -50f);
    [SerializeField, Min(1f)] private float turnOrderRowSpacing = 26f;
    [SerializeField] private Sprite activeCharacterArrowSprite;
    [SerializeField] private Vector3 activeCharacterArrowOffset = new Vector3(0f, 0.3f, 0f);
    [SerializeField, Min(0.1f)] private float activeCharacterArrowScale = 0.8f;
    [SerializeField, Min(0f)] private float turnTransitionDelay = 0.5f;
    [Header("Turn State UI")]
    [SerializeField] private GameObject penaltyTurnPanel;

    public CardType pendingCard;
    public HeroStats pendingHero;
    private LevelData levelData;

    [Header("Player Team Modifier")]
    public ActionModifier playerModifier;

    public float moveduration = 5f;
    public int damage = 5;
    public bool nextbattletriggered = false;
    public int aliveEnemies = 0;
    public BattleState currentstate;
    public bool isSelectingTarget = false;
    public int maxActionPoint = 5;
    public int currentActionPoint;
    public bool isLevelFinished = false;
    private int battleNumber = 0;
    private bool battleSequenceRunning = false;

    private bool battleAlreadyWon = false;

    public System.Action<int, int> OnAPChanged;

    public System.Action OnBattleWon;
    public System.Action OnBattleLost;

    public System.Action OnFirstBattleStarted;
    public System.Action OnSecondBattleStarted;

    public System.Action OnCardPlayAccepted;
    public System.Action OnCardResolutionComplete;

    private CharacterAnimationRelay activeCardAnimationRelay;

    public bool IsTargetingEnemies =>
    isSelectingTarget &&
    pendingCard != null &&
    (pendingCard.effectType == CardEffectType.Damage || 
     pendingCard.effectType == CardEffectType.Piercing ||
     pendingCard.effectType == CardEffectType.Debuff ||
     pendingCard.effectType == CardEffectType.Bleed);

    public bool IsTargetingHeroes =>
        isSelectingTarget &&
        pendingCard != null &&
        (pendingCard.effectType == CardEffectType.Heal ||
         pendingCard.effectType == CardEffectType.Shield);

    EnemyStats[] enemies;
    HeroStats[] heroes;
    private class TurnActor
    {
        public HeroStats hero;
        public EnemyStats enemy;
        public int Speed => hero != null ? hero.Speed : enemy != null ? enemy.Speed : 0;
        public string Name => hero != null ? hero.heroType.heroName : enemy != null ? enemy.DisplayName : "Unknown";
    }
    private readonly List<TurnActor> turnOrder = new List<TurnActor>();
    private int turnIndex = -1;
    private HeroStats activeHero;
    private Transform activeWorldArrowTarget;
    private Transform activeWorldArrowHeadTarget;
    private GameObject activeWorldArrowObject;
    private SpriteRenderer activeWorldArrowRenderer;
    private bool autoEndTurnPending;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetEnemiesActive(false);
        if (penaltyTurnPanel != null) penaltyTurnPanel.SetActive(false);
        CreateTurnOrderDisplay();
    }

    public void Initialize(LevelData data)
    {
        levelData = data;

        if (playerModifier != null)
        {
            playerModifier.ClearModifier();
        }

        CacheEnemies();
        CacheHeroes();

        if (heroes == null || heroes.Length == 0)
        {
            Debug.LogError("Heroes not ready — aborting setup");
            return;
        }

        SetupHeroes();

        
    }

    public enum BattleState
    {
        PlayerTurn,
        EnemyTurn,
        Busy,
        Win,
        Lose,
        ForcedTurnEnd,
        PenaltyTurn
    }

    void CacheEnemies()
    {
        enemies = enemyController.GetComponentsInChildren<EnemyStats>(true);

        aliveEnemies = 0;
        foreach(var e in enemies)
        {
            if (e.gameObject.activeInHierarchy)
            {
                aliveEnemies++;
            }
        }

        Debug.Log("=== ENEMY CACHE START ===");

        foreach (var e in enemies)
        {
            Debug.Log("Cached: " + e.name);
        }

        Debug.Log("=== ENEMY CACHE END ===");
        Debug.Log("Alive Enemies:" + aliveEnemies);
    }

    void CacheHeroes()
    {
        if (heroController == null)
        {
            Debug.LogError("heroController is NOT assigned!");
            return;
        }

        heroes = heroController.GetComponentsInChildren<HeroStats>(true);

        Debug.Log("Cached Heroes: " + heroes.Length);
    }

    void SetupHeroes()
    {
        for (int i = 0; i < heroes.Length; i++)
        {
            if (i < levelData.heroesInLevel.Count)
            {
                heroes[i].heroType = levelData.heroesInLevel[i];
                heroes[i].gameObject.SetActive(true);
                heroes[i].InitializeHero();
            }
            else
            {
                heroes[i].gameObject.SetActive(false);
            }
        }

        if (uiMain != null)
        {
            uiMain.GenerateHeroPanels(heroes);
        }
    }

    void SetupEnemies()
    {
        battleAlreadyWon = false; 
        // A new battle starts with a full AP pool on its first hero turn.
        currentActionPoint = 0;

        int enemyCount = Random.Range(levelData.minEnemies, levelData.maxEnemies + 1);

        for (int i = 0; i < enemies.Length; i++)
        {
            if (i < enemyCount)
            {
                EnemyTypes type = levelData.possibleEnemies[Random.Range(0, levelData.possibleEnemies.Count)];

                enemies[i].enemyType = type;
                enemies[i].gameObject.SetActive(true);
                enemies[i].InitializeEnemy();
                enemies[i].LoadModel();
            }

            else
            {
                enemies[i].gameObject.SetActive(false);
            }
        }

        AssignEnemyDisplayNames();

        if (uiMain != null)
        {
            Debug.Log("Using UI Object: " + uiMain.gameObject.name);

            uiMain.GenerateEnemyPanels(enemies);
        }

        InitializeTurnOrder();
    }

    private void AssignEnemyDisplayNames()
    {
        Dictionary<EnemyTypes, int> totals = new Dictionary<EnemyTypes, int>();
        Dictionary<EnemyTypes, int> numbers = new Dictionary<EnemyTypes, int>();
        foreach (EnemyStats enemy in enemies)
        {
            if (!IsAlive(enemy) || enemy.enemyType == null) continue;
            totals.TryGetValue(enemy.enemyType, out int total);
            totals[enemy.enemyType] = total + 1;
        }

        foreach (EnemyStats enemy in enemies)
        {
            if (!IsAlive(enemy) || enemy.enemyType == null) continue;
            numbers.TryGetValue(enemy.enemyType, out int number);
            number++;
            numbers[enemy.enemyType] = number;
            string displayName = totals[enemy.enemyType] > 1
                ? enemy.enemyType.enemyName + " " + number
                : enemy.enemyType.enemyName;
            enemy.SetDisplayName(displayName);
        }
    }

    private void InitializeTurnOrder()
    {
        RebuildTurnOrder();
        turnIndex = -1;
        UpdateTurnOrderDisplay();
    }

    private void RebuildTurnOrder()
    {
        turnOrder.Clear();
        foreach (HeroStats hero in heroes)
            if (IsAlive(hero)) { hero.RollSpeed(); turnOrder.Add(new TurnActor { hero = hero }); }
        foreach (EnemyStats enemy in enemies)
            if (IsAlive(enemy)) { enemy.RollSpeed(); turnOrder.Add(new TurnActor { enemy = enemy }); }
        // LINQ's stable ordering preserves team/list order when Speed is tied.
        var sorted = turnOrder.OrderByDescending(actor => actor.Speed).ToList();
        turnOrder.Clear();
        turnOrder.AddRange(sorted);
        Debug.Log("Turn order: " + string.Join(" -> ", turnOrder.Select(actor => actor.Name + " (" + actor.Speed + ")")));
    }

    private void CreateTurnOrderDisplay()
    {
        if (turnOrderLabel != null)
        {
            CreateFallbackTurnOrderArrow();
            return;
        }
        Canvas canvas = endButton != null ? endButton.GetComponentInParent<Canvas>() : null;
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("Turn order UI: No Canvas found in the battle scene.");
            return;
        }

        GameObject panel = new GameObject("Turn Order Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.sizeDelta = new Vector2(280f, 250f);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.05f, 0.07f, 0.1f, 0.82f);
        panelImage.raycastTarget = false;
        turnOrderPanel = panelRect;

        GameObject labelObject = new GameObject("Turn Order Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(panel.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(14f, 12f);
        labelRect.offsetMax = new Vector2(-14f, -12f);

        turnOrderLabel = labelObject.GetComponent<TextMeshProUGUI>();
        turnOrderLabel.font = TMP_Settings.defaultFontAsset;
        turnOrderLabel.fontSize = 22f;
        turnOrderLabel.color = Color.white;
        turnOrderLabel.alignment = TextAlignmentOptions.TopLeft;
        turnOrderLabel.textWrappingMode = TextWrappingModes.NoWrap;
        turnOrderLabel.richText = true;
        turnOrderLabel.raycastTarget = false;
        turnOrderLabel.text = turnOrderTitle + "\nWaiting for battle...";
        CreateFallbackTurnOrderArrow();
    }

    private void CreateFallbackTurnOrderArrow()
    {
        if (turnOrderArrowImage != null || turnOrderPanel == null) return;
        GameObject arrowObject = new GameObject("TurnOrderArrow", typeof(RectTransform), typeof(Image));
        arrowObject.transform.SetParent(turnOrderPanel, false);
        turnOrderArrowImage = arrowObject.GetComponent<Image>();
        turnOrderArrowImage.raycastTarget = false;
        RectTransform arrowRect = arrowObject.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(0f, 1f);
        arrowRect.anchorMax = new Vector2(0f, 1f);
        arrowRect.pivot = new Vector2(0.5f, 0.5f);
        arrowRect.sizeDelta = new Vector2(24f, 24f);
    }

    private void UpdateTurnOrderDisplay()
    {
        if (turnOrderLabel == null) return;
        string headerColor = UnityEngine.ColorUtility.ToHtmlStringRGB(turnOrderLabel.color);
        System.Text.StringBuilder text = new System.Text.StringBuilder("<b><color=#" + headerColor + ">" + turnOrderTitle + "</color></b>");
        int activeIndex = -1;
        int visibleRow = 0;
        for (int i = 0; i < turnOrder.Count; i++)
        {
            TurnActor actor = turnOrder[i];
            if ((actor.hero != null && !IsAlive(actor.hero)) ||
                (actor.enemy != null && !IsAlive(actor.enemy)))
                continue;

            bool isActive = i == turnIndex &&
                ((actor.hero != null && actor.hero == activeHero &&
                  (currentstate == BattleState.PlayerTurn || currentstate == BattleState.ForcedTurnEnd)) ||
                 (actor.enemy != null &&
                  (currentstate == BattleState.Busy || currentstate == BattleState.PenaltyTurn)));
            if (isActive) activeIndex = visibleRow;
            string line = actor.Name + "  <size=85%>(SPD " + actor.Speed + ")</size>";
            text.AppendLine().Append(isActive ? "<b>" + line + "</b>" : line);
            visibleRow++;
        }
        turnOrderLabel.text = text.ToString();
        if (turnOrderArrowImage != null)
        {
            turnOrderArrowImage.color = activeTurnColor;
            turnOrderArrowImage.gameObject.SetActive(activeIndex >= 0);
            if (activeIndex >= 0)
            {
                turnOrderArrowImage.rectTransform.anchoredPosition =
                    turnOrderArrowOffset + Vector2.down * (turnOrderRowSpacing * activeIndex);
            }
        }
    }

    private bool IsAlive(HeroStats hero) => hero != null && hero.gameObject.activeInHierarchy && !hero.IsDead();
    private bool IsAlive(EnemyStats enemy) => enemy != null && enemy.gameObject.activeInHierarchy && !enemy.isDead && enemy.currentHP > 0;

    private void AdvanceTurn()
    {
        if (isLevelFinished || battleAlreadyWon || CheckLoseCondition()) return;
        if (turnOrder.Count == 0) InitializeTurnOrder();
        if (turnOrder.Count == 0) return;

        int checkedActors = 0;
        do
        {
            turnIndex++;
            if (turnIndex >= turnOrder.Count)
            {
                turnIndex = 0;
                ProcessEnemyBleedEffects();
                if (battleAlreadyWon || isLevelFinished) return;
                DecayShieldOverflow(true, true);
                RebuildTurnOrder();
                turnIndex = -1;
                turnIndex++;
                UpdateTurnOrderDisplay();
                if (turnOrder.Count == 0) return;
            }
            TurnActor actor = turnOrder[turnIndex];
            if (actor.hero != null && IsAlive(actor.hero))
            {
                StartHeroTurn(actor.hero);
                return;
            }
            if (actor.enemy != null && IsAlive(actor.enemy))
            {
                activeHero = null;
                SetActiveWorldArrow(actor.enemy.currentModel != null ? actor.enemy.currentModel.transform : actor.enemy.transform);
                currentstate = BattleState.Busy;
                UpdateUI();
                UpdateTurnOrderDisplay();
                StartCoroutine(EnemyActorTurn(actor.enemy));
                return;
            }
            checkedActors++;
        } while (checkedActors <= turnOrder.Count);
    }

    private void StartHeroTurn(HeroStats hero)
    {
        activeHero = hero;
        SetActiveWorldArrow(hero.currentModel != null ? hero.currentModel.transform : hero.transform);
        currentstate = BattleState.PlayerTurn;
        if (currentActionPoint <= 0)
            currentActionPoint = maxActionPoint;
        OnAPChanged?.Invoke(currentActionPoint, maxActionPoint);
        FindFirstObjectByType<CardPanelManager>()?.RefillToMax();
        SetCardsInteractable(true);
        if (notEnoughAPPanel != null) notEnoughAPPanel.SetActive(false);
        if (battleLog != null) battleLog.AddLog("--- " + hero.heroType.heroName + "'s Turn (Speed " + hero.Speed + ") ---");
        UpdateUI();
        UpdateTurnOrderDisplay();
    }

    void SetEnemiesActive(bool state)
    {
        foreach (Transform child in enemyController.transform)
        {
            child.gameObject.SetActive(state);
        }
    }

    //Part of Debug Damage Button, Do 5 damage to all active enemies
    /*public void AttackAllEnemies()
    {
        foreach (EnemyStats enemy in enemies)
        {
            if (enemy.gameObject.activeSelf)
            {
                enemy.TakeDamage(damage);
            }
        }
    }*/

    public void DealDamageToAll(int damage)
    {
        foreach (EnemyStats enemy in enemies)
        {
            if (enemy != null && !enemy.isDead)
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    public void StartPlayerTurn()
    {
        if (turnOrder.Count == 0) InitializeTurnOrder();
        AdvanceTurn();
    }

    public void endPlayersTurn()
    {
        EndPlayersTurnInternal(false, false);
    }

    private void EndPlayersTurnInternal(bool automatic, bool transitionDelayElapsed)
    {
        // Prevent ending the turn during required tutorial card interactions.
        TutorialBattleController tutorial = automatic ? null : FindFirstObjectByType<TutorialBattleController>();
        if (tutorial != null && tutorial.BlockEndTurn)
        {
            Debug.Log("Tutorial: End Turn is currently blocked.");
            return;
        }

        if ((currentstate != BattleState.PlayerTurn &&
             !(automatic && currentstate == BattleState.ForcedTurnEnd)) || isSelectingTarget)
        {
            Debug.Log("Cannot end turn while selecting a target.");
            return;
        }

        // Prevent another enemy turn from being started.
        currentstate = BattleState.Busy;
        if (notEnoughAPPanel != null) notEnoughAPPanel.SetActive(false);

        // Decay hero shield overflow at the end of the player's turn.
        if (activeHero != null) activeHero.DecayShieldOverflow();

        UpdateUI();

        Debug.Log("Advancing to next turn actor");
        if (transitionDelayElapsed) AdvanceTurn();
        else StartCoroutine(AdvanceTurnAfterDelay());
    }

    public void StartQuizPenaltyTurn()
    {
        if (currentstate != BattleState.PlayerTurn ||
            isLevelFinished)
        {
            return;
        }

        currentstate = BattleState.PenaltyTurn;
        SetCardsInteractable(false);
        if (penaltyTurnPanel != null) penaltyTurnPanel.SetActive(true);

        UpdateUI();
        UpdateTurnOrderDisplay();

        StartCoroutine(EnemyPenaltyTurn());
    }

    private IEnumerator EnemyActorTurn(EnemyStats enemy)
    {
        if (IsAlive(enemy))
        {
            EnemyAction action = ChooseEnemyAction(enemy) ?? new EnemyAction { actionType = EnemyActionType.Attack, value = 0 };
            ExecuteEnemyAction(enemy, action);
        }
        if (CheckLoseCondition()) { HandleBattleLost(); yield break; }
        if (IsAlive(enemy)) enemy.DecayShieldOverflow();
        if (turnTransitionDelay > 0f) yield return new WaitForSecondsRealtime(turnTransitionDelay);
        AdvanceTurn();
    }

    private IEnumerator AdvanceTurnAfterDelay()
    {
        if (turnTransitionDelay > 0f) yield return new WaitForSecondsRealtime(turnTransitionDelay);
        AdvanceTurn();
    }

    private IEnumerator EnemyPenaltyTurn()
    {
        if (battleLog != null) battleLog.AddLog("--- Enemy Penalty Turn ---");
        bool acted = false;
        int resumeTurnIndex = turnIndex;
        foreach (EnemyStats enemy in enemies)
        {
            if (!IsAlive(enemy)) continue;
            if (turnTransitionDelay > 0f)
                yield return new WaitForSecondsRealtime(turnTransitionDelay);
            int penaltyActorIndex = turnOrder.FindIndex(actor => actor.enemy == enemy);
            if (penaltyActorIndex >= 0) turnIndex = penaltyActorIndex;
            UpdateTurnOrderDisplay();
            SetActiveWorldArrow(enemy.currentModel != null ? enemy.currentModel.transform : enemy.transform);
            EnemyAction action = ChooseEnemyAction(enemy) ?? new EnemyAction { actionType = EnemyActionType.Attack, value = 0 };
            ExecuteEnemyAction(enemy, action);
            if (CheckLoseCondition()) { HandleBattleLost(); yield break; }
            acted = true;
        }
        if (acted && turnTransitionDelay > 0f)
            yield return new WaitForSecondsRealtime(turnTransitionDelay);
        turnIndex = resumeTurnIndex;
        if (penaltyTurnPanel != null) penaltyTurnPanel.SetActive(false);
        currentstate = BattleState.PlayerTurn;
        SetCardsInteractable(currentActionPoint > 0);
        if (IsAlive(activeHero))
            SetActiveWorldArrow(activeHero.currentModel != null ? activeHero.currentModel.transform : activeHero.transform);
        UpdateUI();
        if (battleLog != null && activeHero != null) battleLog.AddLog(activeHero.heroType.heroName + " resumes with " + currentActionPoint + " AP.");
    }

    IEnumerator EnemyTurn(bool isPenaltyTurn = false)
    {
        Debug.Log("EnemyTurn Started");

        currentstate = BattleState.Busy;

        if (battleLog != null)
        {
            battleLog.AddLog(isPenaltyTurn ? "--- Enemy Penalty Turn ---" : "--- Enemy Turn ---");
        }

        // Bleed activates only when the player normally ends their turn.
        if (!isPenaltyTurn)
        {
            ProcessEnemyBleedEffects();

            // Allow death animations to finish before enemy actions.
            yield return new WaitForSecondsRealtime(1f);

            // Bleed may have defeated every enemy.
            if (battleAlreadyWon || isLevelFinished || currentstate == BattleState.Win)
            {
                yield break;
            }
        }

        yield return new WaitForSecondsRealtime(1f);

        foreach (EnemyStats enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.isDead || enemy.currentHP <= 0)
            {
                continue;
            }

            if (enemy != null)
            {
                Debug.Log(enemy.name + " | Active: " + enemy.gameObject.activeInHierarchy + " | Dead: " + enemy.isDead + " | HP: " + enemy.currentHP);
            }

            EnemyAction action = ChooseEnemyAction(enemy);

            if (action == null)
            {
                action = new EnemyAction
                {
                    actionType = EnemyActionType.Attack,
                    value = 0
                };
            }

            Debug.Log("Enemy " + enemy.name + " uses: " + action.actionType);

            ExecuteEnemyAction(enemy, action);

            // Stop immediately if the last hero was defeated.
            if (CheckLoseCondition())
            {
                HandleBattleLost();
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.5f);
        }

        yield return new WaitForSecondsRealtime(1f);

        if (CheckLoseCondition())
        {
            Debug.Log("You Lost");
            HandleBattleLost();
            yield break;
        }

        if (isPenaltyTurn)
        {
            // Resume the existing player turn without resetting resources.
            currentstate = BattleState.PlayerTurn;

            UpdateUI();

            Debug.Log(
                "Enemy penalty turn finished. " +
                "Player resumes with " + currentActionPoint + " AP."
            );
        }

        else
        {
            // Decay enemy shield overflow after their normal turn.
            DecayShieldOverflow(false, true);

            // Normal enemy turn: start a fresh player turn.
            StartPlayerTurn();
        }
    }


    void ExecuteEnemyAction(EnemyStats enemy, EnemyAction action)
    {
        string enemyName = enemy.DisplayName;

        switch (action.actionType)
        {
            case EnemyActionType.Attack:
                HeroStats target = GetRandomAliveHero();

                if (target != null)
                {
                    int baseDamage = enemy.attack;
                    int actionBonus = action.value;

                    int finalDamage = enemy.GetModifiedActionValue(Mathf.Max(0, baseDamage + actionBonus));

                    int previousHP = target.currentHP;
                    int previousShield = target.shield;

                    enemy.PlayAnimation(CharacterAnimationType.Attack);
                    target.TakeDamage(finalDamage);

                    int hpDamage = Mathf.Max(0, previousHP - target.currentHP);

                    int shieldDamage = Mathf.Max(0, previousShield - target.shield);

                    if (battleLog != null)
                    {
                        string targetName = target.heroType.heroName;

                        if (shieldDamage > 0 && hpDamage > 0)
                        {
                            battleLog.AddLog($"{enemyName} attacked {targetName}, " + $"removing {shieldDamage} Shield and " +
                                $"dealing {hpDamage} HP damage.");
                        }

                        else if (shieldDamage > 0)
                        {
                            battleLog.AddLog($"{enemyName} attacked {targetName}, " +$"removing {shieldDamage} Shield.");
                        }

                        else
                        {
                            battleLog.AddLog($"{enemyName} attacked {targetName} " + $"for {hpDamage} damage.");
                        }
                    }

                    Debug.Log(enemy.name + " attacked for " + finalDamage + " damage");
                }
                break;

            case EnemyActionType.Shield:
                EnemyStats shieldTarget = SelectEnemyAllyTarget(EnemyActionType.Shield);
                
                //Temp Debug
                if (shieldTarget != null)
                {
                    Debug.Log(
                        "[Enemy AI EXECUTED] " +
                        enemy.DisplayName +
                        " used SHIELD on " +
                        shieldTarget.DisplayName +
                        " (" + shieldTarget.gameObject.name + ")" +
                        " | Target HP: " + shieldTarget.currentHP +
                        " | Target Shield Before: " + shieldTarget.shield
                    );
                }

                if (shieldTarget != null)
                {
                    int finalShield = enemy.GetModifiedActionValue(action.value);

                    int previousShield = shieldTarget.shield;

                    enemy.PlayAnimation(CharacterAnimationType.Action);
                    shieldTarget.AddShield(finalShield);

                    int actualShield = shieldTarget.shield - previousShield;

                    if (battleLog != null)
                    {
                        battleLog.AddLog($"{enemyName} used Shield, granting " + $"{actualShield} Shield to " + $"{shieldTarget.DisplayName}.");
                    }
                }
                break;

            case EnemyActionType.Heal:
                EnemyStats healTarget = SelectEnemyAllyTarget(EnemyActionType.Heal);

                if (healTarget != null)
                {
                    int finalHeal = enemy.GetModifiedActionValue(action.value);

                    int previousHP = healTarget.currentHP;

                    enemy.PlayAnimation(CharacterAnimationType.Action);
                    healTarget.Heal(finalHeal);

                    int actualHeal = healTarget.currentHP - previousHP;

                    if (battleLog != null)
                    {
                        battleLog.AddLog($"{enemyName} used Heal, restoring " +$"{actualHeal} HP to " + $"{healTarget.DisplayName}.");
                    }
                }
                break;

            case EnemyActionType.Buff:
                EnemyStats buffTarget = SelectEnemyAllyTarget(EnemyActionType.Buff);

                if (buffTarget != null)
                {
                    int buffAmount = Mathf.Abs(action.value);

                    enemy.PlayAnimation(CharacterAnimationType.Action);
                    buffTarget.AddActionModifier(buffAmount);

                    if (battleLog != null)
                    {
                        battleLog.AddLog($"{enemyName} used Buff, increasing " + $"{buffTarget.DisplayName}'s " +
                            $"next action value by {buffAmount}.");
                    }

                    Debug.Log(enemy.name + " buffed " + buffTarget.name + " by " + action.value);
                }
                break;

            case EnemyActionType.Debuff:
                int debuffAmount = Mathf.Abs(action.value);

                enemy.PlayAnimation(CharacterAnimationType.Action);
                AddPlayerModifier(-debuffAmount);

                if (battleLog != null)
                {
                    battleLog.AddLog($"{enemyName} used Debuff, reducing " + $"the player's next action value by {debuffAmount}.");
                }

                Debug.Log(enemy.name +" debuffed the player team by " + action.value);
                break;
        }
    }

    private EnemyAction ChooseEnemyAction(EnemyStats enemy)
    {
        if (enemy == null || enemy.action == null || enemy.action.Length == 0)
        {
            return null;
        }

        List<EnemyAction> validActions = new List<EnemyAction>();
        List<float> weights = new List<float>();

        float totalWeight = 0f;

        foreach (EnemyAction action in enemy.action)
        {
            if (action == null)
            {
                continue;
            }

            if ((action.actionType == EnemyActionType.Buff || action.actionType == EnemyActionType.Debuff) && action.value == 0)
            {
                continue;
            }

            float weight = 0f;

            switch (action.actionType)
            {
                case EnemyActionType.Attack:
                    weight = enemy.enemyType.attackWeight;
                    break;

                case EnemyActionType.Heal:
                    weight = enemy.enemyType.healWeight;
                    break;

                case EnemyActionType.Shield:
                    weight = enemy.enemyType.shieldWeight;
                    break;

                case EnemyActionType.Buff:
                    weight = enemy.enemyType.buffWeight;
                    break;

                case EnemyActionType.Debuff:
                    weight = enemy.enemyType.debuffWeight;
                    break;
            }

            if (weight <= 0f)
            {
                continue;
            }

            switch (action.actionType)
            {
                case EnemyActionType.Attack:
                    weight = 5f;
                    break;

                case EnemyActionType.Heal:
                    EnemyStats healTarget = SelectEnemyAllyTarget(EnemyActionType.Heal);

                    // No injured ally means healing is unnecessary.
                    if (healTarget == null)
                    {
                        continue;
                    }

                    float healthRatio = (float)healTarget.currentHP / Mathf.Max(1, healTarget.enemyType.maxHp);

                    if (healthRatio <= 0.25f)
                    {
                        weight *= 4f;
                    }

                    else if (healthRatio <= 0.50f)
                    {
                        weight *= 2.5f;
                    }

                    else
                    {
                        weight *= 1f;
                    }

                    break;

                case EnemyActionType.Shield:
                    EnemyStats shieldTarget = SelectEnemyAllyTarget(EnemyActionType.Shield);

                    if (shieldTarget == null)
                    {
                        continue;
                    }
                    float shieldHealthRatio = (float)shieldTarget.currentHP / Mathf.Max(1, shieldTarget.enemyType.maxHp);

                    if (shieldHealthRatio <= 0.30f)
                    {
                        // Critically injured ally: prioritize protection.
                        weight *= 3f;
                    }

                    else if (shieldTarget.shield <= 0)
                    {
                        // No protection available.
                        weight *= 2f;
                    }

                    else if (shieldTarget.shield < 5)
                    {
                        // Some protection, but still vulnerable.
                        weight *= 1f;
                    }

                    else
                    {
                        // Already protected: Shield is less attractive.
                        weight *= 0.35f;
                    }

                    break;

                case EnemyActionType.Buff:
                    EnemyStats buffTarget = SelectEnemyAllyTarget(EnemyActionType.Buff);

                    if (buffTarget == null)
                    {
                        continue;
                    }

                    // Buff is useful, but generally less urgent than
                    // healing or protecting a critically injured ally.
                    weight = 4f;
                    break;

                case EnemyActionType.Debuff:
                    if (playerModifier == null)
                    {
                        continue;
                    }

                    int pendingPlayerModifier = playerModifier.PendingModifier;

                    if (pendingPlayerModifier < 0)
                    {
                        // Player is already debuffed.
                        // Allow another debuff, but make it less likely.
                        weight *= 0.25f;
                    }

                    else if (pendingPlayerModifier > 0)
                    {
                        // Player has a positive buff.
                        // Debuff becomes more useful to counter it.
                        weight *= 1.75f;
                    }

                    else
                    {
                        // No active modifier.
                        weight *= 1f;
                    }

                    break;
            }

            if (weight <= 0f)
            {
                continue;
            }

            validActions.Add(action);
            weights.Add(weight);
            totalWeight += weight;
        }

        if (validActions.Count == 0)
        {
            Debug.LogWarning("Enemy AI: No valid actions for " + enemy.name);

            return null;
        }

        float roll = Random.Range(0f, totalWeight);

        for (int i = 0; i < validActions.Count; i++)
        {
            roll -= weights[i];

            if (roll <= 0f)
            {
                Debug.Log("Enemy AI: " + enemy.name + " selected " + validActions[i].actionType + " | Weight: " + weights[i]);

                return validActions[i];
            }
        }

        return validActions[validActions.Count - 1];
    }

    HeroStats GetRandomAliveHero()
    {
        List<HeroStats> alive = new List<HeroStats>();

        foreach (HeroStats hero in heroes)
        {
            if (hero != null && hero.gameObject.activeInHierarchy && !hero.IsDead())
            {
                alive.Add(hero);
            }
        }

        if (alive.Count == 0)
        {
            return null;
        }

        return alive[Random.Range(0, alive.Count)];
    }


    private HeroStats GetHeroForCard(CardType card)
    {
        if (card == null || heroes == null)
        {
            return null;
        }

        foreach (HeroStats hero in heroes)
        {
            if (hero == null || hero.heroType == null || hero.heroType.customCards == null)
            {
                continue;
            }

            foreach (CardType customCard in hero.heroType.customCards)
            {
                if (customCard != card)
                {
                    continue;
                }

                // Custom card owner is still alive.
                if (hero.gameObject.activeInHierarchy && !hero.IsDead())
                {
                    Debug.Log("CUSTOM CARD: " + card.cardName + " | Assigned Hero: " + hero.heroType.heroName);

                    return hero;
                }

                // Custom card owner has been defeated.
                HeroStats replacementHero = GetRandomAliveHero();

                if (replacementHero != null)
                {
                    Debug.Log("CUSTOM CARD: " + card.cardName + " | Original Hero Defeated: " + hero.heroType.heroName + " | Replacement Hero: " +
                        replacementHero.heroType.heroName);
                }

                return replacementHero;
            }
        }

        // Shared cards are played by the hero whose turn is active.
        return IsAlive(activeHero) ? activeHero : null;
    }


    private EnemyStats SelectEnemyAllyTarget(EnemyActionType actionType)
    {
        EnemyStats bestTarget = null;

        foreach (EnemyStats enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.isDead || enemy.currentHP <= 0 || enemy.enemyType == null)
            {
                continue;
            }

            switch (actionType)
            {
                case EnemyActionType.Heal:
                    // Only consider injured enemies.
                    if (enemy.currentHP >= enemy.enemyType.maxHp)
                    {
                        continue;
                    }

                    // Prioritize the lowest HP percentage.
                    if (bestTarget == null || (float)enemy.currentHP / Mathf.Max(1, enemy.enemyType.maxHp) <
                        (float)bestTarget.currentHP / Mathf.Max(1, bestTarget.enemyType.maxHp))
                    {
                        bestTarget = enemy;
                    }

                    break;

                case EnemyActionType.Shield:
                    // Prioritize enemies with the least combined HP and Shield.
                    if (bestTarget == null || enemy.currentHP + enemy.shield < bestTarget.currentHP + bestTarget.shield)
                    {
                        bestTarget = enemy;
                    }
                    break;

                case EnemyActionType.Buff:
                    // Avoid allies that already have a positive buff.
                    if (enemy.PendingActionModifier > 0)
                    {
                        continue;
                    }

                    // Prefer allies with the highest attack.
                    if (bestTarget == null || enemy.attack > bestTarget.attack)
                    {
                        bestTarget = enemy;
                    }
                    break;
            }
        }

        //Temp Debug
        if (bestTarget != null)
        {
            Debug.Log(
                "[Enemy AI Targeting] " +
                "Action: " + actionType +
                " | Selected: " + bestTarget.DisplayName +
                " (" + bestTarget.gameObject.name + ")" +
                " | HP: " + bestTarget.currentHP +
                "/" + bestTarget.enemyType.maxHp +
                " | Shield: " + bestTarget.shield +
                " | Pending Modifier: " + bestTarget.PendingActionModifier
            );
        }
        else
        {
            Debug.Log(
                "[Enemy AI Targeting] " +
                "Action: " + actionType +
                " | No eligible target found."
            );
        }

        return bestTarget;
    }


    EnemyStats GetRandomAliveEnemy()
    {
        List<EnemyStats> alive = new List<EnemyStats>();

        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.isDead && enemy.currentHP > 0)
            {
                alive.Add(enemy);
            }
        }

        if (alive.Count == 0)
            return null;

        return alive[Random.Range(0, alive.Count)];
    }


    bool CheckLoseCondition()
    {
        if (heroes == null || heroes.Length == 0)
        {
            return false;
        }

        foreach (HeroStats hero in heroes)
        {
            if (hero != null && hero.gameObject.activeInHierarchy && !hero.IsDead())
            {
                return false;
            }
        }

        return true;
    }

    private void HandleBattleLost()
    {
        if (currentstate == BattleState.Lose || currentstate == BattleState.Win || isLevelFinished)
        {
            return;
        }

        currentstate = BattleState.Lose;
        SetActiveWorldArrow(null);
        if (penaltyTurnPanel != null) penaltyTurnPanel.SetActive(false);

        isSelectingTarget = false;
        UpdateUI();
        pendingCard = null;
        pendingHero = null;

        if (selectTargetPanel != null)
        {
            selectTargetPanel.SetActive(false);
        }

        if (battleLog != null)
        {
            battleLog.AddLog("All heroes have fallen. Battle Lost.");
        }

        Debug.Log("BattleControl: All heroes defeated. Battle Lost.");

        OnBattleLost?.Invoke();
    }

    public void OnEnemyKilled()
    {
        if (isLevelFinished)
        {
            return;
        }

        aliveEnemies = 0;

        foreach (var e in enemies)
        {
            if (e != null && e.gameObject.activeInHierarchy && !e.isDead)
            {
                aliveEnemies++;
            }
        }

        Debug.Log("Enemies Left: " + aliveEnemies);

        if (aliveEnemies <= 0 && !battleAlreadyWon)
        {
            battleAlreadyWon = true;
            SetActiveWorldArrow(null);
            Debug.Log("Battle Won");

            nextbattletriggered = true;

            OnBattleWon?.Invoke();

            if (!nextbattletriggered)
            {
                nextbattletriggered = true;
            }
        }
    }

    public bool TrySpendAP(int cost)
    {
        if (currentstate != BattleState.PlayerTurn)
        {
            Debug.Log("Cannot play cards outside the player turn.");
            return false;
        }

        if (currentActionPoint < cost)
        {
            Debug.Log("Not Enough AP");

            if (notEnoughAPPanel != null)
            {
                StopCoroutine(nameof(ShowAPWarning));
                StartCoroutine(nameof(ShowAPWarning));
            }

            return false;
        }

        currentActionPoint -= cost;
        OnAPChanged?.Invoke(currentActionPoint, maxActionPoint);
        return true;
    }

    public void AddPlayerModifier(int amount)
    {
        if (playerModifier != null)
        {
            playerModifier.AddModifier(amount);
        }
    }

    public int GetModifiedPlayerValue(int baseValue)
    {
        if (playerModifier != null)
        {
            return playerModifier.ConsumedModifier(baseValue);
        }

        return Mathf.Max(0, baseValue);
    }

    private void ExecuteCardEffect(CardType card, HeroStats actingHero, HeroStats targetHero = null, EnemyStats targetEnemy = null, int? calculatedValue = null, bool suppressLog = false)
    {
        if (card == null || actingHero == null)
        {
            Debug.LogWarning("Cannot Execute card effect: Missing Card or Acting Hero");
            return;
        }

        int value = calculatedValue ?? Mathf.Max(0, card.damage);

        switch (card.effectType)
        {
            case CardEffectType.Damage:
                if (targetEnemy != null && !targetEnemy.isDead)
                {
                    int finalDamage = calculatedValue ?? GetModifiedPlayerValue(value);

                    // Record the target's stats before damage.
                    int previousHP = targetEnemy.currentHP;
                    int previousShield = targetEnemy.shield;

                    targetEnemy.TakeDamage(finalDamage);

                    // Calculate the actual damage applied.
                    int hpDamage = previousHP - targetEnemy.currentHP;
                    int shieldDamage = previousShield - targetEnemy.shield;

                    if (battleLog != null && !suppressLog)
                    {
                        string targetName = targetEnemy.DisplayName;

                        if (shieldDamage > 0 && hpDamage > 0)
                        {
                            battleLog.AddLog($"You played \"{card.cardName}\", removing " +
                                $"{shieldDamage} Shield and dealing {hpDamage} HP damage to {targetName}."
                            );
                        }

                        else if (shieldDamage > 0)
                        {
                            battleLog.AddLog($"You played \"{card.cardName}\", removing " + $"{shieldDamage} Shield from {targetName}.");
                        }

                        else
                        {
                            battleLog.AddLog($"You played \"{card.cardName}\", dealing " + $"{hpDamage} damage to {targetName}.");
                        }
                    }

                    Debug.Log(actingHero.name + " dealt " + finalDamage +" damage to " + targetEnemy.name);
                }
                break;

            case CardEffectType.Piercing:
                if (targetEnemy != null && !targetEnemy.isDead)
                {
                    int finalDamage = calculatedValue ?? GetModifiedPlayerValue(value);

                    int previousHP = targetEnemy.currentHP;

                    targetEnemy.TakePiercingDamage(finalDamage);

                    int actualDamage = previousHP - targetEnemy.currentHP;

                    if (battleLog != null && !suppressLog)
                    {
                        battleLog.AddLog($"You played \"{card.cardName}\", " + $"dealing {actualDamage} Piercing Damage " +
                            $"to {targetEnemy.DisplayName}, " +
                            $"ignoring Shield."
                        );
                    }

                    Debug.Log( actingHero.name + " dealt " + actualDamage + " Piercing Damage to " + targetEnemy.name);
                }
                break;

            case CardEffectType.Bleed:
                if (targetEnemy != null &&!targetEnemy.isDead)
                {
                    int finalBleed = calculatedValue ?? GetModifiedPlayerValue(value);

                    finalBleed = Mathf.Max(0, finalBleed);

                    targetEnemy.AddBleed(finalBleed);

                    if (battleLog != null && !suppressLog)
                    {
                        battleLog.AddLog($"You played \"{card.cardName}\", " + $"applying {finalBleed} Bleed to " + $"{targetEnemy.DisplayName}.");
                    }

                    Debug.Log(actingHero.name + " applied " + finalBleed + " Bleed to " +targetEnemy.name);
                }
                break;

            case CardEffectType.Heal:
                if (targetHero != null && !targetHero.IsDead())
                {
                    int finalHeal = calculatedValue ?? GetModifiedPlayerValue(value);

                    int previousHP = targetHero.currentHP;

                    targetHero.Heal(finalHeal);

                    int actualHeal = targetHero.currentHP - previousHP;

                    if (battleLog != null && !suppressLog)
                    {
                        battleLog.AddLog($"You played \"{card.cardName}\", restoring " +
                            $"{actualHeal} HP to {targetHero.heroType.heroName}.");
                    }
                }
                break;

            case CardEffectType.Shield:
                if (targetHero != null && !targetHero.IsDead())
                {
                    int finalShield = calculatedValue ?? GetModifiedPlayerValue(value);

                    int previousShield = targetHero.shield;

                    targetHero.Shield(finalShield);

                    int actualShield = targetHero.shield - previousShield;

                    if (battleLog != null && !suppressLog)
                    {
                        battleLog.AddLog($"You played \"{card.cardName}\", granting " +
                            $"{actualShield} Shield to {targetHero.heroType.heroName}.");
                    }
                }
                break;

            case CardEffectType.Buff:
                int buffAmount = Mathf.Abs(value);

                AddPlayerModifier(buffAmount);

                if (battleLog != null && !suppressLog)
                {
                    battleLog.AddLog($"You played \"{card.cardName}\", increasing " + $"your team's next action value by {buffAmount}.");
                }
                break;

            case CardEffectType.Debuff:
                if (targetEnemy != null && !targetEnemy.isDead)
                {
                    int debuffAmount = Mathf.Abs(value);

                    targetEnemy.AddActionModifier(-debuffAmount);

                    if (battleLog != null && !suppressLog)
                    {
                        battleLog.AddLog($"You played \"{card.cardName}\", reducing " +
                            $"{targetEnemy.DisplayName}'s next action value by {debuffAmount}.");
                    }
                }
                break;
        }

        UpdateTurnOrderDisplay();
    }

    private void ProcessEnemyBleedEffects()
    {
        if (enemies == null)
            return;

        foreach (EnemyStats enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.isDead || enemy.BleedValue <= 0)
            {
                continue;
            }

            int damage = enemy.BleedValue;
            int previousHP = enemy.currentHP;

            enemy.ProcessBleed();

            int actualDamage = previousHP - enemy.currentHP;

            if (battleLog != null && actualDamage > 0)
            {
                battleLog.AddLog( $"{enemy.DisplayName} suffered " + $"{actualDamage} Bleed Damage, ignoring Shield. " + $"Remaining Bleed: {enemy.BleedValue}."
                );
            }
        }
    }

    private void DecayShieldOverflow(bool decayHeroes, bool decayEnemies)
    {
        if (decayHeroes && heroes != null)
        {
            foreach (HeroStats hero in heroes)
            {
                if (hero == null || !hero.gameObject.activeInHierarchy || hero.IsDead())
                {
                    continue;
                }

                hero.DecayShieldOverflow();
            }
        }

        if (decayEnemies && enemies != null)
        {
            foreach (EnemyStats enemy in enemies)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.isDead)
                {
                    continue;
                }

                enemy.DecayShieldOverflow();
            }
        }
    }

    //Handles Modifier Usage for all 3 Targeting Types 
    private void ModifierEffectHandler(CardType card, HeroStats actingHero)
    {
        if (card == null || actingHero == null)
        {
            return;
        }

        if (card.effectType == CardEffectType.Buff)
        {
            BeginCardAnimation(actingHero, CharacterAnimationType.Action);

            int buffAmount = Mathf.Abs(card.damage);

            AddPlayerModifier(buffAmount);

            if (battleLog != null)
            {
                battleLog.AddLog($"You played \"{card.cardName}\", increasing " + $"your team's next action value by {buffAmount}.");
            }

            return;
        }

        bool consumeModifier =
            card.effectType == CardEffectType.Damage ||
            card.effectType == CardEffectType.Piercing ||
            card.effectType == CardEffectType.Heal ||
            card.effectType == CardEffectType.Shield;

        int finalValue = Mathf.Max(0, card.damage);

        //Consume the acting Hero's Modifier only for numerical values
        //Buff and Debuff cards do not consume the buffed values
        if (consumeModifier)
        {
            finalValue = GetModifiedPlayerValue(finalValue);
        }

        CharacterAnimationType animation =
            card.effectType == CardEffectType.Damage ||
            card.effectType == CardEffectType.Piercing ||
            card.effectType == CardEffectType.Bleed ||
            card.effectType == CardEffectType.Debuff ?
            CharacterAnimationType.Attack : CharacterAnimationType.Action;

        switch (card.targetType)
        {

            case TargetType.AllEnemies:
                BeginCardAnimation(actingHero, animation);

                int enemiesAffected = 0;

                foreach (EnemyStats enemy in enemies)
                {
                    if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.isDead)
                    {
                        ExecuteCardEffect(card, actingHero, targetEnemy: enemy, calculatedValue: finalValue,suppressLog: true);

                        enemiesAffected++;
                    }
                }

                if (battleLog != null && enemiesAffected > 0)
                {
                    battleLog.AddLog($"You played \"{card.cardName}\", " + $"{GetCardEffectLogDescription(card, finalValue)} " + "to all enemies.");
                }
                break;

            case TargetType.AllHeroes:
                BeginCardAnimation(actingHero, animation);

                int heroesAffected = 0;

                foreach (HeroStats hero in heroes)
                {
                    if (hero != null &&hero.gameObject.activeInHierarchy && !hero.IsDead())
                    {
                        ExecuteCardEffect(card, actingHero, targetHero: hero, calculatedValue: finalValue, suppressLog: true);

                        heroesAffected++;
                    }
                }

                if (battleLog != null && heroesAffected > 0)
                {
                    battleLog.AddLog($"You played \"{card.cardName}\", " + $"{GetCardEffectLogDescription(card, finalValue)} " + "to all heroes.");
                }
                break;


            case TargetType.Self:
                BeginCardAnimation(actingHero, animation);

                ExecuteCardEffect(card, actingHero, targetHero: actingHero, calculatedValue: finalValue);

                break;
        }
    }
    public bool HandleCardPlay(CardType card, CardUI cardUI)
    {
        Debug.Log("HandleCardPlay CALLED with: " + card.cardName + " | " + card.targetType);

        HeroStats cardHero = GetHeroForCard(card);
        if (cardHero == null || cardHero != activeHero)
        {
            Debug.Log("That hero's card can only be used on their turn.");
            return false;
        }

        if (!TrySpendAP(card.cost))
        {
            return false;
        }

        OnCardPlayAccepted?.Invoke();

        if (battleLog != null && card.quizCard)
        {
            battleLog.AddLog($"You played \"{card.cardName}\".");
        }

        if (card.quizCard)
        {
            QuizManager quizManager = FindFirstObjectByType<QuizManager>();

            if (quizManager != null) {
                QuizQuestion q = quizManager.GetRandomQuestions();
                quizManager.StartQuiz(q, card, this);
            }

            Destroy(cardUI.gameObject);
            return true;
        }

        if (card.targetType == TargetType.AllEnemies || card.targetType == TargetType.AllHeroes || card.targetType == TargetType.Self)
        {
            HeroStats selectedHero = GetHeroForCard(card);

            ModifierEffectHandler(card, selectedHero);

            Destroy(cardUI.gameObject);
            return true;
        }

        if (card.targetType == TargetType.SingleTarget)
        {
            Debug.Log("Select a Target");

            isSelectingTarget = true;
            pendingCard = card;
            pendingHero = GetHeroForCard(card);

            UpdateUI();

            cardPanel.SetActive(false);

            if (selectTargetPanel != null)
            {
                selectTargetPanel.SetActive(true);
            }

            /*foreach (EnemyStats e in enemies)
            {
                if (e != null && !e.isDead)
                {
                    e.SetHighlighted(true);
                }
            }*/

            Destroy(cardUI.gameObject);
            return true;
        }

        return false;
    }

    public void ResolveQuizCard(CardType card)
    {
        if(card == null)
        {
            return;
        }

        if(card.targetType == TargetType.AllEnemies || card.targetType == TargetType.AllHeroes || card.targetType == TargetType.Self)
        {
            HeroStats selectedHero = GetHeroForCard(card);

            ModifierEffectHandler(card, selectedHero);
        }

        else if (card.targetType == TargetType.SingleTarget)
        {
            Debug.Log("Quiz Completed! Select an enemy target.");

            pendingCard = card;
            pendingHero = GetHeroForCard(card);
            isSelectingTarget = true;

            UpdateUI();

            if (cardPanel != null)
            {
                cardPanel.SetActive(false);
            }

            if (selectTargetPanel != null)
            {
                selectTargetPanel.SetActive(true);
            }
        }
    }

    public void SelectEnemyTarget(EnemyStats enemy)
    {
        if (!isSelectingTarget || pendingCard == null || pendingHero == null || enemy == null ||enemy.IsDead())
        {
            return;
        }

        // Only Damage, Piercing, Bleed, and Debuff may target enemies.
        if (pendingCard.effectType != CardEffectType.Damage &&
            pendingCard.effectType != CardEffectType.Piercing &&
            pendingCard.effectType != CardEffectType.Debuff &&
            pendingCard.effectType != CardEffectType.Bleed)
        {
            return;
        }

        ResolveSingleTarget(enemy, null);
    }

    public void SelectHeroTarget(HeroStats hero)
    {
        if (!isSelectingTarget ||
            pendingCard == null ||
            pendingHero == null ||
            hero == null ||
            hero.IsDead())
        {
            return;
        }

        // Only Heal and Shield may target heroes.
        if (pendingCard.effectType != CardEffectType.Heal && pendingCard.effectType != CardEffectType.Shield)
        {
            return;
        }

        ResolveSingleTarget(null, hero);
    }

    private void ResolveSingleTarget(EnemyStats targetEnemy, HeroStats targetHero)
    {
        CardType card = pendingCard;
        HeroStats actingHero = pendingHero;

        CharacterAnimationType animation =
            card.effectType == CardEffectType.Damage ||
            card.effectType == CardEffectType.Piercing ||
            card.effectType == CardEffectType.Debuff ||
            card.effectType == CardEffectType.Bleed 
            ? CharacterAnimationType.Attack
            : CharacterAnimationType.Action;

        BeginCardAnimation(actingHero, animation);

        // ExecuteCardEffect already applies the correct effect
        // and consumes player modifiers for numerical actions.
        ExecuteCardEffect(card, actingHero, targetHero: targetHero, targetEnemy: targetEnemy);

        isSelectingTarget = false;
        pendingCard = null;
        pendingHero = null;

        // Restore End Turn button state.
        UpdateUI();

        if (selectTargetPanel != null)
        {
            selectTargetPanel.SetActive(false);
        }

        if (cardPanel != null)
        {
            cardPanel.SetActive(true);
        }
    }

    public void StartNextBattleManually()
    {
        Debug.Log("StartNextBattleManually called. nextbattletriggered = " + nextbattletriggered);

        if (!nextbattletriggered)
        {
            return;
        }

        StartCoroutine(NextBattle());
    }

    //Depecrated Battle Win function
    /*
    bool AllEnemiesAreDead()
    {
        if(enemies == null || enemies.Length == 0)
        {
            return false;
        }
        foreach(EnemyStats enemy in enemies)
        {
            if (enemy.gameObject.activeSelf)
            {
                return false;
            }
        }
        return true;
    } */

    private void Update()
    {
        UpdateActiveWorldArrow();

        if (!autoEndTurnPending && currentstate == BattleState.PlayerTurn && currentActionPoint <= 0 &&
            !isSelectingTarget && activeCardAnimationRelay == null && !IsQuizInteractionOpen())
        {
            StartCoroutine(AutoEndTurnAtZeroAP());
        }

        //Debug Damage Button
        /*if (Keyboard.current.slashKey.wasPressedThisFrame)
        {
            AttackAllEnemies();
        }*/

        //Old Battle Progression Code
        /* if (!nextbattletriggered && allEnemiesAreDead())
        {
            nextbattletriggered = true;
            StartCoroutine(NextBattle());
        }*/

        if (isSelectingTarget && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log("Hit: " + hit.collider.name);

                EnemyStats enemy = hit.collider.GetComponentInParent<EnemyStats>();

                HeroStats hero = hit.collider.GetComponentInParent<HeroStats>();

                if (enemy != null)
                {
                    SelectEnemyTarget(enemy);
                }
                else if (hero != null)

                {
                    SelectHeroTarget(hero);
                }
            }
        }
    }

    private bool IsQuizInteractionOpen()
    {
        QuizManager quizManager = FindFirstObjectByType<QuizManager>();
        return quizManager != null &&
            ((quizManager.quizPanel != null && quizManager.quizPanel.activeInHierarchy) ||
             (quizManager.quizInfoPanel != null && quizManager.quizInfoPanel.activeInHierarchy));
    }

    private IEnumerator AutoEndTurnAtZeroAP()
    {
        autoEndTurnPending = true;
        currentstate = BattleState.ForcedTurnEnd;
        SetCardsInteractable(false);
        if (notEnoughAPPanel != null)
        {
            StopCoroutine(nameof(ShowAPWarning));
            notEnoughAPPanel.SetActive(true);
        }
        UpdateUI();
        UpdateTurnOrderDisplay();

        if (turnTransitionDelay > 0f)
            yield return new WaitForSecondsRealtime(turnTransitionDelay);

        if (currentstate == BattleState.ForcedTurnEnd && currentActionPoint <= 0 &&
            !isSelectingTarget && activeCardAnimationRelay == null && !IsQuizInteractionOpen())
        {
            EndPlayersTurnInternal(true, true);
        }
        autoEndTurnPending = false;
    }

    private void SetCardsInteractable(bool interactable)
    {
        CardUI[] cards = FindObjectsByType<CardUI>(FindObjectsSortMode.None);
        foreach (CardUI card in cards)
        {
            if (card != null && card.button != null)
                card.button.interactable = interactable;
        }
    }

    private void SetActiveWorldArrow(Transform target)
    {
        activeWorldArrowTarget = target;
        activeWorldArrowHeadTarget = target != null ? FindCharacterHead(target) : null;
        if (target == null)
        {
            if (activeWorldArrowObject != null) activeWorldArrowObject.SetActive(false);
            return;
        }

        if (activeWorldArrowObject == null)
        {
            Sprite sprite = activeCharacterArrowSprite != null
                ? activeCharacterArrowSprite
                : turnOrderArrowImage != null ? turnOrderArrowImage.sprite : null;
            if (sprite == null) return;

            activeWorldArrowObject = new GameObject("Active Character Turn Arrow");
            activeWorldArrowRenderer = activeWorldArrowObject.AddComponent<SpriteRenderer>();
            activeWorldArrowRenderer.sprite = sprite;
            activeWorldArrowRenderer.sortingOrder = short.MaxValue;
            activeWorldArrowRenderer.color = activeTurnColor;
            activeWorldArrowObject.transform.localScale = Vector3.one * activeCharacterArrowScale;
        }

        activeWorldArrowObject.SetActive(true);
        UpdateActiveWorldArrow();
    }

    private Transform FindCharacterHead(Transform characterRoot)
    {
        foreach (Animator animator in characterRoot.GetComponentsInChildren<Animator>(true))
        {
            if (animator.isHuman)
            {
                Transform humanoidHead = animator.GetBoneTransform(HumanBodyBones.Head);
                if (humanoidHead != null) return humanoidHead;
            }
        }

        Transform firstHeadMatch = null;
        foreach (Transform child in characterRoot.GetComponentsInChildren<Transform>(true))
        {
            string normalizedName = child.name.ToLowerInvariant().Replace("_", "").Replace(" ", "");
            if (normalizedName == "head" || normalizedName.EndsWith(":head") || normalizedName.EndsWith(".head"))
                return child;
            if (firstHeadMatch == null && normalizedName.Contains("head"))
                firstHeadMatch = child;
        }
        return firstHeadMatch;
    }

    private void UpdateActiveWorldArrow()
    {
        if (activeWorldArrowObject == null || activeWorldArrowTarget == null || !activeWorldArrowTarget.gameObject.activeInHierarchy)
        {
            if (activeWorldArrowObject != null) activeWorldArrowObject.SetActive(false);
            return;
        }

        Bounds bounds = new Bounds(activeWorldArrowTarget.position, Vector3.zero);
        bool foundRenderer = false;
        foreach (Renderer modelRenderer in activeWorldArrowTarget.GetComponentsInChildren<Renderer>())
        {
            if (!modelRenderer.enabled) continue;
            if (!foundRenderer) { bounds = modelRenderer.bounds; foundRenderer = true; }
            else bounds.Encapsulate(modelRenderer.bounds);
        }

        Vector3 arrowPosition = activeWorldArrowHeadTarget != null
            ? activeWorldArrowHeadTarget.position
            : activeWorldArrowTarget.position;
        float modelTop = foundRenderer ? bounds.max.y : activeWorldArrowTarget.position.y + 1.8f;
        float headTop = activeWorldArrowHeadTarget != null ? activeWorldArrowHeadTarget.position.y + 0.25f : modelTop;
        arrowPosition.y = Mathf.Max(modelTop, headTop);
        Vector3 offset = activeCharacterArrowOffset;
        offset.y = Mathf.Max(0f, offset.y);
        arrowPosition += offset;
        activeWorldArrowObject.transform.position = arrowPosition;
        activeWorldArrowRenderer.color = activeTurnColor;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 toCamera = (mainCamera.transform.position - arrowPosition).normalized;
            activeWorldArrowObject.transform.rotation =
                Quaternion.LookRotation(toCamera, mainCamera.transform.up) * Quaternion.Euler(0f, 0f, -90f);
        }
    }

    private string GetCardEffectLogDescription(CardType card, int value)
    {
        switch (card.effectType)
        {
            case CardEffectType.Damage:
                return $"dealing up to {value} damage";

            case CardEffectType.Piercing:
                return $"dealing up to {value} Piercing Damage";

            case CardEffectType.Bleed:
                return $"applying up to {value} Bleed";

            case CardEffectType.Heal:
                return $"restoring up to {value} HP";

            case CardEffectType.Shield:
                return $"granting {value} Shield";

            case CardEffectType.Buff:
                return $"increasing action values by {value}";

            case CardEffectType.Debuff:
                return $"reducing action values by {Mathf.Abs(value)}";

            default:
                return "applying its effect";
        }
    }

    void UpdateUI()
    {
        UpdateTurnOrderDisplay();
        bool canEndTurn = currentstate == BattleState.PlayerTurn && !isSelectingTarget;

        UnityEngine.UI.Button button = endButton.GetComponent<UnityEngine.UI.Button>();

        if (button != null)
        {
            button.interactable = canEndTurn;
        }
    }

    public void StartBattleSequence()
    {
        if (battleSequenceRunning || isLevelFinished)
        {
            return;
        }

        battleSequenceRunning = true;
        StartCoroutine(BattleSequence());
    }

    void SetHeroesAnimation(CharacterAnimationType type)
    {
        foreach (HeroStats hero in heroes)
        {
            if (hero != null && hero.gameObject.activeInHierarchy)
            {
                hero.PlayAnimation(type);
            }
        }
    }

    void SetEnemiesAnimation(CharacterAnimationType type)
    {
        foreach (EnemyStats enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.isDead)
            {
                enemy.PlayAnimation(type);
            }
        }
    }

    private void BeginCardAnimation(HeroStats hero, CharacterAnimationType animation)
    {
        if (hero == null)
        {
            Debug.LogWarning("Card animation: No acting hero found.");
            return;
        }

        // Stop listening to any previous card animation.
        if (activeCardAnimationRelay != null)
        {
            activeCardAnimationRelay.OnActionAnimationComplete -= HandleCardAnimationComplete;
        }

        activeCardAnimationRelay =
            hero.GetComponentInChildren<CharacterAnimationRelay>(true);

        if (activeCardAnimationRelay != null)
        {
            activeCardAnimationRelay.OnActionAnimationComplete += HandleCardAnimationComplete;
        }
        else
        {
            Debug.LogWarning("Card animation: CharacterAnimationRelay not found.");
        }

        hero.PlayAnimation(animation);
    }

    private void HandleCardAnimationComplete()
    {
        if (activeCardAnimationRelay == null)
            return;

        activeCardAnimationRelay.OnActionAnimationComplete -= HandleCardAnimationComplete;
        activeCardAnimationRelay = null;

        Debug.Log("BattleControl: Card animation completed.");

        OnCardResolutionComplete?.Invoke();
    }

    private void OnDestroy()
    {
        if (activeCardAnimationRelay != null)
        {
            activeCardAnimationRelay.OnActionAnimationComplete -= HandleCardAnimationComplete;
        }
    }

    //Starting Battle Sequence
    IEnumerator BattleSequence()
    {
        if (isLevelFinished)
        {
            yield break;
        }
        currentstate = BattleState.Busy;

        groundloop.isMoving = true;

        SetHeroesAnimation(CharacterAnimationType.Moving);

        yield return new WaitForSeconds(moveduration);

        SetHeroesAnimation(CharacterAnimationType.Neutral);

        groundloop.isMoving = false;

        SetupEnemies();

        battleNumber++;

        Debug.Log("BattleControl: Current battle number = " + battleNumber);

        StartPlayerTurn();

        if (battleNumber == 1)
        {
            OnFirstBattleStarted?.Invoke();
        }
        else if (battleNumber == 2)
        {
            Debug.Log("BattleControl: Second battle started.");
            OnSecondBattleStarted?.Invoke();
        }

        battleSequenceRunning = false;
    }

    //Subsequent Battle Sequence
    IEnumerator NextBattle()
    {
        if (isLevelFinished)
        {
            yield break;
        }

        currentstate = BattleState.Busy;

        yield return new WaitForSecondsRealtime(2f);

        groundloop.isMoving = true;

        SetHeroesAnimation(CharacterAnimationType.Moving);

        yield return new WaitForSecondsRealtime(moveduration);

        SetHeroesAnimation(CharacterAnimationType.Neutral);

        groundloop.isMoving = false;

        SetupEnemies();

        nextbattletriggered = false;

        battleNumber++;

        Debug.Log("BattleControl: Current battle number = " + battleNumber);

        StartPlayerTurn();

        if(battleNumber == 2)
        {
            Debug.Log("BattleControl: Second battle started.");
            OnSecondBattleStarted?.Invoke();
        }
    }

    IEnumerator ShowAPWarning()
    {
        notEnoughAPPanel.SetActive(true);

        yield return new WaitForSecondsRealtime(1.5f);

        notEnoughAPPanel.SetActive(false);
    }
}
