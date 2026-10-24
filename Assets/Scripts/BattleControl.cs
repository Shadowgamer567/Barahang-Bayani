//Handles Battle Logic and sequence

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

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

    public System.Action OnFirstBattleStarted;
    public System.Action OnSecondBattleStarted;

    public System.Action OnCardPlayAccepted;
    public System.Action OnCardResolutionComplete;

    private CharacterAnimationRelay activeCardAnimationRelay;

    public bool IsTargetingEnemies =>
    isSelectingTarget &&
    pendingCard != null &&
    (pendingCard.effectType == CardEffectType.Damage ||
     pendingCard.effectType == CardEffectType.Debuff);

    public bool IsTargetingHeroes =>
        isSelectingTarget &&
        pendingCard != null &&
        (pendingCard.effectType == CardEffectType.Heal ||
         pendingCard.effectType == CardEffectType.Shield);

    EnemyStats[] enemies;
    HeroStats[] heroes;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetEnemiesActive(false);
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
        Lose
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

        if (uiMain != null)
        {
            Debug.Log("Using UI Object: " + uiMain.gameObject.name);

            uiMain.GenerateEnemyPanels(enemies);
        }
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
        currentstate = BattleState.PlayerTurn;

        currentActionPoint = maxActionPoint;
        OnAPChanged?.Invoke(currentActionPoint, maxActionPoint);

        Debug.Log("AP reset to Max");

        FindFirstObjectByType<CardPanelManager>()?.RefillToMax();
    }

    public void endPlayersTurn()
    {
        // Prevent ending the turn during required tutorial card interactions.
        TutorialBattleController tutorial =
            FindFirstObjectByType<TutorialBattleController>();

        if (tutorial != null && tutorial.BlockEndTurn)
        {
            Debug.Log("Tutorial: End Turn is currently blocked.");
            return;
        }

        if (currentstate != BattleState.PlayerTurn)
        {
            return;
        }

        // Prevent another enemy turn from being started.
        currentstate = BattleState.Busy;

        Debug.Log("Starting Enemy Turn");
        StartCoroutine(EnemyTurn());
    }

    public void StartQuizPenaltyTurn()
    {
        if (currentstate != BattleState.PlayerTurn ||
            isLevelFinished)
        {
            return;
        }

        currentstate = BattleState.Busy;

        StartCoroutine(EnemyTurn(true));
    }

    IEnumerator EnemyTurn(bool isPenaltyTurn = false)
    {
        Debug.Log("EnemyTurn Started");

        currentstate = BattleState.Busy;

        yield return new WaitForSecondsRealtime(1f);

        foreach (EnemyStats enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy || enemy.isDead || enemy.currentHP <= 0)
            {
                continue;
            }

            if (enemy != null)
            {
                Debug.Log(
                    enemy.name +
                    " | Active: " + enemy.gameObject.activeInHierarchy +
                    " | Dead: " + enemy.isDead +
                    " | HP: " + enemy.currentHP
                );
            }

            EnemyAction action;

            if (enemy.action != null && enemy.action.Length > 0)
            {
                action = enemy.action[Random.Range(0, enemy.action.Length)];
            }
            else
            {
                action = new EnemyAction
                {
                    actionType = EnemyActionType.Attack,
                    value = enemy.enemyType.damage
                };
            }

            Debug.Log("Enemy " + enemy.name + " uses: " + action.actionType);

            ExecuteEnemyAction(enemy, action);

            yield return new WaitForSecondsRealtime(0.5f);
        }

        yield return new WaitForSecondsRealtime(1f);

        if (CheckLoseCondition())
        {
            currentstate = BattleState.Lose;
            Debug.Log("You Lost");
            yield break;
        }

        if (isPenaltyTurn)
        {
            // Resume the existing player turn without resetting resources.
            currentstate = BattleState.PlayerTurn;

            Debug.Log(
                "Enemy penalty turn finished. " +
                "Player resumes with " + currentActionPoint + " AP."
            );
        }
        else
        {
            // Normal enemy turn: start a fresh player turn.
            StartPlayerTurn();
        }
    }

    void ExecuteEnemyAction(EnemyStats enemy, EnemyAction action)
    {
        switch (action.actionType)
        {
            case EnemyActionType.Attack:
                HeroStats target = GetRandomAliveHero();
                if(target != null)
                {
                    int finalDamage = enemy.GetModifiedActionValue(enemy.enemyType.damage);

                    enemy.PlayAnimation(CharacterAnimationType.Attack);
                    target.TakeDamage(finalDamage);
                    Debug.Log(enemy.name + " attacked for " + finalDamage + " damage");
                }
                break;

            case EnemyActionType.Shield:
                EnemyStats shieldTarget = GetRandomAliveEnemy();
                if (shieldTarget != null)
                {
                    int finalShield = enemy.GetModifiedActionValue(action.value);
                    enemy.PlayAnimation(CharacterAnimationType.Action);
                    shieldTarget.AddShield(finalShield);
                }
                break;

            case EnemyActionType.Heal:
                EnemyStats healTarget = GetRandomAliveEnemy();
                if (healTarget != null)
                {
                    int finalHeal = enemy.GetModifiedActionValue(action.value);
                    enemy.PlayAnimation(CharacterAnimationType.Action);
                    healTarget.Heal(finalHeal);
                }
                break;

            case EnemyActionType.Buff:
                EnemyStats buffTarget = GetRandomAliveEnemy();
                if (buffTarget != null)
                {
                    enemy.PlayAnimation(CharacterAnimationType.Action);
                    buffTarget.AddActionModifier(Mathf.Abs(action.value));
                    Debug.Log(enemy.name + " buffed " + buffTarget.name + " by " + action.value);
                }
                break;

            case EnemyActionType.Debuff:
                enemy.PlayAnimation(CharacterAnimationType.Action);

                AddPlayerModifier(-Mathf.Abs(action.value));

                Debug.Log(enemy.name + " debuffed the player team by " + action.value);
                break;
        }
    }

    HeroStats GetRandomAliveHero()
    {
        List<HeroStats> alive = new List<HeroStats>();

        foreach (var hero in heroes)
        {
            if (hero != null && hero.gameObject.activeInHierarchy)
            {
                alive.Add(hero);
            }
        }

        if(alive.Count == 0)
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

        //Checks whether this is a Custom or General Card
        foreach (HeroStats hero in heroes)
        {
            if(hero == null || !hero.gameObject.activeInHierarchy || hero.heroType == null || hero.heroType.customCards == null)
            {
                continue;
            }

            foreach (CardType customCard in hero.heroType.customCards)
            {
                if (customCard == card)
                {
                    Debug.Log("CUSTOM CARD: " + card.cardName + " | Assigned Hero: " + hero.heroType.name);
                    return hero;
                }
            }
        }

        //Generals cards use one randomly selected active hero
        HeroStats randomHero = GetRandomAliveHero();

        if (randomHero != null)
        {
            Debug.Log("GENERAL CARD: " + card.cardName + " | Random Hero: " + randomHero.heroType.name);
        }

        return randomHero;
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
        foreach(var enemy in enemies)
        {
            if(enemy != null && !enemy.isDead)
            {
                return false;
            }
        }

        return true;
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

    private void ExecuteCardEffect(CardType card, HeroStats actingHero, HeroStats targetHero = null, EnemyStats targetEnemy = null, int? calculatedValue = null)
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
                    targetEnemy.TakeDamage(finalDamage);

                    Debug.Log(actingHero.name + " dealt " + finalDamage + " damage to " + targetEnemy.name);
                }
                break;

            case CardEffectType.Heal:
                if (targetHero != null && !targetHero.IsDead())
                {
                    int finalHeal = calculatedValue ?? GetModifiedPlayerValue(value);
                    targetHero.Heal(finalHeal);
                }
                break;

            case CardEffectType.Shield:
                if (targetHero != null && !targetHero.IsDead())
                {
                    int finalShield = calculatedValue ?? GetModifiedPlayerValue(value);
                    targetHero.Shield(finalShield);
                }
                break;

            case CardEffectType.Buff:
                AddPlayerModifier(Mathf.Abs(value));
                break;

            case CardEffectType.Debuff:
                if (targetEnemy != null && !targetEnemy.isDead)
                {
                    targetEnemy.AddActionModifier(-Mathf.Abs(value));
                }
                break;
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

            AddPlayerModifier(Mathf.Abs(card.damage));

            return;
        }

        bool consumeModifier =
            card.effectType == CardEffectType.Damage ||
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
            card.effectType == CardEffectType.Debuff ?
            CharacterAnimationType.Attack : CharacterAnimationType.Action;

        switch (card.targetType)
        {
            case TargetType.AllEnemies:
                BeginCardAnimation(actingHero, animation);

                foreach (EnemyStats enemy in enemies)
                {
                    if (enemy != null && enemy.gameObject.activeInHierarchy && !enemy.isDead)
                    {
                        ExecuteCardEffect(card, actingHero, targetEnemy: enemy, calculatedValue: finalValue);
                    }
                }
                break;

            case TargetType.AllHeroes:
                BeginCardAnimation(actingHero, animation);

                foreach (HeroStats hero in heroes)
                {
                    if(hero != null && hero.gameObject.activeInHierarchy && !hero.IsDead())
                    {
                        ExecuteCardEffect(card, actingHero, targetHero: hero, calculatedValue: finalValue);
                    }
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

        if (!TrySpendAP(card.cost))
        {
            return false;
        }

        OnCardPlayAccepted?.Invoke();

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
        if (!isSelectingTarget ||
            pendingCard == null ||
            pendingHero == null ||
            enemy == null ||
            enemy.IsDead())
        {
            return;
        }

        // Only Damage and Debuff may target enemies.
        if (pendingCard.effectType != CardEffectType.Damage &&
            pendingCard.effectType != CardEffectType.Debuff)
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
            card.effectType == CardEffectType.Debuff
            ? CharacterAnimationType.Attack
            : CharacterAnimationType.Action;

        BeginCardAnimation(actingHero, animation);

        // ExecuteCardEffect already applies the correct effect
        // and consumes player modifiers for numerical actions.
        ExecuteCardEffect(card, actingHero, targetHero: targetHero, targetEnemy: targetEnemy);

        isSelectingTarget = false;
        pendingCard = null;
        pendingHero = null;

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

    void UpdateUI()
    {
        endButton.SetActive(currentstate == BattleState.PlayerTurn);
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
