//Handles Primary UI display and displaly logic

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class EnemyPanel
{
    public GameObject enemyPanel;
    public TextMeshProUGUI EnemyName;
    public Image hpFill;
    public TextMeshProUGUI hpText;
}

[System.Serializable]
public class HeroPanel
{
    public GameObject heroPanel;
    public TextMeshProUGUI HeroName;
    public Image hpFill;
    public TextMeshProUGUI hpText;
}
public class UIMain : MonoBehaviour
{
    public EnemyPanel[] enemypanel;
    public EnemyStats[] enemies;
    public HeroPanel[] heropanel;
    public HeroStats[] heroes;


    private float[] enemydisplayedHP;
    private float[] herodisplayedHP;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemydisplayedHP = new float[enemypanel.Length];
        herodisplayedHP = new float[heropanel.Length];

        for(int i = 0; i < enemypanel.Length; i++)
        {
            enemypanel[i].enemyPanel.SetActive(false);
            enemydisplayedHP[i] = 1f;
        }

        for (int i = 0; i < heropanel.Length; i++)
        {
            heropanel[i].heroPanel.SetActive(false);
            herodisplayedHP[i] = 1f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < enemypanel.Length; i++)
        {
            EnemyStats enemy = (i < enemies.Length) ? enemies[i] : null;

            if (enemy != null && enemy.gameObject.activeInHierarchy)
            {
                enemypanel[i].enemyPanel.SetActive(true);
                enemypanel[i].EnemyName.text = enemy.enemyType.enemyName;

                float targetHP = (float)enemy.currentHP / enemy.enemyType.maxHp;
                float speed = 5f + Mathf.Abs(enemydisplayedHP[i] - targetHP) * 10f;
                enemydisplayedHP[i] = Mathf.Lerp(enemydisplayedHP[i], targetHP, Time.deltaTime * speed);

                enemypanel[i].hpFill.fillAmount = enemydisplayedHP[i];
                enemypanel[i].hpText.text = $"{enemy.currentHP} / {enemy.enemyType.maxHp}";
            }

            /*else
            {
                if (displayedHP[i] > 0f)
                {
                    displayedHP[i] = Mathf.Lerp(displayedHP[i], 0f, Time.deltaTime * 10f);
                    enemypanel[i].hpFill.fillAmount = displayedHP[i];
                    enemypanel[i].hpText.text = $"0/{(i < enemies.Length ? enemies[i].enemyType.maxHp : 0)}";
                }
                else
                {
                    enemypanel[i].enemyPanel.SetActive(false);
                }
            }*/
        }

        for (int e = 0; e < heropanel.Length; e++)
        {
            HeroStats hero = (e < heroes.Length) ? heroes[e] : null;

            if (hero != null && hero.gameObject.activeInHierarchy)
            {
                heropanel[e].heroPanel.SetActive(true);
                heropanel[e].HeroName.text = hero.heroType.heroName;

                float targetHP = (float)hero.currentHP / hero.heroType.maxHp;
                float speed = 5f + Mathf.Abs(herodisplayedHP[e] - targetHP) * 10f;
                herodisplayedHP[e] = Mathf.Lerp(herodisplayedHP[e], targetHP, Time.deltaTime * speed);

                heropanel[e].hpFill.fillAmount = herodisplayedHP[e];
                heropanel[e].hpText.text = $"{hero.currentHP} / {hero.heroType.maxHp}";
            }


            /*else
            {
                if (displayedHP[i] > 0f)
                {
                    displayedHP[i] = Mathf.Lerp(displayedHP[i], 0f, Time.deltaTime * 10f);
                    enemypanel[i].hpFill.fillAmount = displayedHP[i];
                    enemypanel[i].hpText.text = $"0/{(i < enemies.Length ? enemies[i].enemyType.maxHp : 0)}";
                }
                else
                {
                    enemypanel[i].enemyPanel.SetActive(false);
                }
            }*/
        }
    }
}

// Backup of Dynamic Code, Use in case of infinite expansion
/*using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class EnemyPanelManager : MonoBehaviour
{
    public Transform template; // Assign EnemyUI_Template here
    public Transform panelParent; // Assign the EnemyPanel itself

    private Dictionary<EnemyStats, GameObject> enemyUIs = new Dictionary<EnemyStats, GameObject>();

    void Update()
    {
        // Find all active enemies
        EnemyStats[] enemies = FindObjectsOfType<EnemyStats>();

        // Add UI for new enemies
        foreach (EnemyStats enemy in enemies)
        {
            if (!enemy.gameObject.activeInHierarchy) continue;

            if (!enemyUIs.ContainsKey(enemy))
            {
                // Create new UI entry
                GameObject uiGO = Instantiate(template.gameObject, panelParent);
                uiGO.SetActive(true);

                enemyUIs[enemy] = uiGO;
            }

            // Update UI
            UpdateEnemyUI(enemy, enemyUIs[enemy]);
        }

        // Remove UI for dead or disabled enemies
        List<EnemyStats> toRemove = new List<EnemyStats>();
        foreach (var kvp in enemyUIs)
        {
            if (!kvp.Key.gameObject.activeInHierarchy)
            {
                Destroy(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }
        foreach (var enemy in toRemove)
        {
            enemyUIs.Remove(enemy);
        }
    }

    void UpdateEnemyUI(EnemyStats enemy, GameObject uiGO)
    {
        TextMeshProUGUI nameText = uiGO.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
        Image hpFill = uiGO.transform.Find("HP_Background/HP_Fill").GetComponent<Image>();
        TextMeshProUGUI hpText = uiGO.transform.Find("HP_Background/HP_Text").GetComponent<TextMeshProUGUI>();

        nameText.text = enemy.enemyType.enemyName;

        float hpPercent = (float)enemy.CurrentHP / enemy.enemyType.maxHp;
        hpFill.fillAmount = hpPercent;

        if (hpText != null)
            hpText.text = $"{enemy.CurrentHP}/{enemy.enemyType.maxHp}";
    }
} */