using UnityEngine;
using UnityEngine.InputSystem;

// deprecated damage button script
// functions merged into battle control script
/*public class DebugDamage : MonoBehaviour
{
    public int damage = 5;

    EnemyStats[] enemies;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CacheEnemies();
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.slashKey.wasPressedThisFrame)
        {
            AttackAllEnemies();
        }
    }

    void CacheEnemies()
    {
        enemies = GetComponentsInChildren<EnemyStats>(true);
    }

    void AttackAllEnemies()
    {
        foreach(EnemyStats enemy in enemies)
        {
            if (enemy.gameObject.activeSelf)
            {
                enemy.TakeDamage(damage);
            }
        }
    }
}
*/