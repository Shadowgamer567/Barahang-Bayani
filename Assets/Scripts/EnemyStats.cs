//Handles Enemy Statistics

using System.Collections;
using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    public EnemyTypes enemyType;
    public GameObject uiObject;
    public EnemyUI ui;

    public int currentHP;

    private void Start()
    {
        
    }

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

    public void SyncToData(GameData data)
    {
        data.enemy_health = this.currentHP;
    }

    IEnumerator Die()
    {
        Quaternion startRotation = transform.rotation;

        Quaternion targetRotation = startRotation * Quaternion.Euler(0f, 0f, 90f);

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

    private void Update()
    {
        
    }
}
