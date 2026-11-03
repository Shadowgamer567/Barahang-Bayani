using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EffectIndicatorUI : MonoBehaviour
{
    [Header("UI References")]
    public Image effectIcon;
    public TextMeshProUGUI effectValue;

    [Header("Animation Settings")]
    public float animationDuration = 0.2f;
    public float slideDistance = 30f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void Setup(Sprite icon, int value)
    {
        effectIcon.sprite = icon;
        UpdateValue(value);
    }

    private bool isBleed = false;

    public void SetBleedDisplay()
    {
        isBleed = true;

        if (effectValue != null)
        {
            effectValue.text = effectValue.text.TrimStart('+');
            effectValue.color = new Color32(180, 40, 50, 255);
        }
    }

    public void UpdateValue(int value)
    {
        if (isBleed)
        {
            effectValue.text = value.ToString();
            effectValue.color = new Color32(180, 40, 50, 255);
        }
        else
        {
            effectValue.text = value > 0 ? "+" + value : value.ToString();
            effectValue.color = value > 0 ? Color.green : Color.red;
        }
    }


    public void PlayAppear()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimateAppear());
    }

    private IEnumerator AnimateAppear()
    {
        // Animation is applied to the visual children,
        // leaving the root available for layout positioning.
        RectTransform iconRect = effectIcon.GetComponent<RectTransform>();

        RectTransform valueRect = effectValue.GetComponent<RectTransform>();

        Vector2 iconPosition = iconRect.anchoredPosition;
        Vector2 valuePosition = valueRect.anchoredPosition;

        iconRect.anchoredPosition = iconPosition + Vector2.up * slideDistance;

        valueRect.anchoredPosition = valuePosition + Vector2.up * slideDistance;

        canvasGroup.alpha = 0f;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, animationDuration));

            float eased = t * t * (3f - 2f * t);

            iconRect.anchoredPosition = Vector2.Lerp(iconPosition + Vector2.up * slideDistance, iconPosition, eased);

            valueRect.anchoredPosition = Vector2.Lerp(valuePosition + Vector2.up * slideDistance, valuePosition, eased);

            canvasGroup.alpha = eased;

            yield return null;
        }

        iconRect.anchoredPosition = iconPosition;
        valueRect.anchoredPosition = valuePosition;

        canvasGroup.alpha = 1f;
        animationCoroutine = null;
    }

    public void PlayDisappear()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimateDisappear()
        );
    }

    private IEnumerator AnimateDisappear()
    {
        RectTransform iconRect = effectIcon.GetComponent<RectTransform>();

        RectTransform valueRect = effectValue.GetComponent<RectTransform>();

        Vector2 iconPosition = iconRect.anchoredPosition;
        Vector2 valuePosition = valueRect.anchoredPosition;

        Vector2 iconTarget = iconPosition + Vector2.up * slideDistance;

        Vector2 valueTarget = valuePosition + Vector2.up * slideDistance;

        float startingAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, animationDuration));

            float eased = t * t * (3f - 2f * t);

            iconRect.anchoredPosition = Vector2.Lerp(iconPosition, iconTarget, eased);

            valueRect.anchoredPosition = Vector2.Lerp( valuePosition, valueTarget, eased);

            canvasGroup.alpha = Mathf.Lerp(startingAlpha, 0f, eased);

            yield return null;
        }

        Destroy(gameObject);
    }
}
