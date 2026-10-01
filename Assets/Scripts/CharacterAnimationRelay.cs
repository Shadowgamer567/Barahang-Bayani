using UnityEngine;

public class CharacterAnimationRelay : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("State Names")]
    public string neutralState = "Idle";
    public string moveState = "Move";
    public string attackState = "Attack";
    public string actionState = "Action";
    public string damagedState = "Hit";
    public string deathState = "Death";

    private EnemyStats enemyParent;
    private HeroStats heroParent;

    private void Awake()
    {
        if(animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        enemyParent = GetComponentInParent<EnemyStats>();
        heroParent = GetComponentInParent<HeroStats>();
    }

    public void Play(CharacterAnimationType type)
    {
        if(animator == null)
        {
            Debug.LogError(name + "has no animator");
            return;
        }

        Debug.Log(name + "PLAYING " + type);

        switch (type)
        {
            case CharacterAnimationType.Neutral:
                animator.Play(neutralState);
                break;

            case CharacterAnimationType.Moving:
                animator.Play(moveState);
                break;

            case CharacterAnimationType.Attack:
                animator.Play(attackState);
                break;

            case CharacterAnimationType.Action:
                animator.Play(actionState);
                break;

            case CharacterAnimationType.Damaged:
                animator.Play(damagedState);
                break;

            case CharacterAnimationType.Death:
                animator.Play(deathState);
                break;
        }
    }

    public void OnDeathAnimationComplete()
    {
        if(enemyParent != null)
        {
            enemyParent.OnDeathAnimationComplete();
        }

        if(heroParent != null)
        {
            heroParent.OnDeathAnimationComplete();
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
