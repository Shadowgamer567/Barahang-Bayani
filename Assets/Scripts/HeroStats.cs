using System.Collections;
using UnityEngine;

public class HeroStats : MonoBehaviour
{
    public HeroType heroType;
    public GameObject uiObject;

    public Transform animateChild;
    public Transform visualRoot;
    public GameObject currentModel;
    private CharacterAnimationRelay animationRelay;

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

        if (animationRelay != null && currentHP > 0)
        {
            animationRelay.Play(CharacterAnimationType.Damaged);
        }

        if (currentHP <= 0)
        {
            if(animationRelay != null)
            {
                animationRelay.Play(CharacterAnimationType.Death);
            }

            else
            {
                gameObject.SetActive(false);
            }
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

        currentModel = Instantiate(heroType.prefab, visualRoot);
        currentModel.transform.localPosition = heroType.modelPositionOffset;
        currentModel.transform.localRotation = Quaternion.Euler(heroType.modelRotationOffset);

        animationRelay = currentModel.GetComponentInChildren<CharacterAnimationRelay>();

        if (animationRelay == null)
        {
            Debug.LogError("CharacterAnimationRelay missing on hero prefab: " + heroType.heroName);
            return;
        }

        FaceEnemy();

        animationRelay.Play(CharacterAnimationType.Neutral);
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

    //deprecated Local Death Anim.
    /*
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
    */

    public void PlayAnimation(CharacterAnimationType type)
    {
        if (animationRelay != null)
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

    // Update is called once per frame
    void Update()
    {
        
    }
}
