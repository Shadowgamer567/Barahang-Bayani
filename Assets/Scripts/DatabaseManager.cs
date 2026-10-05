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

    public void SetDatabase(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            Debug.LogError("Cannot set database: database name is empty.");
            return;
        }

        CurrentDatabase = databaseName.Trim();

        SaveDatabaseConfig();

        Debug.Log("Current Database: " + CurrentDatabase);
    }

    public void CreateDatabase(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            Debug.LogError("Cannot create database: database name is empty.");
            return;
        }

        databaseName = databaseName.Trim();

        SetDatabase(databaseName);

        Debug.Log("Created/selected classroom database: " + CurrentDatabase);
    }

    public void SaveDatabaseConfig()
    {
        DatabaseConfig config = new DatabaseConfig();

        config.currentDatabase = CurrentDatabase;

        string json = JsonUtility.ToJson(config, true);

        File.WriteAllText(ConfigPath, json);

        Debug.Log("Saved database config: " + CurrentDatabase);
    }

    public void LoadDatabaseConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            CurrentDatabase = "DefaultDatabase";

            SaveDatabaseConfig();

            Debug.Log("No database config found. Using DefaultDatabase.");

            return;
        }

        string json = File.ReadAllText(ConfigPath);

        DatabaseConfig config =
            JsonUtility.FromJson<DatabaseConfig>(json);

        if (config == null ||
            string.IsNullOrWhiteSpace(config.currentDatabase))
        {
            CurrentDatabase = "DefaultDatabase";

            SaveDatabaseConfig();
        }
        else
        {
            CurrentDatabase = config.currentDatabase;
        }

        Debug.Log("Loaded database: " + CurrentDatabase);
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
