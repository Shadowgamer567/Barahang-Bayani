using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Creates an editable, animated achievement toast on the active UI canvas.</summary>
public sealed class AchievementPopupUI : MonoBehaviour
{
    [System.Serializable]
    public class AchievementDefinition
    {
        public string id;
        public string title;
        [TextArea(2, 4)] public string description;
        public Sprite icon;
        public Color badgeColor = new Color(0.91f, 0.65f, 0.22f);
    }

    private static AchievementPopupUI instance;

    [Header("Achievement content")]
    [SerializeField] private List<AchievementDefinition> achievements = new List<AchievementDefinition>
    {
        new AchievementDefinition { id = "complete_tutorial", title = "First Steps", description = "Complete the tutorial and begin your journey.", badgeColor = new Color(0.29f, 0.68f, 0.87f) },
        new AchievementDefinition { id = "triple_elimination", title = "Triple Elimination", description = "Defeat three enemies in a single turn.", badgeColor = new Color(0.85f, 0.32f, 0.27f) },
        new AchievementDefinition { id = "master_healer", title = "Master Healer", description = "Restore 100 HP in one level.", badgeColor = new Color(0.34f, 0.72f, 0.47f) }
    };

    [Header("Panel appearance")]
    [SerializeField] private Vector2 panelSize = new Vector2(540f, 112f);
    [SerializeField] private Vector2 topCenterOffset = new Vector2(0f, -34f);
    [SerializeField] private Color panelColor = new Color(0.12f, 0.10f, 0.08f, 0.97f);
    [SerializeField] private Color titleColor = new Color(1f, 0.82f, 0.40f);
    [SerializeField] private Color descriptionColor = new Color(0.95f, 0.93f, 0.88f);
    [SerializeField] private Sprite panelSprite;
    [SerializeField] private Sprite defaultAchievementIcon;
    [SerializeField] private TMP_FontAsset font;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float slideDistance = 150f;
    [SerializeField, Min(0.01f)] private float slideDuration = 0.42f;
    [SerializeField, Min(0f)] private float visibleDuration = 3.2f;
    [SerializeField] private bool createPopupCanvasIfMissing = true;

    private readonly Queue<string> pending = new Queue<string>();
    private bool showing;

