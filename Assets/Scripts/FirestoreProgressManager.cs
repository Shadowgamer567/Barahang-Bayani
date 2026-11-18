using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Progress access through authenticated website callable functions.
/// Direct client Firestore access is intentionally not used because the
/// production Firestore rules deny all direct client reads and writes.
/// </summary>
public static class FirestoreProgressManager
{
    [Serializable]
    private class ProgressPayload
    {
        public string username;
        public string accountType;
        public List<string> completedLevels;
        public string lastCampaignScene;
        public string playthroughId;
        public bool tutorialCompleted;
        public bool quizResultLedgerInitialized;
        public int multipleChoiceCorrect, multipleChoiceTotal;
        public int identificationCorrect, identificationTotal;
        public int trueFalseCorrect, trueFalseTotal;
        public int multipleSelectCorrect, multipleSelectTotal;
        public int fillInTheBlankCorrect, fillInTheBlankTotal;
    }

    [Serializable]
    private class ProgressEnvelope
    {
        public bool success;
        public bool exists;
        public ProgressPayload progress;
    }

    [Serializable] private class BoolEnvelope { public bool success; }

    [Serializable]
    private class ResetRequest
    {
        public string classroomId;
        public string previousPlaythroughId;
        public ProgressPayload progress;
    }

    [Serializable]
    private class CampaignRequest
    {
        public string classroomId;
        public string playthroughId;
        public List<string> completedLevels;
        public bool tutorialCompleted;
        public string lastCampaignScene;
    }

    [Serializable]
    private class QuizResultRequest
    {
        public string classroomId;
        public QuizResultData result;
    }

    [Serializable]
    private class ReconciliationRequest
    {
        public string classroomId;
        public string playthroughId;
        public List<QuizResultData> pendingResults;
    }

    [Serializable]
    private class ReconciliationEnvelope
    {
        public bool success;
        public ProgressPayload progress;
        public string[] uploadedResultIds;
    }

    private static string GetClassroomId(string accountID)
    {
        GameAuthManager auth = GameAuthManager.Instance;
        if (auth == null || !auth.IsSignedIn)
            throw new InvalidOperationException("A signed-in website account is required.");

        if (!string.Equals(auth.UserUID, accountID, StringComparison.Ordinal))
            throw new InvalidOperationException("Progress account does not match the signed-in user.");

        if (string.IsNullOrWhiteSpace(auth.SelectedClassroomID))
            throw new InvalidOperationException("Select a classroom before accessing progress.");

        return auth.SelectedClassroomID;
    }

    private static ProgressPayload ToPayload(ProgressData progress)
    {
        if (progress == null) throw new ArgumentNullException(nameof(progress));
        return new ProgressPayload
        {
            username = progress.username ?? "",
            accountType = progress.accountType ?? "",
            completedLevels = progress.completedLevel ?? new List<string>(),
            lastCampaignScene = progress.lastCampaignScene ?? "MapMenu",
            playthroughId = progress.playthroughId ?? "",
            tutorialCompleted = progress.tutorialCompleted,
            quizResultLedgerInitialized = progress.quizResultLedgerInitialized,
            multipleChoiceCorrect = progress.multipleChoiceCorrect,
            multipleChoiceTotal = progress.multipleChoiceTotal,
            identificationCorrect = progress.identificationCorrect,
            identificationTotal = progress.identificationTotal,
            trueFalseCorrect = progress.trueFalseCorrect,
            trueFalseTotal = progress.trueFalseTotal,
            multipleSelectCorrect = progress.multipleSelectCorrect,
            multipleSelectTotal = progress.multipleSelectTotal,
            fillInTheBlankCorrect = progress.fillInTheBlankCorrect,
            fillInTheBlankTotal = progress.fillInTheBlankTotal
        };
    }

    private static ProgressData FromPayload(ProgressPayload payload)
    {
        if (payload == null) return null;
        return new ProgressData
        {
            username = payload.username ?? "",
            accountType = payload.accountType ?? "",
            completedLevel = payload.completedLevels ?? new List<string>(),
            lastCampaignScene = payload.lastCampaignScene ?? "MapMenu",
            playthroughId = payload.playthroughId ?? "",
            tutorialCompleted = payload.tutorialCompleted,
            quizResultLedgerInitialized = payload.quizResultLedgerInitialized,
            multipleChoiceCorrect = payload.multipleChoiceCorrect,
            multipleChoiceTotal = payload.multipleChoiceTotal,
            identificationCorrect = payload.identificationCorrect,
            identificationTotal = payload.identificationTotal,
            trueFalseCorrect = payload.trueFalseCorrect,
            trueFalseTotal = payload.trueFalseTotal,
            multipleSelectCorrect = payload.multipleSelectCorrect,
            multipleSelectTotal = payload.multipleSelectTotal,
            fillInTheBlankCorrect = payload.fillInTheBlankCorrect,
            fillInTheBlankTotal = payload.fillInTheBlankTotal
        };
    }

