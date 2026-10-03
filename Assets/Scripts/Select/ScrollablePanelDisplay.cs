using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;

public class ScrollablePanelDisplay : MonoBehaviour
{
    [Header("UI Element References")]
    [SerializeField] private TMP_Text titleComponent;
    [SerializeField] private TMP_Text bodyComponent;
    [SerializeField] private Image imageComponent;
    [SerializeField] private Image panelBackgroundImage;
    [SerializeField] private VerticalLayoutGroup verticalLayoutGroup;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField, Min(1f)] private float scrollSensitivity = 20f;

    [Header("Default Content")]
    [SerializeField] private ScrollableContentData currentContent;

    private void Awake()
    {
        if (scrollRect != null)
        {
            scrollRect.scrollSensitivity = scrollSensitivity;
        }
    }

    private void Start()
    {
        if (currentContent != null)
        {
            DisplayContent(currentContent);
        }
    }

    public void DisplayContent(ScrollableContentData data)
    {
        if (data == null) return;

        currentContent = data;

        // 1. Text Setup
        bool hasTitle = !string.IsNullOrEmpty(data.titleText);
        bool hasBody = !string.IsNullOrEmpty(data.bodyText);
        bool shareTextComponent = titleComponent != null && titleComponent == bodyComponent;
        bool shareGameObject = titleComponent != null &&
            bodyComponent != null &&
            titleComponent.gameObject == bodyComponent.gameObject;

        if (shareTextComponent)
        {
            titleComponent.gameObject.SetActive(hasTitle || hasBody);
            titleComponent.richText = true;
            titleComponent.color = Color.white;
            titleComponent.fontSize = hasTitle ? data.titleFontSize : data.bodyFontSize;
            titleComponent.alignment = hasTitle ? data.titleAlignment : data.bodyAlignment;

            string formattedTitle = FormatText(
                data.titleText, data.titleColor, data.titleFontSize, data.titleAlignment);
            string formattedBody = FormatText(
                data.bodyText, data.bodyTextColor, data.bodyFontSize, data.bodyAlignment);
            titleComponent.text = hasTitle && hasBody
                ? formattedTitle + "\n\n" + formattedBody
                : formattedTitle + formattedBody;
        }
        else
        {
            if (shareGameObject)
            {
                titleComponent.gameObject.SetActive(hasTitle || hasBody);
            }

            ConfigureText(titleComponent, data.titleText, data.titleColor, data.titleFontSize,
                data.titleAlignment, !shareGameObject);
            ConfigureText(bodyComponent, data.bodyText, data.bodyTextColor, data.bodyFontSize,
                data.bodyAlignment, !shareGameObject);
        }

        // 2. Image Resizing & Positioning Setup
        if (imageComponent != null)
        {
            if (data.displayImage != null && data.imagePosition != ImagePosition.Hidden)
            {
                imageComponent.gameObject.SetActive(true);
                imageComponent.sprite = data.displayImage;
                imageComponent.color = data.imageTint;
                imageComponent.preserveAspect = data.preserveAspectRatio;

                // Ensure LayoutElement exists for automatic vertical layout handling
                LayoutElement layoutElement = imageComponent.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = imageComponent.gameObject.AddComponent<LayoutElement>();
                }

                ApplyImageSizing(data, layoutElement);

                PositionImage(data.imagePosition);
            }
            else
            {
                imageComponent.gameObject.SetActive(false);
            }
        }

        // 3. Panel Styling & Layout
        if (panelBackgroundImage != null)
        {
            panelBackgroundImage.color = data.panelBackgroundColor;
        }

        if (verticalLayoutGroup != null)
        {
            verticalLayoutGroup.padding = data.panelPadding;
            verticalLayoutGroup.spacing = data.elementSpacing;
        }

        // 4. Force UI Refresh & Scroll Reset
        Canvas.ForceUpdateCanvases();

        if (verticalLayoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(
                verticalLayoutGroup.GetComponent<RectTransform>());
        }

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private static void ConfigureText(
        TMP_Text component,
        string text,
        Color color,
        float fontSize,
        TextAlignmentOptions alignment,
        bool controlGameObjectActivation)
    {
        if (component == null) return;

        if (controlGameObjectActivation)
        {
            component.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        component.text = text;
        component.color = color;
        component.fontSize = fontSize;
        component.alignment = alignment;
    }

    private static string FormatText(
        string text,
        Color color,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        string colorHex = ColorUtility.ToHtmlStringRGBA(color);
        string size = fontSize.ToString("0.##", CultureInfo.InvariantCulture);
        return "<align=" + GetAlignmentTag(alignment) + "><size=" + size +
            "><color=#" + colorHex + ">" + text + "</color></size></align>";
    }

    private static string GetAlignmentTag(TextAlignmentOptions alignment)
    {
        switch (alignment)
        {
            case TextAlignmentOptions.TopLeft:
            case TextAlignmentOptions.Left:
            case TextAlignmentOptions.MidlineLeft:
            case TextAlignmentOptions.BottomLeft:
            case TextAlignmentOptions.BaselineLeft:
                return "left";
            case TextAlignmentOptions.TopRight:
            case TextAlignmentOptions.Right:
            case TextAlignmentOptions.MidlineRight:
            case TextAlignmentOptions.BottomRight:
            case TextAlignmentOptions.BaselineRight:
                return "right";
            case TextAlignmentOptions.TopJustified:
            case TextAlignmentOptions.Justified:
            case TextAlignmentOptions.MidlineJustified:
            case TextAlignmentOptions.BottomJustified:
            case TextAlignmentOptions.BaselineJustified:
                return "justified";
            case TextAlignmentOptions.TopFlush:
            case TextAlignmentOptions.Flush:
            case TextAlignmentOptions.MidlineFlush:
            case TextAlignmentOptions.BottomFlush:
            case TextAlignmentOptions.BaselineFlush:
                return "flush";
            case TextAlignmentOptions.TopGeoAligned:
            case TextAlignmentOptions.MidlineGeoAligned:
            case TextAlignmentOptions.BottomGeoAligned:
            case TextAlignmentOptions.BaselineGeoAligned:
                return "geometry";
            default:
                return "center";
        }
    }

    private void PositionImage(ImagePosition imagePosition)
    {
        Transform imageTransform = imageComponent.transform;
        Transform parent = imageTransform.parent;
        if (parent == null) return;

        int firstTextIndex = int.MaxValue;
        int lastTextIndex = -1;
        int imageSiblingIndex = imageTransform.GetSiblingIndex();
        UpdateTextSiblingRange(titleComponent, parent, ref firstTextIndex, ref lastTextIndex);
        UpdateTextSiblingRange(bodyComponent, parent, ref firstTextIndex, ref lastTextIndex);

        if (imagePosition == ImagePosition.AboveText)
        {
            int targetIndex = firstTextIndex == int.MaxValue
                ? 0
                : firstTextIndex - (imageSiblingIndex < firstTextIndex ? 1 : 0);
            imageTransform.SetSiblingIndex(targetIndex);
        }
        else if (imagePosition == ImagePosition.BelowText)
        {
            int targetIndex = lastTextIndex < 0
                ? parent.childCount - 1
                : lastTextIndex + (imageSiblingIndex > lastTextIndex ? 1 : 0);
            imageTransform.SetSiblingIndex(targetIndex);
        }
    }

    private static void UpdateTextSiblingRange(
        TMP_Text textComponent,
        Transform parent,
        ref int firstTextIndex,
        ref int lastTextIndex)
    {
        if (textComponent == null || textComponent.transform.parent != parent) return;

        int siblingIndex = textComponent.transform.GetSiblingIndex();
        firstTextIndex = Mathf.Min(firstTextIndex, siblingIndex);
        lastTextIndex = Mathf.Max(lastTextIndex, siblingIndex);
    }

    private void ApplyImageSizing(ScrollableContentData data, LayoutElement layoutElement)
    {
        RectTransform imgRect = imageComponent.rectTransform;

        switch (data.sizeMode)
        {
            case ImageSizeMode.ExplicitSize:
                // Set fixed width and height via LayoutElement
                layoutElement.preferredWidth = data.customImageSize.x;
                layoutElement.preferredHeight = data.customImageSize.y;
                imgRect.sizeDelta = data.customImageSize;
                break;

            case ImageSizeMode.MatchPanelWidth:
                // Width scales to container; calculate height based on sprite aspect ratio
                float parentWidth = ((RectTransform)imageComponent.transform.parent).rect.width;
                if (parentWidth <= 0) parentWidth = 300f; // Fallback default

                float spriteAspect = data.displayImage.rect.width / data.displayImage.rect.height;
                float calculatedHeight = parentWidth / spriteAspect;

                layoutElement.preferredWidth = parentWidth;
                layoutElement.preferredHeight = calculatedHeight;
                imgRect.sizeDelta = new Vector2(parentWidth, calculatedHeight);
                break;

            case ImageSizeMode.NativeSize:
                imageComponent.SetNativeSize();
                layoutElement.preferredWidth = imgRect.sizeDelta.x;
                layoutElement.preferredHeight = imgRect.sizeDelta.y;
                break;
        }
    }
}