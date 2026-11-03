using JetBrains.Annotations;
using System;
using UnityEngine;

public class ActionModifier : MonoBehaviour
{
    public int pendingModifier = 0;
    public int PendingModifier => pendingModifier;

    //Notifies UI whenever the Modifier changes
    public event Action<int> OnModifierChanged;
    public void AddModifier(int amount)
    {
        pendingModifier += amount;

        Debug.Log(gameObject.name + " modifier changed to " + pendingModifier);

        OnModifierChanged?.Invoke(pendingModifier);
    }

    public int ConsumedModifier(int baseValue)
    {
        int finalValue = Mathf.Max(0, baseValue + pendingModifier);

        Debug.Log(gameObject.name + " action name: " + baseValue + " + (" + pendingModifier + ") = " + finalValue);

        pendingModifier = 0;

        OnModifierChanged?.Invoke(pendingModifier);

        return finalValue;
    }

    public int GetModifier()
    {
        return pendingModifier;
    }

    public void ClearModifier()
    {
        pendingModifier = 0;

        OnModifierChanged?.Invoke(pendingModifier);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
