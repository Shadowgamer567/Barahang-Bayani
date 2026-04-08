using System.Collections;
using UnityEngine;

public class HeroStats : MonoBehaviour
{
    public HeroType heroType;
    public GameObject uiObject;

    public int currentHP;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHP = heroType.maxHp;
    }

    public void TakeDamage(int amount)
    {
        currentHP -= amount;

        if (currentHP <= 0)
        {
            StartCoroutine(Die());
        }
    }

    public void SyncToData(GameData data)
    {
        data.hero_health = this.currentHP;
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
