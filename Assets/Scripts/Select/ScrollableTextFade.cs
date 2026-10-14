using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class ScrollableTextFade : MonoBehaviour
{
    [SerializeField] private RectTransform viewport;
    [SerializeField, Min(1f)] private float fadeHeight = 180f;

    private TMP_Text textComponent;
    private Color32[][] originalColors;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();

        if (viewport == null)
        {
            Debug.LogError("ScrollableTextFade requires a viewport reference.", this);
            enabled = false;
            return;
        }

        textComponent.OnPreRenderText += CacheOriginalColors;
        textComponent.ForceMeshUpdate();
        CacheOriginalColors(textComponent.textInfo);
    }

    private void OnDestroy()
    {
        if (textComponent != null)
        {
            textComponent.OnPreRenderText -= CacheOriginalColors;
        }
    }

    private void LateUpdate()
    {
        if (originalColors == null || textComponent.textInfo == null)
        {
            return;
        }

        TMP_TextInfo textInfo = textComponent.textInfo;
        int characterCount = textInfo.characterCount;
        float fadeStart = viewport.rect.yMin;
        float fadeEnd = fadeStart + fadeHeight;

        for (int i = 0; i < characterCount; i++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[i];
            if (!character.isVisible)
            {
                continue;
            }

            int materialIndex = character.materialReferenceIndex;
            int vertexIndex = character.vertexIndex;

            if (materialIndex >= originalColors.Length ||
                materialIndex >= textInfo.meshInfo.Length)
            {
                continue;
            }

            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
            Color32[] baseColors = originalColors[materialIndex];
            Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32;

            for (int vertex = 0; vertex < 4; vertex++)
            {
                int colorIndex = vertexIndex + vertex;
                if (colorIndex >= baseColors.Length || colorIndex >= vertexColors.Length)
                {
                    continue;
                }

                Vector3 worldPosition = textComponent.transform.TransformPoint(
                    vertices[colorIndex]);
                float viewportY = viewport.InverseTransformPoint(worldPosition).y;
                float alpha = Mathf.Clamp01((viewportY - fadeStart) / (fadeEnd - fadeStart));

                Color32 color = baseColors[colorIndex];
                color.a = (byte)Mathf.RoundToInt(color.a * alpha);
                vertexColors[colorIndex] = color;
            }
        }

        textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private void CacheOriginalColors(TMP_TextInfo textInfo)
    {
        originalColors = new Color32[textInfo.meshInfo.Length][];

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            Color32[] colors = textInfo.meshInfo[i].colors32;
            originalColors[i] = new Color32[colors.Length];
            System.Array.Copy(colors, originalColors[i], colors.Length);
        }
    }
}
