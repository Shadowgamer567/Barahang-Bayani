//Handles Primary UI display and displaly logic

using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class EnemyPanel
{
    public GameObject enemyPanel;
    public TextMeshProUGUI EnemyName;
    public Image hpFill;
    public TextMeshProUGUI hpText;
    public Image shieldFill;
    public TextMeshProUGUI shieldText;
}

[System.Serializable]
public class HeroPanel
{
    public GameObject heroPanel;
    public TextMeshProUGUI HeroName;
    public Image hpFill;
    public TextMeshProUGUI hpText;
    public Image shieldFill;
    public TextMeshProUGUI shieldText;
}
public class UIMain : MonoBehaviour
{
    public EnemyPanel[] enemypanel;
    public EnemyStats[] enemies;
    public HeroPanel[] heropanel;
    public HeroStats[] heroes;


    private float[] enemydisplayedHP;
    private float[] herodisplayedHP;
    private float[] enemydisplayedShield;
    private float[] herodisplayedShield;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemydisplayedHP = new float[enemypanel.Length];
        herodisplayedHP = new float[heropanel.Length];
        enemydisplayedShield = new float[enemypanel.Length];
        herodisplayedShield = new float[heropanel.Length];

        for(int i = 0; i < enemypanel.Length; i++)
        {
            enemypanel[i].enemyPanel.SetActive(false);
            enemydisplayedHP[i] = 1f;
            enemydisplayedShield[i] = 1f;
        }

        for (int i = 0; i < heropanel.Length; i++)
        {
            heropanel[i].heroPanel.SetActive(false);
            herodisplayedHP[i] = 1f;
            herodisplayedShield[i] = 1f;
        }
    }
    void UpdateEnemyUI()
    {
        for (int i = 0; i < enemypanel.Length; i++)
        {
            EnemyStats enemy = (i < enemies.Length) ? enemies[i] : null;

            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemypanel[i].enemyPanel.SetActive(true);
                enemypanel[i].EnemyName.text = enemy.enemyType.enemyName;

                float targetHP = (float)enemy.currentHP / enemy.enemyType.maxHp;
                enemydisplayedHP[i] = Mathf.Lerp(enemydisplayedHP[i], targetHP, Time.deltaTime * 8f);

                enemypanel[i].hpFill.fillAmount = enemydisplayedHP[i];
                enemypanel[i].hpText.text = $"{enemy.currentHP} / {enemy.enemyType.maxHp}";

                float maxShield = Mathf.Max(1, enemy.enemyType.maxShield);
                float targetShield = (float)enemy.shield / maxShield;

                enemydisplayedShield[i] = Mathf.Lerp(enemydisplayedShield[i], targetShield, Time.deltaTime * 8f);

                enemypanel[i].shieldFill.fillAmount = enemydisplayedShield[i];
                enemypanel[i].shieldText.text = $"{enemy.shield} / {enemy.enemyType.maxShield}";
            }
            else
            {
                enemypanel[i].enemyPanel.SetActive(false);
            }
        }
    }

    void UpdateHeroUI()
    {
        for (int i = 0; i < heropanel.Length; i++)
        {
            HeroStats hero = (i < heroes.Length) ? heroes[i] : null;

            if (hero != null && hero.gameObject.activeInHierarchy)
            {
                heropanel[i].heroPanel.SetActive(true);
                heropanel[i].HeroName.text = hero.heroType.heroName;

                // HP
                float targetHP = (float)hero.currentHP / hero.heroType.maxHp;
                herodisplayedHP[i] = Mathf.Lerp(herodisplayedHP[i], targetHP, Time.deltaTime * 8f);

                heropanel[i].hpFill.fillAmount = herodisplayedHP[i];
                heropanel[i].hpText.text = $"{hero.currentHP} / {hero.heroType.maxHp}";

                // SHIELD
                float maxShield = Mathf.Max(1, hero.heroType.maxShield);
                float targetShield = (float)hero.shield / maxShield;

                herodisplayedShield[i] = Mathf.Lerp(herodisplayedShield[i], targetShield, Time.deltaTime * 8f);

                heropanel[i].shieldFill.fillAmount = herodisplayedShield[i];
                heropanel[i].shieldText.text = $"{hero.shield} / {hero.heroType.maxShield}";
            }
            else
            {
                heropanel[i].heroPanel.SetActive(false);
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        UpdateEnemyUI();
        UpdateHeroUI();
    }
}