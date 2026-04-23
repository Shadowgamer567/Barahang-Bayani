using UnityEngine;

public class GroundLoop : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Transform Ground1;
    public Transform Ground2;
    public Transform Ground3;
    public Transform Ground4;
    public Transform endMarker;

    float groundLength;

    public float speed = 7f;
    public bool isMoving = false;

    void Start()
    {
        groundLength = Ground1.GetComponent<Renderer>().bounds.size.x;
    }

    // Update is called once per frame
    private void Update()
    {
        if(!isMoving)
        {
            return;
        }
        Ground1.Translate(Vector3.right * speed * Time.deltaTime, Space.World);
        Ground2.Translate(Vector3.right * speed * Time.deltaTime, Space.World);
        Ground3.Translate(Vector3.right * speed * Time.deltaTime, Space.World);
        Ground4.Translate(Vector3.right * speed * Time.deltaTime, Space.World);

        if (Ground1.position.x + groundLength / -2 >= endMarker.position.x)
        {
            Ground1.position = new Vector3(Ground4.position.x - groundLength, Ground1.position.y, Ground1.position.z);
        }

        if (Ground2.position.x + groundLength / -2 >= endMarker.position.x)
        {
            Ground2.position = new Vector3(Ground1.position.x - groundLength, Ground2.position.y, Ground2.position.z);
        }

        if (Ground3.position.x + groundLength / -2 >= endMarker.position.x)
        {
            Ground3.position = new Vector3(Ground2.position.x - groundLength, Ground3.position.y, Ground3.position.z);
        }

        if (Ground4.position.x + groundLength / -2 >= endMarker.position.x)
        {
            Ground4.position = new Vector3(Ground3.position.x - groundLength, Ground3.position.y, Ground3.position.z);
        }

    }
}
