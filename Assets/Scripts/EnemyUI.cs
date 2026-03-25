//Handles Enemy information display

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    public Image hpFill;

    float currentDisplayHP = 1f;

    EnemyStats enemy;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemy = GetComponentInParent<EnemyStats>();
    }

    // Update is called once per frame
    void Update()
    {
        if(enemy == null || hpFill == null)
        {
            return; 
        }

        if (!enemy.gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            return;
        }

        transform.position = enemy.transform.position + Vector3.up * 2f;

        transform.forward = Camera.main.transform.forward;

        if (!enemy.gameObject.activeSelf)
        {
            gameObject.SetActive(false);
            return;
        }

        if(enemy.enemyType == null)
        {
            return;
        }

        float hpPercent = (float)enemy.currentHP / enemy.enemyType.maxHp;
        float speed = 5f + Mathf.Abs(currentDisplayHP - hpPercent) * 10f;

        currentDisplayHP = Mathf.Lerp(currentDisplayHP, hpPercent, Time.deltaTime * speed);
        hpFill.fillAmount = currentDisplayHP;
    }

    private void LateUpdate()
    {
        if(enemy == null)
        {
            return;
        }

        transform.rotation = Quaternion.identity;

        transform.forward = Camera.main.transform.forward;
    }

}
