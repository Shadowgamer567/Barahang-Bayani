using System;
using System.IO;
using UnityEngine;

public static class QuizPackageSyncManager
{
    private static string SyncRoot =>
        Path.Combine(
            Application.persistentDataPath,
            "QuizPackageSync"
        );

    private static string GetSyncFilePath(
        string classroomID,
        string packageID)
    {
        string classroomFolder = Path.Combine(
            SyncRoot,
            classroomID
        );

        return Path.Combine(
            classroomFolder,
            packageID + ".json"
        );
    }

    public static QuizPackageSyncData Load(
        string classroomID,
        string packageID)
    {
        string path = GetSyncFilePath(
            classroomID,
            packageID
        );

        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);

            QuizPackageSyncData data =
                JsonUtility.FromJson<QuizPackageSyncData>(json);

            if (data == null ||
                data.classroomID != classroomID ||
                data.packageID != packageID)
            {
                Debug.LogWarning(
                    "Quiz Package sync metadata mismatch."
                );

                return null;
            }

            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Failed to load Quiz Package sync metadata: " + ex
            );

            return null;
        }
    }

    public static void Save(
        string classroomID,
        string packageID,
        string versionID)
    {
        string path = GetSyncFilePath(
            classroomID,
            packageID
        );

        string folder = Path.GetDirectoryName(path);

        Directory.CreateDirectory(folder);

        QuizPackageSyncData data = new QuizPackageSyncData
        {
            classroomID = classroomID,
            packageID = packageID,
            baseVersionID = versionID,
            lastSyncedAt = DateTime.UtcNow.ToString("O")
        };

        string json = JsonUtility.ToJson(data, true);

        File.WriteAllText(path, json);

        Debug.Log(
            $"Saved Quiz Package sync metadata: " +
            $"{packageID} -> {versionID}"
        );
    }

    public static bool Delete(
        string classroomID,
        string packageID)
    {
        if (string.IsNullOrWhiteSpace(classroomID) ||
            string.IsNullOrWhiteSpace(packageID))
        {
            return false;
        }

        try
        {
            string path = GetSyncFilePath(
                classroomID,
                packageID
            );

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            Debug.Log(
                "Quiz Package synchronization metadata removed: " +
                packageID
            );

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "Failed to remove Quiz Package synchronization metadata: " +
                ex
            );

            return false;
        }
    }

}
