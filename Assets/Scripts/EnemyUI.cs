//Handles Enemy information display

using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyUI : MonoBehaviour
{
    public Image hpFill;
    [SerializeField, Range(0f, 1f)] private float lowHealthThreshold = 0.25f;
    [SerializeField] private Color lowHealthColor = Color.red;

    float currentDisplayHP = 1f;
    private Color normalHpColor;

    EnemyStats enemy;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemy = GetComponentInParent<EnemyStats>();
        if (hpFill != null) normalHpColor = hpFill.color;
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

        transform.position = enemy.transform.position + Vector3.up * 3.15f;

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
        hpFill.color = hpPercent <= lowHealthThreshold ? lowHealthColor : normalHpColor;
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
