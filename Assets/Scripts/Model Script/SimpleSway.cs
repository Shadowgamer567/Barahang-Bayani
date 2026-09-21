using UnityEngine;

public class SimpleBushSwayDeformRotation : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [Header("Orientation Settings")]
    [Tooltip("Base offset added to the initial rotation")]
    public Vector3 baseRotation = Vector3.zero;

    [Header("Sway Settings")]
    [Tooltip("Which axis the model sways back and forth on")]
    public Axis swayAxis = Axis.Z;

    public float swaySpeed = 2.0f;
    public float swayAngle = 5.0f;

    [Header("Deformation Settings")]
    [Tooltip("How much the model stretches/squashes (e.g., 0.05 = 5% change)")]
    public float deformAmount = 0.05f;

    private Quaternion initialRotation;
    private Vector3 initialScale;
    private float randomOffset;

    void Start()
    {
        initialRotation = transform.localRotation;
        initialScale = transform.localScale;
        
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float time = (Time.time + randomOffset) * swaySpeed;

        // 1. Calculate Sway Rotation
        float swayFactor = Mathf.Sin(time) * swayAngle;
        Vector3 swayEuler = Vector3.zero;

        switch (swayAxis)
        {
            case Axis.X: swayEuler.x = swayFactor; break;
            case Axis.Y: swayEuler.y = swayFactor; break;
            case Axis.Z: swayEuler.z = swayFactor; break;
        }

        // Apply Base Rotation offset + Dynamic Sway
        transform.localRotation = initialRotation * Quaternion.Euler(baseRotation) * Quaternion.Euler(swayEuler);

        // 2. Calculate Squash & Stretch Deformation
        float deformFactor = Mathf.Sin(time * 2.0f) * deformAmount;

        Vector3 newScale = initialScale;
        newScale.y += deformFactor;
        newScale.x -= deformFactor * 0.5f;
        newScale.z -= deformFactor * 0.5f;

        transform.localScale = newScale;
    }
}