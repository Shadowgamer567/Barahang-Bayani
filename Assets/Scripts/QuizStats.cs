using System.Collections.Generic;
using UnityEngine;

public class QuizStats : MonoBehaviour
{
    public static QuizStats Instance;

    public int totalQuestions = 0;
    public int totalCorrect = 0;

    public Dictionary<InputType, int> typeTotal = new Dictionary<InputType, int>();
    public Dictionary<InputType, int> typeCorrect = new Dictionary<InputType, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            foreach (InputType type in System.Enum.GetValues(typeof(InputType)))
            {
                typeTotal[type] = 0;
                typeCorrect[type] = 0;
            }
        }

        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterQuestion(InputType type)
    {

        if (!typeTotal.ContainsKey(type))
        {
            typeTotal[type] = 0;
        }

        typeTotal[type]++;
        totalQuestions++;
    }

    public void RegisterCorrect(InputType type)
    {

        if (!typeCorrect.ContainsKey(type))
        {
            typeCorrect[type] = 0;
        }

        typeCorrect[type]++;
        totalCorrect++;
    }

    public int GetTotalType(InputType type)
    {
        return typeTotal.ContainsKey(type) ? typeTotal[type] : 0;
    }

    public int GetTypeCorrect(InputType type)
    {
        return typeCorrect.ContainsKey(type) ? typeCorrect[type] : 0;
    }

    public void ResetStats()
    {
        totalQuestions = 0;
        totalCorrect = 0;
        typeTotal.Clear();
        typeCorrect.Clear();


        foreach (InputType type in System.Enum.GetValues(typeof(InputType)))
        {
            typeTotal[type] = 0;
            typeCorrect[type] = 0;
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
