using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CardTransitionAnimator : MonoBehaviour
{
    [Header("References")]
    public RectTransform transitionLayer;
    public RectTransform fullCardTarget;

    [Header("Animation Settings")]
    public float transitionDuration = 0.3f;

    private GameObject animatedCard;

    public IEnumerator AnimateCard(RectTransform source, RectTransform destination, bool reverse = false)
    {
        if (source == null || destination == null || transitionLayer == null)
        {
            yield break;
        }

        if (animatedCard != null)
        {
            Destroy(animatedCard);
        }

        // Create a visual copy of the selected hand card.
        animatedCard = Instantiate(source.gameObject, transitionLayer);
        animatedCard.SetActive(true);

        // Disable gameplay and hover interactions on the copy.
        CardUI cardUI = animatedCard.GetComponent<CardUI>();

        if (cardUI != null)
        {
            cardUI.enabled = false;
        }

        CardHoverAnimation hover = animatedCard.GetComponent<CardHoverAnimation>();

        if (hover != null)
        {
            hover.enabled = false;
        }

        foreach (Button button in animatedCard.GetComponentsInChildren<Button>())
        {
            button.interactable = false;
        }

        CanvasGroup canvasGroup = animatedCard.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = animatedCard.AddComponent<CanvasGroup>();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        RectTransform animatedRect = animatedCard.GetComponent<RectTransform>();

        // Remove the copied hover enlargement.
        animatedRect.localScale = Vector3.one;

        // Get source and destination positions in transition-layer space.
        Vector2 startPosition = GetLocalCenter(source);
        Vector2 endPosition = GetLocalCenter(destination);

        Vector2 startSize = GetLocalSize(source);
        Vector2 endSize = GetLocalSize(destination);

        if (reverse)
        {
            (startPosition, endPosition) = (endPosition, startPosition);
            (startSize, endSize) = (endSize, startSize);
        }

        animatedRect.anchorMin = new Vector2(0.5f, 0.5f);
        animatedRect.anchorMax = new Vector2(0.5f, 0.5f);
        animatedRect.pivot = new Vector2(0.5f, 0.5f);

        animatedRect.anchoredPosition = startPosition;
        animatedRect.sizeDelta = startSize;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, transitionDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            animatedRect.anchoredPosition = Vector2.Lerp(startPosition, endPosition, t);

            animatedRect.sizeDelta = Vector2.Lerp(startSize, endSize, t);

            yield return null;
        }

        Destroy(animatedCard);
        animatedCard = null;
    }

    private Vector2 GetLocalCenter(RectTransform rect)
    {
        Vector3 worldCenter = rect.TransformPoint(rect.rect.center);

        return transitionLayer.InverseTransformPoint(worldCenter);
    }

    private Vector2 GetLocalSize(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);

        Vector3 bottomLeft = transitionLayer.InverseTransformPoint(corners[0]);

        Vector3 topRight = transitionLayer.InverseTransformPoint(corners[2]);

        return new Vector2(Mathf.Abs(topRight.x - bottomLeft.x), Mathf.Abs(topRight.y - bottomLeft.y)
        );
    }
}
