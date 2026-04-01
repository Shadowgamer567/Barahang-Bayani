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

    public float moveduration = 5f;
    public int damage = 5;
    public bool nextbattletriggered = false;
    public BattleState currentstate;

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

        StartCoroutine(EnemyTurn());
    }

    IEnumerator EnemyTurn()
    {
        currentstate = BattleState.Busy;

        yield return new WaitForSeconds(1f);

        foreach (EnemyStats enemy in enemies)
        {
            if (!enemy.gameObject.activeInHierarchy)
            {
                continue;
            }

            HeroStats target = GetRandomAliveHero();

            if(target != null)
            {
                target.TakeDamage(enemy.enemyType.damage);
            }

            yield return new WaitForSeconds(0.5f);
        }

        yield return new WaitForSeconds(1f);

        if (CheckLoseCondition())
        {
            currentstate = BattleState.Lose;

            Debug.Log("You Lost");

            yield break;
        }

        currentstate = BattleState.PlayerTurn;
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
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                return;
            }

            if (!nextbattletriggered)
            {
                nextbattletriggered = true;
                StartCoroutine(NextBattle());
            }
        }
    }
    bool allEnemiesAreDead()
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
    } 
    private void Update()
    {
        //Debug Damage Button
        /*if (Keyboard.current.slashKey.wasPressedThisFrame)
        {
            AttackAllEnemies();
        }*/

        if (!nextbattletriggered && allEnemiesAreDead())
        {
            nextbattletriggered = true;
            StartCoroutine(NextBattle());
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

        yield return new WaitForSeconds(2f);

        groundloop.isMoving = true;

        yield return new WaitForSeconds(moveduration);

        groundloop.isMoving = false;

        SetEnemiesActive(true);
        CacheEnemies();

        nextbattletriggered = false;

        currentstate = BattleState.PlayerTurn;
    }
}
