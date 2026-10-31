using UnityEngine;
using UnityEngine.EventSystems;

public class CardHoverAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    public float hoverScale = 1.12f;
    public float animationSpeed = 12f;

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector3 targetScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        originalScale = rectTransform.localScale;
        targetScale = originalScale;
    }

    private void Update()
    {
        rectTransform.localScale = Vector3.Lerp(
            rectTransform.localScale,
            targetScale,
            Time.unscaledDeltaTime * animationSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    private void OnDisable()
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
        }

        targetScale = originalScale;
    }

    void Start()
    {

    }
}
