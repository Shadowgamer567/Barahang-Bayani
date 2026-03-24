using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class BattleControl : MonoBehaviour
{

    public GameObject EnemyController;
    public GroundLoop groundloop;

    public float moveduration = 5f;
    public int damage = 5;
    public bool nextbattletriggered = false;

    EnemyStats[] enemies;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetEnemiesActive(false);

        StartCoroutine(BattleSequence());
    }

    void SetEnemiesActive(bool state)
    {
        foreach (Transform child in EnemyController.transform) 
        {
            child.gameObject.SetActive(state);
        }
    }

    void CacheEnemies()
    {
        enemies = GetComponentsInChildren<EnemyStats>(true);
    }

    public void AttackAllEnemies()
    {
        foreach (EnemyStats enemy in enemies)
        {
            if (enemy.gameObject.activeSelf)
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    public void DealDamageToAll(int damage)
    {
        foreach (EnemyStats enemy in enemies)
        {
            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemy.TakeDamage(damage);
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

    //Debug Damage Button
    private void Update()
    {
        if (Keyboard.current.slashKey.wasPressedThisFrame)
        {
            AttackAllEnemies();
        }

        if (!nextbattletriggered && allEnemiesAreDead())
        {
            nextbattletriggered = true;
            StartCoroutine(NextBattle());
        }
    }

    //Starting Battle Sequence
    IEnumerator BattleSequence()
    {
        groundloop.isMoving = true;

        yield return new WaitForSeconds(moveduration);

        groundloop.isMoving = false;

        SetEnemiesActive(true);
        CacheEnemies();
    }

    //Subsequent Battle Sequence
    IEnumerator NextBattle()
    {
        yield return new WaitForSeconds(2f);

        groundloop.isMoving = true;

        yield return new WaitForSeconds(moveduration);

        groundloop.isMoving = false;

        SetEnemiesActive(true);
        CacheEnemies();

        nextbattletriggered = false;
    }
}
