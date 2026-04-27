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

    Queue<GameObject> recentPrefabs = new Queue<GameObject>();
    public int historySize = 2;

    public BackgroundSet currentCampaign;
    public int currentLevel = 1;
    private Dictionary<Transform, GameObject> spawnedObject = new Dictionary<Transform, GameObject>();

    public GroundLoop groundLoop;
    float BackgroundLength;

    public float speed = 7f;
    public bool isMoving = false;

    void Start()
    {
        BackgroundLength = Background1.GetComponent<Renderer>().bounds.size.x;
    }

    void SpawnOnTiles(Transform tile)
    {
        if (currentCampaign == null)
        {
            Debug.LogError("No campaign assigned");
            return;
        }

        GameObject prefab = currentCampaign.GetRandomPrefab(currentLevel);

        int safety = 0;

        while(recentPrefabs.Contains(prefab) && safety < 10)
        {
            prefab = currentCampaign.GetRandomPrefab(currentLevel);
            safety++;
        }

        if (prefab == null)
        {
            Debug.LogWarning("No prefab found for level" + currentLevel);
            return;
        }

        Debug.Log("Spawning: " + prefab.name + " | Level: " + currentLevel + " | Campaign: " + currentCampaign.name);

        if (spawnedObject.ContainsKey(tile) && spawnedObject[tile] != null)
        {
            Destroy(spawnedObject[tile]);
        }

        /*if (backgroundPrefab.Length == 0)
        {
            return;
        }*/

        GameObject obj = Instantiate(prefab, tile);

        obj.transform.localPosition = new Vector3(0, 0f, 0);
        obj.transform.localRotation = Quaternion.identity;

        Vector3 parentScale = tile.lossyScale;

        obj.transform.localScale = new Vector3(
            1f / parentScale.x,
            1f / parentScale.y,
            1f / parentScale.z
        );

        spawnedObject[tile] = obj;

        recentPrefabs.Enqueue(prefab);

        if (recentPrefabs.Count > historySize)
        {
            recentPrefabs.Dequeue();
        }
    }

    public void InitializeTiles()
    {
        SpawnOnTiles(Background1);
        SpawnOnTiles(Background2);
        SpawnOnTiles(Background3);
        SpawnOnTiles(Background4);
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