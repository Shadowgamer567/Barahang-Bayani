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

    public Transform animateChild;
    public Transform visualRoot;
    public GameObject currentModel;
    private CharacterAnimationRelay animationRelay;
    //public GameObject selectionIndicator;

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

        if(animationRelay != null)
        {
            animationRelay.Play(CharacterAnimationType.Neutral);
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
        }
    }

    public void InitializeEnemy()
    {
        if (enemyType == null)
        {
            Debug.LogError("EnemyType is NULL");
            return;
        }

        currentHP = enemyType.maxHp;
        shield = enemyType.maxShield;
        isDead = false;
    }

    void Awake()
    {
        animationRelay = GetComponentInChildren<CharacterAnimationRelay>();
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

        if (animationRelay != null && currentHP > 0)
        {
            animationRelay.Play(CharacterAnimationType.Damaged);
        }

        if (currentHP <= 0 && !isDying)
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

            if (animationRelay != null)
            {
                animationRelay.Play(CharacterAnimationType.Death);
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

        currentModel = Instantiate(enemyType.prefab, visualRoot);
        currentModel.transform.localPosition = enemyType.modelPositionOffset;
        currentModel.transform.localRotation = Quaternion.Euler(enemyType.modelRotationOffset);

        animationRelay = currentModel.GetComponentInChildren<CharacterAnimationRelay>();

        if(animationRelay == null)
        {
            animationRelay = currentModel.AddComponent<CharacterAnimationRelay>();
        }

        FaceHeroes();

        animationRelay.Play(CharacterAnimationType.Neutral);
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

    /*public void SetHighlighted(bool state)
    {
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(state);
        }
    }*/

    public void PlayAnimation(CharacterAnimationType type)
    {
        if(animationRelay != null)
        {
            animationRelay.Play(type);
        }
    }

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
