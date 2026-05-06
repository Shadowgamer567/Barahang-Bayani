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
    public Transform model;
    private BattleControl battle;
    public Animator animator;
    public Transform modelRoot;
    public GameObject currentModel;

    public int currentHP;
    public int shield;
    public int attack;
    private bool isDying = false;
    public bool isDead = false;

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
        isDead = false;

        transform.rotation = Quaternion.identity;

        if (model != null)
        {
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
        }

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }
    }

    void Awake()
    {
        if (model != null && animator == null)
        {
            animator = model.GetComponent<Animator>();
        }
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
            isDead = true;
            Debug.Log(name + " ENTERING DEATH");
            isDying = true;
            
            foreach(var c in GetComponentsInChildren<Collider>())
            {
                c.enabled = false;
            }

            if (battle != null)
            {
                battle.OnEnemyKilled();
            }

            if (animator != null)
            {
                animator.SetTrigger("Die");
            }

            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    /*
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
    */

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

    public void LoadModel()
    {
        if (enemyType == null || enemyType.prefab == null)
        {
            Debug.LogWarning("No prefab assigned for enemy: " + name);
            return;
        }

        if (currentModel != null)
        {
            Destroy(currentModel);
        }

        currentModel = Instantiate(enemyType.prefab, modelRoot);
        currentModel.transform.localPosition = enemyType.modelPositionOffset;
        currentModel.transform.localRotation = Quaternion.Euler(enemyType.modelRotationOffset);

        FaceHeroes();
    }

    void FaceHeroes()
    {
        HeroStats[] heroes = FindObjectsByType<HeroStats>(FindObjectsSortMode.None);

        HeroStats target = null;

        foreach (var h in heroes)
        {
            if (h != null && h.gameObject.activeInHierarchy)
            {
                target = h;
                break;
            }
        }

        if (target == null) return;

        Vector3 direction = (target.transform.position - transform.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            transform.forward = direction;
        }
    }

    /*
    IEnumerator Die()
    {
        Debug.Log(name + " DIE STARTED");

        Collider col = GetComponent<Collider>();

        if(col != null)
        {
            col.enabled = false;
        }

        if(battle != null)
        {
            battle.OnEnemyKilled();
        }

        Transform model = transform.Find("AnimateChild");
        if (model != null)
        {
            Quaternion startRotation = model.rotation;
            Quaternion targetRotation = startRotation * Quaternion.Euler(0f, 0f, 90f);

            float elapsed = 0f;
            float duration = 2f;

            while (elapsed < duration)
            {
                model.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            model.rotation = targetRotation;
        }
        yield return new WaitForSeconds(0.3f);

        gameObject.SetActive(false);
    }
    */

    public void OnDeathAnimationComplete()
    {
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
