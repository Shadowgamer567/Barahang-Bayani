using TMPro;
using UnityEngine;

public class APDisplay : MonoBehaviour
{
    public TextMeshProUGUI apText;
    public BattleControl battle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        battle.OnAPChanged += UpdateAP;
        UpdateAP(battle.currentActionPoint, battle.maxActionPoint);
    }

    void UpdateAP(int current, int max)
    {
        apText.text = $"Action Points: {current}";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
