//Handles Battle Logic and sequence

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.ShaderGraph.Internal;
using UnityEditorInternal.Profiling.Memory.Experimental.FileFormat;
using UnityEngine;
using UnityEngine.InputSystem;

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
    private LevelData levelData;

    public float moveduration = 5f;
    public int damage = 5;
    public bool nextbattletriggered = false;
    public int aliveEnemies = 0;
    public BattleState currentstate;
    public bool isSelectingTarget = false;
    public int maxActionPoint = 5;
    public int currentActionPoint;
    public bool isLevelFinished = false;

    private bool battleAlreadyWon = false;

    public System.Action<int, int> OnAPChanged;


    public System.Action OnBattleWon;

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
                enemies[i].InitializeEnemy();
                enemies[i].gameObject.SetActive(true);
                enemies[i].LoadModel();
            }

            else
            {
                enemies[i].gameObject.SetActive(false);
            }
        }

        aliveEnemies = 0;

        for (int i = 0; i < enemies.Length; i++)
        {
            if (i >= enemies.Length)
                continue;

            EnemyStats enemy = enemies[i];
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
        if(currentstate != BattleState.PlayerTurn)
        {
            return;
        }

        Debug.Log("Starting Enemy Turn");
        StartCoroutine(EnemyTurn());
    }

    IEnumerator EnemyTurn()
    {
        Debug.Log("EnemyTurn Started");

        currentstate = BattleState.Busy;

        yield return new WaitForSecondsRealtime(1f);

        foreach (EnemyStats enemy in enemies)
        {
            if (enemy == null || enemy.isDead)
            {
                continue;
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

            Debug.Log("Enemy uses: " + action.actionType);

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

        StartPlayerTurn();
    }

    void ExecuteEnemyAction(EnemyStats enemy, EnemyAction action)
    {
        switch (action.actionType)
        {
            case EnemyActionType.Attack:
                HeroStats target = GetRandomAliveHero();
                if(target != null)
                {
                    target.TakeDamage(enemy.enemyType.damage);
                }
                break;

            case EnemyActionType.Shield:
                EnemyStats shieldTarget = GetRandomAliveEnemy();
                if (shieldTarget != null)
                {
                    shieldTarget.AddShield(action.value);
                }
                break;

            case EnemyActionType.Heal:
                EnemyStats healTarget = GetRandomAliveEnemy();
                if (healTarget != null)
                {
                    healTarget.Heal(action.value);
                }
                break;

            case EnemyActionType.Buff:
                EnemyStats buffTarget = GetRandomAliveEnemy();
                if (buffTarget != null)
                {
                    buffTarget.BuffAttack(action.value);
                }
                break;

            case EnemyActionType.Debuff:
                HeroStats debuffTarget = GetRandomAliveHero();
                if (debuffTarget != null)
                {
                    debuffTarget.ReduceAttack(action.value);
                }
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

    EnemyStats GetRandomAliveEnemy()
    {
        List<EnemyStats> alive = new List<EnemyStats>();

        foreach (var enemy in enemies)
        {
            if (enemy != null && !enemy.isDead)
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

            OnBattleWon?.Invoke();

            if (!nextbattletriggered)
            {
                nextbattletriggered = true;
            }
        }
    }

    public bool TrySpendAP(int cost)
    {
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

    public void HandleCardPlay(CardType card, CardUI cardUI)
    {
        Debug.Log("HandleCardPlay CALLED with: " + card.cardName + " | " + card.targetType);

        if (!TrySpendAP(card.cost))
        {
            return;
        }

        if (card.quizCard)
        {
            QuizManager quizManager = FindFirstObjectByType<QuizManager>();

            if (quizManager != null) {
                QuizQuestion q = quizManager.GetRandomQuestions();
                quizManager.StartQuiz(q, card.damage, this);
            }

            Destroy(cardUI.gameObject);
            return;
        }


        if (card.targetType == TargetType.AllEnemies)
        {
            DealDamageToAll(card.damage);
            Destroy(cardUI.gameObject);
            return;
        }

        if (card.targetType == TargetType.SingleTarget)
        {
            Debug.Log("Select a Target");

            isSelectingTarget = true;
            pendingCard = card;

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
        }
    }

    public void SelectEnemyTarget(EnemyStats enemy)
    {
        Debug.Log("SelectEnemyTarget Called");

        if(enemy == null || enemy.IsDead())
        {
            return;
        }

        if (!isSelectingTarget || pendingCard == null)
        {
            Debug.Log("Blocked: selecting=" + isSelectingTarget + " pending=" + pendingCard);
            return;
        }

        if (selectTargetPanel != null)
        {
            selectTargetPanel.SetActive(false);
        }

        /*foreach (EnemyStats e in enemies)
        {
            if (e != null)
            {
                e.SetHighlighted(false);
            }
        }*/

        CardType usedCard = pendingCard;

        Debug.Log("Applying Damage: " + pendingCard.damage);

        Debug.Log("TARGET CLICKED: " + enemy.name);
        Debug.Log("Pending damage: " + pendingCard.damage);

        enemy.TakeDamage(pendingCard.damage);

        isSelectingTarget = false;
        pendingCard = null;

        if (cardPanel != null)
        {
            cardPanel.SetActive(true);
        }
    }

    public void StartNextBattleManually()
    {
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

        if (Mouse.current.leftButton.wasPressedThisFrame) {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log("Hit: " + hit.collider.name);

                EnemyStats enemy = hit.collider.GetComponentInParent<EnemyStats>();

                if(enemy != null)
                {
                    SelectEnemyTarget(enemy);
                }
            }

            else
            {
                Debug.Log("Nothing Hit");
            }
        }

        if (isLevelFinished)
        {
            return;
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        endButton.SetActive(currentstate == BattleState.PlayerTurn);
    }

    public void StartBattleSequence()
    {
        StartCoroutine(BattleSequence());
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

        yield return new WaitForSeconds(moveduration);

        groundloop.isMoving = false;

        SetupEnemies();

        StartPlayerTurn();
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

        yield return new WaitForSecondsRealtime(moveduration);

        groundloop.isMoving = false;

        SetupEnemies();

        nextbattletriggered = false;

        StartPlayerTurn();
    }

    IEnumerator ShowAPWarning()
    {
        notEnoughAPPanel.SetActive(true);

        yield return new WaitForSecondsRealtime(1.5f);

        notEnoughAPPanel.SetActive(false);
    }
}
