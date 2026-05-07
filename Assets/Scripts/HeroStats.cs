using System.Collections;
using UnityEngine;

public class HeroStats : MonoBehaviour
{
    public HeroType heroType;
    public GameObject uiObject;
    public Transform modelRoot;
    public GameObject currentModel;


    public int currentHP;
    public int attack;
    public int shield;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void InitializeHero()
    {
        if (heroType == null)
        {
            Debug.LogError("Hero type is Null");
        }

        currentHP = heroType.maxHp;
        shield = heroType.maxShield;
        

        LoadModel();
    }

    public void TakeDamage(int amount)
    {
        if (shield > 0)
        {
            int shieldDamage = Mathf.Min(shield, amount);
            shield -= shieldDamage;

            int remaining = amount - shieldDamage;

            if (remaining > 0)
            {
                currentHP -= remaining;
            }
        }
        else
        {
            currentHP -= amount;
        }

        if (currentHP <= 0)
        {
            StartCoroutine(Die());
        }
    }

    public void ReduceAttack(int amount)
    {
        attack -= amount;
        Debug.Log(name + " reduced " + amount);
    }

    public void SyncToData(GameData data)
    {
        data.hero_health = this.currentHP;
    }

    public  void LoadModel()
    {
        if (heroType == null || heroType.prefab == null)
        {
            Debug.LogWarning("No prefab assigned for hero: " + name);
            return;
        }

        if (currentModel != null)
        {
            Destroy(currentModel);
        }

        currentModel = Instantiate(heroType.prefab, modelRoot);
        currentModel.transform.localPosition = heroType.modelPositionOffset;
        currentModel.transform.localRotation = Quaternion.Euler(heroType.modelRotationOffset);

        FaceEnemy();
    }

    void FaceEnemy()
    {
        EnemyStats[] enemies = FindObjectsByType<EnemyStats>(FindObjectsSortMode.None);

        EnemyStats target = null;

        foreach (var e in enemies)
        {
            if (e != null && e.gameObject.activeInHierarchy && !e.isDead)
            {
                target = e;
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

    IEnumerator Die()
    {
        Quaternion startRotation = transform.rotation;

        Quaternion targetRotation = startRotation * Quaternion.Euler(0f, 0f, -90f);

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

    // Update is called once per frame
    void Update()
    {
        
    }
}
