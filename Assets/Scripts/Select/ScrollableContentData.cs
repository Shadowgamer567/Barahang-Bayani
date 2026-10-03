using UnityEngine;
using TMPro;

public enum ImageSizeMode
{
    ExplicitSize,      // Use manual Width x Height
    MatchPanelWidth,   // Stretch to panel width, auto-calculate height using aspect ratio
    NativeSize         // Use original sprite pixels
}

public enum ImagePosition
{
    AboveText,
    BelowText,
    Hidden
}

[CreateAssetMenu(fileName = "NewCustomContentData", menuName = "UI/Custom Content Data", order = 1)]
public class ScrollableContentData : ScriptableObject
{
    [Header("Content Header")]
    public string titleText = "Section Title";
    public Color titleColor = Color.white;
    [Range(12, 72)] public float titleFontSize = 28f;
    public TextAlignmentOptions titleAlignment = TextAlignmentOptions.TopLeft;

    [Header("Body Text")]
    [TextArea(5, 15)]
    public string bodyText = "Enter body text here...";
    public Color bodyTextColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    [Range(10, 48)] public float bodyFontSize = 16f;
    public TextAlignmentOptions bodyAlignment = TextAlignmentOptions.TopLeft;

    [Header("Image Settings")]
    public Sprite displayImage;
    public ImagePosition imagePosition = ImagePosition.AboveText;
    public Color imageTint = Color.white;

    [Header("Image Resizing & Sizing")]
    public ImageSizeMode sizeMode = ImageSizeMode.ExplicitSize;

    [Tooltip("Used when Size Mode is set to Explicit Size.")]
    public Vector2 customImageSize = new Vector2(300f, 200f);

    [Tooltip("Maintains the original sprite aspect ratio when resizing.")]
    public bool preserveAspectRatio = true;

    [Header("Layout & Panel Styling")]
    public Color panelBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
    public RectOffset panelPadding = new RectOffset(15, 15, 15, 15);
    public float elementSpacing = 10f;
}