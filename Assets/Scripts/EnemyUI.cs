using TMPro;
using UnityEngine;

public class EnemyUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI hpText;

    EnemyStats enemy;

    void UpdateUI()
    {
        if (enemy == null) 
        {
            return;
        }

        nameText.text = enemy.enemyType.enemyName;
        hpText.text = "HP: " + enemy.currentHP + "/" + enemy.enemyType.maxHp;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemy = GetComponentInParent<EnemyStats>();

        UpdateUI();
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = enemy.transform.position + Vector3.up * 2f;

        transform.forward = Camera.main.transform.forward;
        UpdateUI();
    }


}
