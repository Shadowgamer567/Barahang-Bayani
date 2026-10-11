using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardType))]
public class CardTypeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        CardType card = (CardType)target;

        SerializedProperty effectProperty =
            serializedObject.FindProperty("effectType");

        SerializedProperty targetProperty =
            serializedObject.FindProperty("targetType");

        // Draw Effect Type first
        EditorGUILayout.PropertyField(effectProperty);

        CardEffectType selectedEffect =
            (CardEffectType)effectProperty.enumValueIndex;

        TargetType[] allowedTargets;

        switch (selectedEffect)
        {
            case CardEffectType.Damage:
            case CardEffectType.Debuff:
                allowedTargets = new TargetType[]
                {
                    TargetType.SingleTarget,
                    TargetType.AllEnemies
                };
                break;

            case CardEffectType.Heal:
            case CardEffectType.Shield:
                allowedTargets = new TargetType[]
                {
                    TargetType.SingleTarget,
                    TargetType.AllHeroes,
                    TargetType.Self
                };
                break;

            case CardEffectType.Buff:
                allowedTargets = new TargetType[]
                {
                    TargetType.Self
                };
                break;

            default:
                allowedTargets = new TargetType[]
                {
                    TargetType.SingleTarget
                };
                break;
        }

        // Find current selection in allowed targets
        int selectedIndex = 0;

        TargetType currentTarget =
            (TargetType)targetProperty.intValue;

        for (int i = 0; i < allowedTargets.Length; i++)
        {
            if (allowedTargets[i] == currentTarget)
            {
                selectedIndex = i;
                break;
            }
        }

        // Display only allowed targets
        string[] options = new string[allowedTargets.Length];

        for (int i = 0; i < allowedTargets.Length; i++)
        {
            options[i] = allowedTargets[i].ToString();
        }

        selectedIndex = EditorGUILayout.Popup(
            "Target Type",
            selectedIndex,
            options
        );

        targetProperty.intValue =
            (int)allowedTargets[selectedIndex];

        // Draw all other CardType fields normally
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",
            "effectType",
            "targetType"
        );

        serializedObject.ApplyModifiedProperties();
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
