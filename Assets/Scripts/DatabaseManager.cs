using System.IO;
using UnityEngine;

public class DatabaseManager : MonoBehaviour
{

    public static DatabaseManager Instance;

    public string CurrentDatabase { get; private set; }

    string ConfigPath => Application.persistentDataPath + "/database_config.json";

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadDatabaseConfig();
        }

        else
        {
            Destroy(gameObject);
        }
    }

    void SetDatabase(string databaseName)
    {
        CurrentDatabase = databaseName;

        SaveDatabaseConfig();

        Debug.Log("Current Database: " + CurrentDatabase);
    }

    public void SaveDatabaseConfig()
    {
        DatabaseConfig config = new DatabaseConfig();

        config.currentDatabase = CurrentDatabase;

        string json = JsonUtility.ToJson(config, true);

        File.WriteAllText(ConfigPath, json);
    }

    public void LoadDatabaseConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            CurrentDatabase = "DefaultDatabase";

            SaveDatabaseConfig();

            return;
        }

        string json = File.ReadAllText(ConfigPath);

        DatabaseConfig config = JsonUtility.FromJson<DatabaseConfig>(json);

        CurrentDatabase = config.currentDatabase;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DatabaseManager.Instance.SetDatabase("BSIT701");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