    public static void Show(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId)) return;
        AchievementPopupUI popup = FindFirstObjectByType<AchievementPopupUI>();
        if (popup == null)
        {
            GameObject host = new GameObject("Achievement Popup");
            popup = host.AddComponent<AchievementPopupUI>();
        }
        popup.pending.Enqueue(achievementId);
        popup.ShowNext();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void ShowNext()
    {
        if (showing || pending.Count == 0) return;
        AchievementDefinition definition = FindDefinition(pending.Dequeue());
        if (definition == null) { ShowNext(); return; }
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            if (!createPopupCanvasIfMissing) return;
            GameObject canvasObject = new GameObject("Achievement Popup Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }
        showing = true;
        StartCoroutine(ShowRoutine(canvas.transform, definition));
    }

    private AchievementDefinition FindDefinition(string id)
    {
        foreach (AchievementDefinition definition in achievements)
            if (definition != null && definition.id == id) return definition;
        return null;
    }

    private IEnumerator ShowRoutine(Transform parent, AchievementDefinition definition)
    {
        GameObject root = new GameObject("Achievement Toast - " + definition.id, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = panelSize;
        rect.anchoredPosition = topCenterOffset + Vector2.up * slideDistance;
        Image background = root.GetComponent<Image>();
        background.color = panelColor;
        background.sprite = panelSprite;
        background.type = panelSprite == null ? Image.Type.Simple : Image.Type.Sliced;

        GameObject badge = CreateUIObject("Achievement Badge", root.transform, typeof(Image));
        RectTransform badgeRect = badge.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0f, 0.5f); badgeRect.anchorMax = new Vector2(0f, 0.5f);
        badgeRect.pivot = new Vector2(0f, 0.5f); badgeRect.anchoredPosition = new Vector2(16f, 0f);
        badgeRect.sizeDelta = new Vector2(panelSize.y - 28f, panelSize.y - 28f);
        badge.GetComponent<Image>().color = definition.badgeColor;

        GameObject iconObject = CreateUIObject("Achievement Icon", badge.transform, typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero; iconRect.anchorMax = Vector2.one; iconRect.offsetMin = new Vector2(9f, 9f); iconRect.offsetMax = new Vector2(-9f, -9f);
        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = definition.icon != null ? definition.icon : defaultAchievementIcon;
        if (iconImage.sprite == null)
        {
            string iconName = definition.id == "complete_tutorial" ? "FirstSteps" :
                definition.id == "triple_elimination" ? "TripleElimination" : "MasterHealer";
            Texture2D texture = Resources.Load<Texture2D>("AchievementIcons/" + iconName);
            if (texture != null)
                iconImage.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
        iconImage.color = Color.white;
        if (iconImage.sprite == null) CreateFallbackGlyph(iconObject.transform, definition.id);

        GameObject titleObject = CreateUIObject("Achievement Title", root.transform, typeof(TextMeshProUGUI));
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.5f); titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = new Vector2(panelSize.y + 2f, -4f); titleRect.offsetMax = new Vector2(-20f, -13f);
        TextMeshProUGUI title = titleObject.GetComponent<TextMeshProUGUI>();
        title.text = definition.title; title.fontSize = 25f; title.fontStyle = FontStyles.Bold; title.color = titleColor; title.alignment = TextAlignmentOptions.BottomLeft;
        if (font != null) title.font = font;

        GameObject descriptionObject = CreateUIObject("Achievement Description", root.transform, typeof(TextMeshProUGUI));
        RectTransform descriptionRect = descriptionObject.GetComponent<RectTransform>();
        descriptionRect.anchorMin = Vector2.zero; descriptionRect.anchorMax = new Vector2(1f, 0.5f);
        descriptionRect.offsetMin = new Vector2(panelSize.y + 2f, 12f); descriptionRect.offsetMax = new Vector2(-20f, 4f);
        TextMeshProUGUI description = descriptionObject.GetComponent<TextMeshProUGUI>();
        description.text = definition.description; description.fontSize = 17f; description.color = descriptionColor; description.alignment = TextAlignmentOptions.TopLeft; description.textWrappingMode = TextWrappingModes.Normal;
        if (font != null) description.font = font;

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        yield return Animate(rect, group, topCenterOffset + Vector2.up * slideDistance, topCenterOffset, 0f, 1f, slideDuration);
        yield return new WaitForSecondsRealtime(visibleDuration);
        yield return Animate(rect, group, topCenterOffset, topCenterOffset + Vector2.up * slideDistance, 1f, 0f, slideDuration);
        Destroy(root);
        showing = false;
        ShowNext();
    }

    private IEnumerator Animate(RectTransform rect, CanvasGroup group, Vector2 from, Vector2 to, float alphaFrom, float alphaTo, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            rect.anchoredPosition = Vector2.LerpUnclamped(from, to, eased);
            group.alpha = Mathf.Lerp(alphaFrom, alphaTo, eased);
            yield return null;
        }
        rect.anchoredPosition = to;
        group.alpha = alphaTo;
    }

    private static GameObject CreateUIObject(string name, Transform parent, params System.Type[] components)
    {
        GameObject child = new GameObject(name, components);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static void CreateFallbackGlyph(Transform parent, string id)
    {
        GameObject glyphObject = CreateUIObject("Achievement Glyph", parent, typeof(TextMeshProUGUI));
        RectTransform rect = glyphObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
        TextMeshProUGUI glyph = glyphObject.GetComponent<TextMeshProUGUI>();
        glyph.text = id == "complete_tutorial" ? "★" : id == "triple_elimination" ? "⚔" : "+";
        glyph.fontSize = 38f; glyph.alignment = TextAlignmentOptions.Center; glyph.color = Color.white;
    }
}
