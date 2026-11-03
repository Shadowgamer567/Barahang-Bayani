using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectContainerUI : MonoBehaviour
{
    [Header("Effect Indicator Prefab")]
    public EffectIndicatorUI effectIndicatorPrefab;

    private class ActiveIndicator
    {
        public EffectIndicatorUI indicator;
        public int priority;
        public int value;
    }

    private readonly Dictionary<string, ActiveIndicator> activeIndicators = new Dictionary<string, ActiveIndicator>();

    public void SetEffect(string effectID, Sprite icon, int value, int priority)
    {
        if (string.IsNullOrEmpty(effectID))
        {
            return;
        }

        // A value of zero means the effect is inactive.
        if (value == 0)
        {
            RemoveEffect(effectID);
            return;
        }

        // Update an existing effect without replaying its animation.
        if (activeIndicators.TryGetValue(effectID, out ActiveIndicator existing))
        {
            if (existing.indicator == null)
            {
                activeIndicators.Remove(effectID);
            }
            else
            {
                if (existing.value != value)
                {
                    existing.indicator.UpdateValue(value);
                    existing.value = value;
                }

                if (existing.priority != priority)
                {
                    existing.priority = priority;
                    RefreshOrder();
                }

                return;
            }
        }

        if (effectIndicatorPrefab == null)
        {
            Debug.LogError("EffectContainerUI is missing its EffectIndicator prefab.", this);
            return;
        }

        EffectIndicatorUI newIndicator = Instantiate(effectIndicatorPrefab, transform);

        newIndicator.Setup(icon, value);

        activeIndicators[effectID] = new ActiveIndicator
        {
            indicator = newIndicator,
            priority = priority,
            value = value
        };

        RefreshOrder();
        newIndicator.PlayAppear();
    }

    public void RemoveEffect(string effectID)
    {
        if (!activeIndicators.TryGetValue(effectID, out ActiveIndicator entry))
        {
            return;
        }

        activeIndicators.Remove(effectID);

        if (entry.indicator != null)
        {
            // Remove the outgoing indicator from the layout immediately,
            // while keeping it visible long enough to animate out.
            StartCoroutine(AnimateRemoval(entry.indicator));
        }

        RefreshOrder();
    }

    private IEnumerator AnimateRemoval(EffectIndicatorUI indicator)
    {
        if (indicator == null)
        {
            yield break;
        }

        RectTransform rect = indicator.GetComponent<RectTransform>();

        // Preserve the indicator's current world position.
        Vector3 worldPosition = rect.position;
        Quaternion worldRotation = rect.rotation;

        // Remove it from the Grid Layout Group.
        rect.SetParent(transform.parent, true);

        // Restore its original visual position.
        rect.position = worldPosition;
        rect.rotation = worldRotation;

        // Play the disappearance animation.
        indicator.PlayDisappear();

        yield break;
    }

    private void RefreshOrder()
    {
        List<KeyValuePair<string, ActiveIndicator>> sorted = new List<KeyValuePair<string, ActiveIndicator>>(activeIndicators);

        sorted.Sort((a, b) =>
        {
            int comparison = a.Value.priority.CompareTo(b.Value.priority);

            if (comparison != 0)
            {
                return comparison;
            }
            return string.CompareOrdinal(a.Key, b.Key);
        });

        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i].Value.indicator != null)
            {
                sorted[i].Value.indicator.transform.SetSiblingIndex(i);
            }
        }
    }
}
