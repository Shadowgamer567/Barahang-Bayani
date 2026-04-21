//Handles Battle Logic and sequence

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class BattleControl : MonoBehaviour
{

    public Transform enemyController;
    public Transform heroController;
    public GroundLoop groundloop;
    public GameObject endButton;
    public GameObject cardPanel;
    public CardType pendingCard;

    public float moveduration = 5f;
    public int damage = 5;
    public bool nextbattletriggered = false;
    public int aliveEnemies = 0;
    public BattleState currentstate;
    public bool isSelectingTarget = false;


    public System.Action OnBattleWon;

    EnemyStats[] enemies;
    HeroStats[] heroes;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetEnemiesActive(false);

        CacheEnemies();
        CacheHeroes();

        currentstate = BattleState.PlayerTurn;

        StartCoroutine(BattleSequence());
        
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
        enemies = GetComponentsInChildren<EnemyStats>(true);

        aliveEnemies = 0;
        foreach(var e in enemies)
        {
            if (e.gameObject.activeInHierarchy)
            {
                aliveEnemies++;
            }
        }
    }

    void CacheHeroes()
    {
        heroes = GetComponentsInChildren<HeroStats>(true);
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
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemy.TakeDamage(damage);
            }
        }

        CheckWinCondition();
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
            if (!enemy.gameObject.activeInHierarchy)
                continue;

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

        currentstate = BattleState.PlayerTurn;
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
            if (enemy != null && enemy.gameObject.activeInHierarchy)
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
            if(enemy != null && enemy.gameObject.activeInHierarchy)
            {
                return false;
            }
        }

        return true;
    }

    void CheckWinCondition()
    {
        Debug.Log("Checking Win Condition");

        foreach (var enemy in enemies)
        {
            if (enemy == null)
            {
                Debug.Log("Enemy is Null");
                continue;
            }

            Debug.Log(enemy.name + " active: " + enemy.gameObject.activeInHierarchy);

            if (enemy.gameObject.activeInHierarchy)
            {
                Debug.Log("Still Enemies alive -> now win");
                return;
            }
        }

        Debug.Log("Battle Won");

        OnBattleWon?.Invoke();

        if (!nextbattletriggered)
        {
            nextbattletriggered = true;
            StartCoroutine(NextBattle());
        }
    }

    public void OnEnemyKilled()
    {
        aliveEnemies--;

        Debug.Log("Enemied Left: " + aliveEnemies);

        if(aliveEnemies <= 0)
        {
            Debug.Log("Battle Won");

            OnBattleWon?.Invoke();

            if (!nextbattletriggered)
            {
                nextbattletriggered = true;
                StartCoroutine(NextBattle());
            }
        }
    }

    public void HandleCardPlay(CardType card, CardUI cardUI)
    {
        Debug.Log("HandleCardPlay CALLED with: " + card.cardName + " | " + card.targetType);

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
            Destroy(cardUI.gameObject);
        }
    }

    public void SelectEnemyTarget(EnemyStats enemy)
    {
        Debug.Log("SelectEnemyTarget Called");

        if (!isSelectingTarget || pendingCard == null)
        {
            Debug.Log("Blocked: selecting=" + isSelectingTarget + " pending=" + pendingCard);
            return;
        }

        Debug.Log("Selected Enemy: " + enemy.name);

        enemy.TakeDamage(pendingCard.damage);

        isSelectingTarget = false;
        pendingCard = null;

        cardPanel.SetActive(true);
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

        UpdateUI();

        if (Mouse.current.leftButton.wasPressedThisFrame) {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Debug.Log("Hit: " + hit.collider.name);
            }

            else
            {
                Debug.Log("Nothing Hit");
            }
        }
    }

    void UpdateUI()
    {
        endButton.SetActive(currentstate == BattleState.PlayerTurn);
    }

    //Starting Battle Sequence
    IEnumerator BattleSequence()
    {
        currentstate = BattleState.Busy;

        groundloop.isMoving = true;

        yield return new WaitForSeconds(moveduration);

        groundloop.isMoving = false;

        SetEnemiesActive(true);
        CacheEnemies();

        currentstate = BattleState.PlayerTurn;
    }

    //Subsequent Battle Sequence
    IEnumerator NextBattle()
    {
        currentstate = BattleState.Busy;

        yield return new WaitForSecondsRealtime(2f);

        groundloop.isMoving = true;

        yield return new WaitForSecondsRealtime(moveduration);

        groundloop.isMoving = false;

        SetEnemiesActive(true);
        CacheEnemies();

        nextbattletriggered = false;

        currentstate = BattleState.PlayerTurn;
    }
}
