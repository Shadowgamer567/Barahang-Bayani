using System.Collections.Generic;
using UnityEngine;

public class BackgroundLoop : MonoBehaviour
{
 // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform Background1;
    public Transform Background2;
    public Transform Background3;
    public Transform Background4;
    public Transform endMarker;

    public GameObject[] backgroundPrefab;
    private Dictionary<Transform, GameObject> spawnedObject = new Dictionary<Transform, GameObject>();

    public GroundLoop groundLoop;
    float BackgroundLength;

    public float speed = 7f;
    public bool isMoving = false;

    void Start()
    {
        BackgroundLength = Background1.GetComponent<Renderer>().bounds.size.x;

        SpawnOnTiles(Background1);
        SpawnOnTiles(Background2);
        SpawnOnTiles(Background3);
        SpawnOnTiles(Background4);
    }

    void SpawnOnTiles(Transform tile)
    {
        if (spawnedObject.ContainsKey(tile) && spawnedObject[tile] != null)
        {
            Destroy(spawnedObject[tile]);
        }

        if (backgroundPrefab.Length == 0)
        {
            return;
        }

        GameObject prefab = backgroundPrefab[Random.Range(0, backgroundPrefab.Length)];

        GameObject obj = Instantiate(prefab, tile);

        obj.transform.localPosition = new Vector3(0, 0.5f, 0);
        obj.transform.localRotation = Quaternion.identity;

        spawnedObject[tile] = obj;
    }

    // Update is called once per frame
    private void Update()
    {
        if (groundLoop != null)
        {
            isMoving = groundLoop.isMoving;
        }
 
        if (!isMoving)
        {
            return;
        }

        Background1.Translate(Vector3.right * speed * Time.deltaTime, Space.World);
        Background2.Translate(Vector3.right * speed * Time.deltaTime, Space.World);
        Background3.Translate(Vector3.right * speed * Time.deltaTime, Space.World);
        Background4.Translate(Vector3.right * speed * Time.deltaTime, Space.World);

        if (Background1.position.x + BackgroundLength / -2 >= endMarker.position.x)
        {
            Background1.position = new Vector3(Background4.position.x - BackgroundLength, Background1.position.y, Background1.position.z);
            SpawnOnTiles(Background1);
        }

        if (Background2.position.x + BackgroundLength / -2 >= endMarker.position.x)
        {
            Background2.position = new Vector3(Background1.position.x - BackgroundLength, Background2.position.y, Background2.position.z);
            SpawnOnTiles(Background2);
        }

        if (Background3.position.x + BackgroundLength / -2 >= endMarker.position.x)
        {
            Background3.position = new Vector3(Background2.position.x - BackgroundLength, Background3.position.y, Background3.position.z);
            SpawnOnTiles(Background3);
        }

        if (Background4.position.x + BackgroundLength / -2 >= endMarker.position.x)
        {
            Background4.position = new Vector3(Background3.position.x - BackgroundLength, Background3.position.y, Background3.position.z);
            SpawnOnTiles(Background4);
        }
    }
}