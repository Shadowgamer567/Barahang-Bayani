using System.Collections;
using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    public EnemyTypes enemyType;

    public int currentHP;

    void OnEnable()
    {
        currentHP = enemyType.maxHp;

        transform.rotation = Quaternion.identity;
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;

        if(currentHP <= 0)
        {
            StartCoroutine(Die());
        }
    }

    IEnumerator Die()
    {
        Quaternion startRotation = transform.rotation;

        // Rotate 90 degrees on X axis over 2 seconds
        Quaternion targetRotation = startRotation * Quaternion.Euler(0f, 0f, 90f);

        float elapsed = 0f;
        float duration = 2f;

        while (elapsed < duration)
        {
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure final rotation is exact
        transform.rotation = targetRotation;

        // Disable the enemy
        gameObject.SetActive(false);
    }
        public bool IsDead()
    {
        return currentHP <= 0;
    }
}
