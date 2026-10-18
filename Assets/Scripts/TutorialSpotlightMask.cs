
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class TutorialSpotlightMask : MonoBehaviour
{
    [Header("References")]
    public Image overlayImage;
    public RectTransform target;
    public RectTransform secondTarget;

    [Header("Settings")]
    public Vector2 padding = new Vector2(10f, 10f);

    private RectTransform overlayRect;
    private Material runtimeMaterial;
    private readonly Vector3[] corners = new Vector3[4];

    private void Awake()
    {
        overlayRect = GetComponent<RectTransform>();

        if (overlayImage == null)
        {
            Debug.LogError("TutorialSpotlightMask: Overlay Image missing.");
            enabled = false;
            return;
        }

        // Give this spotlight its own material instance.
        runtimeMaterial = new Material(overlayImage.material);
        overlayImage.material = runtimeMaterial;
    }

    private void OnEnable()
    {
        UpdateCutout();
    }

    private void LateUpdate()
    {
        UpdateCutout();
    }

    private void UpdateCutout()
    {
        if (overlayRect == null || runtimeMaterial == null)
            return;

        runtimeMaterial.SetVector(
            "_Cutout",
            CalculateCutout(target)
        );

        runtimeMaterial.SetVector(
            "_Cutout2",
            CalculateCutout(secondTarget)
        );
    }

    private Vector4 CalculateCutout(RectTransform selectedTarget)
    {
        if (selectedTarget == null)
            return Vector4.zero;

        selectedTarget.GetWorldCorners(corners);

        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;

        foreach (Vector3 corner in corners)
        {
            Vector3 local = overlayRect.InverseTransformPoint(corner);

            minX = Mathf.Min(minX, local.x);
            minY = Mathf.Min(minY, local.y);
            maxX = Mathf.Max(maxX, local.x);
            maxY = Mathf.Max(maxY, local.y);
        }

        Rect rect = overlayRect.rect;

        if (rect.width <= 0 || rect.height <= 0)
            return Vector4.zero;

        float left = (minX - padding.x - rect.xMin) / rect.width;
        float bottom = (minY - padding.y - rect.yMin) / rect.height;
        float right = (maxX + padding.x - rect.xMin) / rect.width;
        float top = (maxY + padding.y - rect.yMin) / rect.height;

        return new Vector4(left, bottom, right, top);
    }

    private void OnDestroy()
    {
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}
