using UnityEngine;
using UnityEngine.InputSystem;

public class HeroTargetIndicator : MonoBehaviour
{
    public GameObject targetIndicator;

    private HeroStats hero;
    private BattleControl battle;

    void Start()
    {
        hero = GetComponent<HeroStats>();
        battle = FindFirstObjectByType<BattleControl>();

        if (targetIndicator != null)
            targetIndicator.SetActive(false);
    }

    void Update()
    {
        if (targetIndicator == null ||
            hero == null ||
            battle == null)
            return;

        bool showIndicator = false;

        if (battle.IsTargetingHeroes &&
            !hero.IsDead() &&
            Mouse.current != null &&
            Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(
                Mouse.current.position.ReadValue()
            );

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                HeroStats hoveredHero =
                    hit.collider.GetComponentInParent<HeroStats>();

                showIndicator = hoveredHero == hero;
            }
        }

        if (targetIndicator.activeSelf != showIndicator)
            targetIndicator.SetActive(showIndicator);
    }

    void OnDisable()
    {
        if (targetIndicator != null)
            targetIndicator.SetActive(false);
    }
}