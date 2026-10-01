using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class SimpleTreeUpperSway : MonoBehaviour
{
    [Header("Tree Height Masking")]
    public float heightCutoff = 7f;
    public float bendPower = 1.5f;

    [Header("Sway Direction")]
    public bool randomizeSwayAxis = true;
    public Vector3 customSwayAxis = new Vector3(1, 0, 0);

    [Header("Speed & Angle Ranges")]
    public float minSwaySpeed = 1.0f;
    public float maxSwaySpeed = 2.5f;
    public float minSwayDistance = 0.2f;
    public float maxSwayDistance = 0.6f;

    [Header("Deformation Settings")]
    public float deformAmount = 0.05f;

    private Mesh originalMesh;
    private Mesh clonedMesh;
    private Vector3[] baseVertices;
    private Vector3[] displacedVertices;

    private Vector3 activeSwayAxis;
    private float activeSwaySpeed;
    private float activeSwayDistance;
    private float randomOffset;

    void Start()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        originalMesh = meshFilter.sharedMesh;
        clonedMesh = Instantiate(originalMesh);
        meshFilter.mesh = clonedMesh;

        baseVertices = originalMesh.vertices;
        displacedVertices = new Vector3[baseVertices.Length];

        if (randomizeSwayAxis)
        {
            float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            activeSwayAxis = new Vector3(Mathf.Cos(randomAngle), 0f, Mathf.Sin(randomAngle));
        }
        else
        {
            activeSwayAxis = customSwayAxis.normalized;
        }

        activeSwaySpeed = Random.Range(minSwaySpeed, maxSwaySpeed);
        activeSwayDistance = Random.Range(minSwayDistance, maxSwayDistance);
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float time = (Time.time + randomOffset) * activeSwaySpeed;

        float swayWave = Mathf.Sin(time) * activeSwayDistance;
        float deformWave = Mathf.Sin(time * 2.0f) * deformAmount;

        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 vertex = baseVertices[i];

            if (vertex.y > heightCutoff)
            {
                float heightFactor = Mathf.Pow(vertex.y - heightCutoff, bendPower);
                Vector3 offset = activeSwayAxis * (swayWave * heightFactor);
                offset.y += deformWave * heightFactor;

                displacedVertices[i] = vertex + offset;
            }
            else
            {
                displacedVertices[i] = vertex;
            }
        }

        clonedMesh.vertices = displacedVertices;
        clonedMesh.RecalculateBounds();
    }
}