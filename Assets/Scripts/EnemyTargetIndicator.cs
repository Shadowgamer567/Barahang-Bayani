using UnityEngine;
using UnityEngine.InputSystem;

public class EnemyTargetIndicator : MonoBehaviour
{
    public GameObject targetIndicator;

    private EnemyStats enemy;
    private BattleControl battle;

    void Start()
    {
        enemy = GetComponent<EnemyStats>();
        battle = FindFirstObjectByType<BattleControl>();

        if (targetIndicator != null)
            targetIndicator.SetActive(false);
    }

    void Update()
    {
        if (targetIndicator == null ||
            enemy == null ||
            battle == null)
            return;

        bool showIndicator = false;

        if (battle.IsTargetingEnemies &&
            !enemy.isDead &&
            enemy.currentHP > 0 &&
            Mouse.current != null &&
            Camera.main != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(
                Mouse.current.position.ReadValue()
            );

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                EnemyStats hoveredEnemy =
                    hit.collider.GetComponentInParent<EnemyStats>();

                showIndicator = hoveredEnemy == enemy;
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