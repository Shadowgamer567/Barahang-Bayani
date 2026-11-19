using System.Collections;
using UnityEngine;

public class HeroStats : MonoBehaviour
{
    public static event System.Action OnHpLost;
    public static event System.Action OnShieldGained;
    public static event System.Action OnHealed;

    public HeroType heroType;
    public GameObject uiObject;

    public Transform animateChild;
    public Transform visualRoot;
    public GameObject currentModel;
    private CharacterAnimationRelay animationRelay;

    public int currentHP;
    [SerializeField, Min(1)] private int rolledSpeed = 1;
    public int Speed => rolledSpeed;
    public int MaxSpeed => heroType != null ? Mathf.Max(1, heroType.speed) : 1;
    public void RollSpeed() => rolledSpeed = Random.Range(1, MaxSpeed + 1);
    public int attack;
    public int shield;
    public bool LowHealthRallyActive { get; private set; }
    public int LowHealthRallyDamageBonusPercent { get; private set; }
    public int LowHealthRallyResistancePercent { get; private set; }

    public void SetLowHealthRally(bool active, int damageBonusPercent, int resistancePercent)
    {
        LowHealthRallyActive = active;
        LowHealthRallyDamageBonusPercent = active ? Mathf.Max(0, damageBonusPercent) : 0;
        LowHealthRallyResistancePercent = active ? Mathf.Clamp(resistancePercent, 0, 90) : 0;
    }

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
        shield = Mathf.Max(0, heroType.shield);

        LoadModel();
    }

    public void TakeDamage(int amount, int shieldDamageMultiplier = 1, int piercingDamage = 0)
    {
        if (IsDead())
        {
            return;
        }

        amount = Mathf.Max(0, amount);
        piercingDamage = Mathf.Max(0, piercingDamage);

        if (LowHealthRallyActive)
        {
            float damageMultiplier = 1f - LowHealthRallyResistancePercent / 100f;
            amount = Mathf.CeilToInt(amount * damageMultiplier);
            piercingDamage = Mathf.CeilToInt(piercingDamage * damageMultiplier);
        }

        int previousHP = currentHP;

        // Normal damage consumes all available shield first,
        // including temporary overflow.
        shieldDamageMultiplier = Mathf.Max(1, shieldDamageMultiplier);
        int shieldDamage = Mathf.Min(shield, amount * shieldDamageMultiplier);
        shield -= shieldDamage;

        int remainingDamage = Mathf.Max(0, amount - shieldDamage) + piercingDamage;

        if (remainingDamage > 0)
        {
            currentHP -= remainingDamage;
        }

        currentHP = Mathf.Max(0, currentHP);
        shield = Mathf.Max(0, shield);

        if (currentHP < previousHP)
        {
            OnHpLost?.Invoke();
        }

        if (animationRelay != null && currentHP > 0)
        {
            animationRelay.Play(CharacterAnimationType.Damaged);
        }

        if (currentHP <= 0)
        {
            // Remove this hero's custom cards from future draws.
            CardPanelManager cardManager = FindFirstObjectByType<CardPanelManager>();

            if (cardManager != null)
            {
                cardManager.RemoveHeroCards(heroType);
            }

            else
            {
                Debug.LogWarning("HeroStats: CardPanelManager not found when hero died.");
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

    public void Heal(int amount)
    {
        if (IsDead())
        {
            return;
        }

        amount = Mathf.Max(0, amount);

        int previousHP = currentHP;
        currentHP = Mathf.Min(currentHP + amount, heroType.maxHp);

        if (currentHP > previousHP)
        {
            OnHealed?.Invoke();
        }

        Debug.Log(name + " Healed for" + amount + " | Current HP: " + currentHP);
    }

    public void Shield(int amount)
    {
        if (IsDead())
        {
            return;
        }

        if (!gameObject.activeInHierarchy || IsDead())
        {
            return;
        }

        amount = Mathf.Max(0, amount);

        if (amount <= 0)
        {
            return;
        }

        shield += amount;

        OnShieldGained?.Invoke();

        Debug.Log(name + " gained " + amount + " shield | Total Shield: " + shield);
    }

    public void DecayShieldOverflow()
    {
        int maxShield = Mathf.Max(0, heroType.maxShield);

        if (shield <= maxShield)
        {
            return;
        }

        int overflow = shield - maxShield;
        int remainingOverflow = Mathf.CeilToInt(overflow / 2f);

        shield = maxShield + remainingOverflow;

        Debug.Log(name + " shield overflow decayed. " + "Remaining Shield: " + shield + " | Max Shield: " + maxShield);
    }

    /*
    public void ReduceAttack(int amount)
    {
        attack -= amount;
        Debug.Log(name + " reduced " + amount);
    }
    */

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
            Debug.Log(
            "HERO ANIMATION CHECK | " +
            "Hero Object: " + gameObject.name +
            " | Hero Type: " + heroType.heroName +
            " | Model: " + currentModel.name +
            " | Animator: " + animationRelay.animator.name +
            " | Animation: " + type
        );

            animationRelay.Play(type);
        }
        
        else
        {
            Debug.LogWarning(
                "No animation relay found for: " + gameObject.name
            );
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
