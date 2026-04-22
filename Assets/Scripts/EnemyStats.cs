//Handles Enemy Statistics

using System.Collections;
using UnityEditor.UIElements;
using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    public EnemyTypes enemyType;
    public GameObject uiObject;
    public EnemyUI ui;
    public EnemyAction[] action;

    private BattleControl battle;

    public int currentHP;
    public int shield;
    public int attack;
    private bool isDying = false;

    private void Start()
    {
        
    }

    void OnEnable()
    {
       battle = FindFirstObjectByType<BattleControl>();

        currentHP = enemyType.maxHp;
        shield = enemyType.maxShield;

        attack = enemyType.damage;

        isDying = false;

        transform.rotation = Quaternion.identity;
    }

    public void TakeDamage(int amount)
    {
        Debug.Log(name + " RECEIVED DAMAGE CALL");

        if (shield > 0)
        {
            int shieldDamage = Mathf.Min(shield, amount);
            shield -= shieldDamage;

            int excessDamage = amount - shieldDamage;

            if(excessDamage > 0)
            {
                currentHP -= excessDamage;
            }
        }

        else
        {
            currentHP -= amount;
        }
        Debug.Log(name + " HP AFTER DAMAGE: " + currentHP);

        currentHP = Mathf.Max(currentHP, 0);
        shield = Mathf.Max(shield, 0);

        Debug.Log(name + " took damage: " + amount + " | HP: " + currentHP + " | Shield: " + shield);

        if(currentHP <= 0 && !isDying)
        {
            Debug.Log(name + " ENTERING DEATH");
            isDying = true;
            DieImmediate();
        }
    }

    void DieImmediate()
    {
        isDying = true;

        Debug.Log(name + " IMMEDIATE DEATH");

        gameObject.SetActive(false);

        if (battle != null)
        {
            battle.OnEnemyKilled();
        }
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, enemyType.maxHp);
        Debug.Log(name + " healed for: " + amount);
    }

    public void AddShield(int amount)
    {
        shield += amount;
        Debug.Log(name + " gained shield " + amount);
    }

    public void BuffAttack(int amount)
    {
        attack += amount;
        Debug.Log(name + " attack increased by " + amount);
    }

    public void SyncToData(GameData data)
    {
        data.enemy_health = this.currentHP;
    }

    
    IEnumerator Die()
    {
        Debug.Log(name + " DIE STARTED");

        GetComponent<Collider>().enabled = false;

        if(battle != null)
        {
            battle.OnEnemyKilled();
        }

        Quaternion startRotation = transform.rotation;

        Quaternion targetRotation = startRotation * Quaternion.Euler(0f, 0f, 90f);

        float elapsed = 0f;
        float duration = 2f;

        while (elapsed < duration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.rotation = targetRotation;
        yield return new WaitForSeconds(0.3f);

        gameObject.SetActive(false);
    }
        public bool IsDead()
    {
        return currentHP <= 0;
    }
    

    private void Update()
    {

    }
}
