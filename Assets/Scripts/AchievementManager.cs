using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Account-wide achievements. Local data is keyed only by the authenticated
/// account UID, never by classroom or playthrough. Cloud access uses callable
/// functions because Firestore rules deny direct client access.
/// </summary>
public static class AchievementManager
{
    [Serializable]
    private class AchievementSave
    {
        public string accountId;
        public List<string> unlockedIds = new List<string>();
    }

    [Serializable]
    private class AchievementRequest
    {
        public List<string> unlockedIds = new List<string>();
    }

    [Serializable]
    private class AchievementResponse
    {
        public bool success;
        public List<string> unlockedIds = new List<string>();
    }

    private static readonly HashSet<string> validIds = new HashSet<string>
    {
        "complete_tutorial",
        "triple_elimination",
        "master_healer"
    };

    private static readonly HashSet<string> unlocked = new HashSet<string>();
    private static string activeAccountId = "";
    private static string SavePath => Path.Combine(Application.persistentDataPath,
        "achievements_" + activeAccountId + ".json");
    private static bool syncInProgress;

    public static bool IsUnlocked(string id) => unlocked.Contains(id);


    public static string GetAchievementDisplayText()
    {
        string[] ids =
        {
            "complete_tutorial",
            "triple_elimination",
            "master_healer"
        };

        string[] titles =
        {
            "Complete the Tutorial",
            "Triple Elimination",
            "Master Healer"
        };

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int unlockedCount = 0;

        for (int i = 0; i < ids.Length; i++)
        {
            bool isUnlocked = unlocked.Contains(ids[i]);
            if (isUnlocked)
                unlockedCount++;

            builder.Append("<sprite name=\"").Append(GetIconName(ids[i])).Append("\">  ");
            builder.Append(isUnlocked ? "[Unlocked] " : "[Locked] ");
            builder.AppendLine(titles[i]);
        }

        return "ACHIEVEMENTS (" + unlockedCount + "/" + ids.Length + ")\n" +
            builder.ToString();
    }

    public static string GetIconName(string id)
    {
        switch (id)
        {
            case "complete_tutorial": return "FirstSteps";
            case "triple_elimination": return "TripleElimination";
            case "master_healer": return "MasterHealer";
            default: return "FirstSteps";
        }
    }

    public static void SetActiveAccount(string accountId)
    {
        if (string.Equals(activeAccountId, accountId, StringComparison.Ordinal))
            return;

        activeAccountId = accountId ?? "";
        unlocked.Clear();
        LevelHealingTotal = 0;

        if (string.IsNullOrWhiteSpace(activeAccountId))
            return;

        LoadLocal();

        // A restored offline session keeps local achievements available.
        // When online-authenticated, reconcile local and cloud unlocks.
        if (IsOnlineAuthenticated())
            _ = SynchronizeAsync();
    }

    public static void Unlock(string id)
    {
        if (string.IsNullOrWhiteSpace(activeAccountId) || !validIds.Contains(id))
            return;

        if (!unlocked.Add(id))
            return;

        SaveLocal();
        Debug.Log("Achievement unlocked: " + id);
        AchievementPopupUI.Show(id);

        if (IsOnlineAuthenticated())
            _ = SynchronizeAsync();
    }

    public static void RecordHealing(int actualHpRestored)
    {
        if (actualHpRestored <= 0 || IsUnlocked("master_healer"))
            return;

        LevelHealingTotal += actualHpRestored;
        if (LevelHealingTotal >= 100)
            Unlock("master_healer");
    }

    private static int LevelHealingTotal;

    public static void BeginLevel()
    {
        LevelHealingTotal = 0;
    }

    private static bool IsOnlineAuthenticated()
    {
        GameAuthManager auth = GameAuthManager.Instance;
        return auth != null && auth.IsOnlineAuthenticated &&
            FirebaseManager.IsAuthenticated;
    }

    private static void LoadLocal()
    {
        try
        {
            if (!File.Exists(SavePath))
                return;

            AchievementSave save = JsonUtility.FromJson<AchievementSave>(
                File.ReadAllText(SavePath));

            if (save == null || save.accountId != activeAccountId ||
                save.unlockedIds == null)
                return;

            foreach (string id in save.unlockedIds)
                if (validIds.Contains(id))
                    unlocked.Add(id);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Could not load local achievements: " + ex.Message);
        }
    }

    private static void SaveLocal()
    {
        if (string.IsNullOrWhiteSpace(activeAccountId))
            return;

        try
        {
            AchievementSave save = new AchievementSave
            {
                accountId = activeAccountId,
                unlockedIds = unlocked.Where(validIds.Contains).OrderBy(id => id).ToList()
            };
            File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));
        }
        catch (Exception ex)
        {
            Debug.LogError("Could not save local achievements: " + ex.Message);
        }
    }

    public static async Task SynchronizeAsync()
    {
        if (syncInProgress || string.IsNullOrWhiteSpace(activeAccountId) ||
            !IsOnlineAuthenticated())
            return;

        string accountAtStart = activeAccountId;
        syncInProgress = true;
        try
        {
            AchievementRequest request = new AchievementRequest
            {
                unlockedIds = unlocked.Where(validIds.Contains).ToList()
            };

            AchievementResponse response =
                await WebsiteCallableClient.CallAsync<AchievementResponse>(
                    "syncGameAchievements", JsonUtility.ToJson(request));

            if (activeAccountId != accountAtStart || response == null ||
                !response.success || response.unlockedIds == null)
                return;

            bool changed = false;
            foreach (string id in response.unlockedIds)
            {
                if (validIds.Contains(id) && unlocked.Add(id))
                    changed = true;
            }

            if (changed)
                SaveLocal();
        }
        catch (Exception ex)
        {
            // Local unlocks remain saved and will be retried at the next login
            // or achievement unlock.
            Debug.LogWarning("Achievement cloud synchronization deferred: " + ex.Message);
        }
        finally
        {
            syncInProgress = false;
        }
    }
}
