using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class TreeUpperSwayScript : MonoBehaviour
{
    [Header("Sway Settings")]
    public float swaySpeed = 2.0f;
    public float swayAmount = 0.15f;
    public float heightCutoff = 2.0f;
    public float bendPower = 0.5f;

    [Header("Deformation Settings")]
    public float deformAmount = 0.03f;

    private Mesh originalMesh;
    private Mesh clonedMesh;
    private Vector3[] baseVertices;
    private Vector3[] displacedVertices;

    void Start()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        originalMesh = meshFilter.sharedMesh;
        clonedMesh = Instantiate(originalMesh);
        meshFilter.mesh = clonedMesh;

        baseVertices = originalMesh.vertices;
        displacedVertices = new Vector3[baseVertices.Length];
    }

    void Update()
    {
        float time = Time.time * swaySpeed;
        float swayWave = Mathf.Sin(time) * swayAmount;
        float deformWave = Mathf.Sin(time * 2.0f) * deformAmount;

        for (int i = 0; i < baseVertices.Length; i++)
        {
            Vector3 pos = baseVertices[i];

            float heightFactor = Mathf.Max(0.0f, pos.y - heightCutoff);
            heightFactor = Mathf.Pow(heightFactor, bendPower);

            pos.x += swayWave * heightFactor;
            pos.z += Mathf.Cos(time * 0.7f) * (swayAmount * 0.5f) * heightFactor;

            pos.y += deformWave * heightFactor;

            displacedVertices[i] = pos;
        }

        clonedMesh.vertices = displacedVertices;
        clonedMesh.RecalculateBounds();
    }
}