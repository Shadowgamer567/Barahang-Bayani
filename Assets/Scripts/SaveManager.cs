using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    private string savePath;

    private void Awake()
    {
        // Sets the path to: C:/Users/[User]/AppData/LocalLow/[Company]/[Game]
        savePath = Application.persistentDataPath + "/savefile.json";
    }
    public CardPanelManager cards;
    public EnemyStats enemy_health;
    public HeroStats hero_health;

    public void SaveGame(int currentHP, int cardCount)
    {
        GameData data = new GameData();
        cards.SyncToData(data);
        enemy_health.SyncToData(data);
        hero_health.SyncToData(data);


        // Convert the object to a JSON string
        string json = JsonUtility.ToJson(data, true);

        // Write the string to a file
        File.WriteAllText(savePath, json);
        Debug.Log("Game Saved to: " + savePath);
    }

    public GameData LoadGame()
    {
        if (File.Exists(savePath))
        {
            // Read the JSON string from the file
            string json = File.ReadAllText(savePath);

            // Convert the JSON back into a GameData object
            GameData data = JsonUtility.FromJson<GameData>(json);
            Debug.Log("Game Loaded!");
            return data;
        }
        else
        {
            Debug.LogWarning("Save file not found.");
            return null;
        }
    }
}