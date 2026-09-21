using UnityEngine;

public class SimpleSway : MonoBehaviour
{
    [Header("Orientation Settings")]
    [Tooltip("If true, picks a random Y-rotation on start so bushes don't all face the same way")]
    public bool randomizeFacingDirection = true;

    [Header("Sway Direction")]
    [Tooltip("If true, picks a random direction vector on the X/Z plane to sway towards on start")]
    public bool randomizeSwayAxis = true;

    [Tooltip("Manual sway axis if randomizeSwayAxis is false")]
    public Vector3 customSwayAxis = new Vector3(0, 0, 1);

    [Header("Speed Range")]
    [Tooltip("Minimum and maximum speed multiplier for the sway")]
    public float minSwaySpeed = 1.2f;
    public float maxSwaySpeed = 3.0f;

    [Header("Angle Range")]
    [Tooltip("Minimum and maximum angle of sway in degrees")]
    public float minSwayAngle = 3.0f;
    public float maxSwayAngle = 8.0f;

    [Header("Deformation Settings")]
    [Tooltip("How much the model stretches/squashes (0.05 = 5% change)")]
    public float deformAmount = 0.05f;

    private Quaternion initialRotation;
    private Vector3 initialScale;
    private Vector3 activeSwayAxis;
    private float activeSwaySpeed;
    private float activeSwayAngle;
    private float randomOffset;

    void Start()
    {
        // 1. Randomize facing direction (Y-axis rotation)
        if (randomizeFacingDirection)
        {
            transform.Rotate(0f, Random.Range(0f, 360f), 0f, Space.Self);
        }

        initialRotation = transform.localRotation;
        initialScale = transform.localScale;

        // 2. Randomize sway direction (360 degrees on X/Z plane)
        if (randomizeSwayAxis)
        {
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            activeSwayAxis = new Vector3(Mathf.Cos(randomAngle), 0f, Mathf.Sin(randomAngle));
        }
        else
        {
            activeSwayAxis = customSwayAxis.normalized;
        }

        // 3. Pick random speed and angle for this bush instance
        activeSwaySpeed = Random.Range(minSwaySpeed, maxSwaySpeed);
        activeSwayAngle = Random.Range(minSwayAngle, maxSwayAngle);

        // 4. Randomize starting wave offset
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float time = (Time.time + randomOffset) * activeSwaySpeed;

        // 1. Calculate Sway along the random axis using the randomized angle
        float swayFactor = Mathf.Sin(time) * activeSwayAngle;
        Vector3 currentSwayEuler = activeSwayAxis * swayFactor;

        transform.localRotation = initialRotation * Quaternion.Euler(currentSwayEuler);

        // 2. Squash & Stretch Deformation
        float deformFactor = Mathf.Sin(time * 2.0f) * deformAmount;

        Vector3 newScale = initialScale;
        newScale.y += deformFactor;
        newScale.x -= deformFactor * 0.5f;
        newScale.z -= deformFactor * 0.5f;

        transform.localScale = newScale;
    }
}