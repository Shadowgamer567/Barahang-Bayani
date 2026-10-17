using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectTracker : MonoBehaviour
{
    public static string lastLevelSelectScene = "";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lastLevelSelectScene = SceneManager.GetActiveScene().name;

        Debug.Log("LevelSelectTracker: Remembered scene: " + lastLevelSelectScene);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
