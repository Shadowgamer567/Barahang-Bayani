using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DynamicTextReplacer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private ScrollRect scrollRect;

    [Header("TMP Sprite Settings")]
    [Tooltip("Optional: Assign a specific TMP Sprite Asset if not using default.")]
    [SerializeField] private TMP_SpriteAsset customSpriteAsset;

    private void Start()
    {
        if (customSpriteAsset != null)
        {
            contentText.spriteAsset = customSpriteAsset;
        }

        // Example initial load
        SetPanelContent("Welcome! <sprite name=\"icon_star\"> Scroll down to see more.\n\n" +
                        "Here is an inline image: <sprite name=\"icon_shield\"> Shield Item.\n\n" +
                        "You can add as much body text as needed, and the scroll view will expand dynamically.");
    }

    /// <summary>
    /// Replaces the panel content text dynamically and resets scroll position to the top.
    /// </summary>
    /// <param name="newText">Text containing rich text tags and <sprite name="SpriteName"></param>
    public void SetPanelContent(string newText)
    {
        if (contentText == null) return;

        contentText.text = newText;

        // Force UI update so the Content Size Fitter recalculates heights immediately
        Canvas.ForceUpdateCanvases();

        // Scroll back to top
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}