    public static async Task UploadProgressForNewGame(string accountID, ProgressData progress)
    {
        bool success = await ResetPlaythrough(accountID, progress, "");
        if (!success) throw new Exception("The new-game progress was rejected by the server.");
    }

    public static Task<bool> UploadProgressIfCurrent(string accountID, ProgressData progress)
    {
        return UploadCampaignProgress(accountID, progress.playthroughId,
            progress.completedLevel, progress.tutorialCompleted, progress.lastCampaignScene);
    }

    public static async Task<bool> UploadCampaignProgress(
        string accountID, string expectedPlaythroughId,
        List<string> completedLevels, bool tutorialCompleted, string lastCampaignScene)
    {
        CampaignRequest request = new CampaignRequest
        {
            classroomId = GetClassroomId(accountID),
            playthroughId = expectedPlaythroughId,
            completedLevels = completedLevels ?? new List<string>(),
            tutorialCompleted = tutorialCompleted,
            lastCampaignScene = lastCampaignScene ?? ""
        };
        BoolEnvelope response = await WebsiteCallableClient.CallAsync<BoolEnvelope>(
            "saveGameCampaignProgress", JsonUtility.ToJson(request));
        return response != null && response.success;
    }

    public static async Task<ProgressData> DownloadProgress(string accountID)
    {
        string classroomId = GetClassroomId(accountID);
        string json = "{\"classroomId\":\"" + EscapeJson(classroomId) + "\"}";
        ProgressEnvelope response = await WebsiteCallableClient.CallAsync<ProgressEnvelope>(
            "getGameProgress", json);
        return response != null && response.exists ? FromPayload(response.progress) : null;
    }

    public static async Task<bool> ResetPlaythrough(
        string accountID, ProgressData newProgress, string previousPlaythroughId)
    {
        ResetRequest request = new ResetRequest
        {
            classroomId = GetClassroomId(accountID),
            previousPlaythroughId = previousPlaythroughId ?? "",
            progress = ToPayload(newProgress)
        };
        BoolEnvelope response = await WebsiteCallableClient.CallAsync<BoolEnvelope>(
            "resetGamePlaythrough", JsonUtility.ToJson(request));
        return response != null && response.success;
    }

    public static async Task<bool> UploadQuizResult(string accountID, QuizResultData result)
    {
        if (result == null || string.IsNullOrWhiteSpace(result.resultId) ||
            string.IsNullOrWhiteSpace(result.playthroughId)) return false;

        QuizResultRequest request = new QuizResultRequest
        {
            classroomId = GetClassroomId(accountID),
            result = result
        };
        BoolEnvelope response = await WebsiteCallableClient.CallAsync<BoolEnvelope>(
            "recordGameQuizResult", JsonUtility.ToJson(request));
        return response != null && response.success;
    }

    private static async Task<ReconciliationEnvelope> GetReconciliation(
        string accountID, string playthroughId, List<QuizResultData> pendingResults)
    {
        ReconciliationRequest request = new ReconciliationRequest
        {
            classroomId = GetClassroomId(accountID),
            playthroughId = playthroughId,
            pendingResults = pendingResults ?? new List<QuizResultData>()
        };
        return await WebsiteCallableClient.CallAsync<ReconciliationEnvelope>(
            "getGameQuizReconciliation", JsonUtility.ToJson(request));
    }

    public static async Task<HashSet<string>> GetUploadedQuizResultIds(
        string accountID, string playthroughId, List<QuizResultData> pendingResults)
    {
        HashSet<string> ids = new HashSet<string>();
        ReconciliationEnvelope response = await GetReconciliation(accountID, playthroughId, pendingResults);
        if (response != null && response.uploadedResultIds != null)
            foreach (string id in response.uploadedResultIds) ids.Add(id);
        return ids;
    }

    public static async Task<QuizReconciliationSnapshot> GetQuizReconciliationSnapshot(
        string accountID, string playthroughId, List<QuizResultData> pendingResults)
    {
        ReconciliationEnvelope response = await GetReconciliation(accountID, playthroughId, pendingResults);
        if (response == null || response.progress == null) return null;

        QuizReconciliationSnapshot snapshot = new QuizReconciliationSnapshot
        {
            cloudProgress = FromPayload(response.progress)
        };
        if (response.uploadedResultIds != null)
            foreach (string id in response.uploadedResultIds)
                snapshot.uploadedResultIds.Add(id);
        return snapshot;
    }

    private static string EscapeJson(string value)
    {
        return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}

public class QuizReconciliationSnapshot
{
    public ProgressData cloudProgress;
    public HashSet<string> uploadedResultIds = new HashSet<string>();
}
