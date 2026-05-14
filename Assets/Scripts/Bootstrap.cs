using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    public GameObject gameManagerPrefab;
    public GameObject progressManagerPrefab;
    public GameObject quizstatsPrefab;
    public GameObject accountManagerPrefab;

    void Awake()
    {
        if (GameManager.Instance == null)
        {
            Instantiate(gameManagerPrefab);
            Debug.Log("GameManager created by Bootstrap");
        }

        if (ProgressManager.Instance == null)
        {
            Instantiate(progressManagerPrefab);
            Debug.Log("ProgressManager created by Bootstrap");
        }

        if (QuizStats.Instance == null)
        {
            Instantiate(quizstatsPrefab);
            Debug.Log("QuizStats created by Bootstrap");
        }

        if (AccountManager.Instance == null)
        {
            Instantiate(accountManagerPrefab);
            Debug.Log("AccountManager created by Bootstrap");
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
