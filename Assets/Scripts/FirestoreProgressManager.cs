using Firebase.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class FirestoreProgressManager : MonoBehaviour
{
    public static async Task UploadProgressForNewGame(
        string accountID,
        ProgressData progress)
    {
        Dictionary<string, object> data =
            new Dictionary<string, object>();

        data["username"] = progress.username;
        data["accountType"] = progress.accountType;

        data["completedLevels"] =
            progress.completedLevel;

        data["lastCampaignScene"] =
            progress.lastCampaignScene;

        data["playthroughId"] =
            progress.playthroughId ?? "";

        data["multipleChoiceCorrect"] =
            progress.multipleChoiceCorrect;

        data["multipleChoiceTotal"] =
            progress.multipleChoiceTotal;

        data["identificationCorrect"] =
            progress.identificationCorrect;

        data["identificationTotal"] =
            progress.identificationTotal;

        data["trueFalseCorrect"] =
            progress.trueFalseCorrect;

        data["trueFalseTotal"] =
            progress.trueFalseTotal;

        data["tutorialCompleted"] = 
            progress.tutorialCompleted;

        Debug.Log(
            "Uploading Progress | Account ID: " +
            accountID +
            " | Username: " +
            progress.username
        );

        await FirestoreManager.Instance
            .ProgressCollection()
            .Document(accountID)
            .SetAsync(data);

        Debug.Log(
            "Uploaded Progress for account: " +
            accountID
        );
    }

    public static async Task<bool> UploadProgressIfCurrent(
        string accountID,
        ProgressData progress)
    {
        DocumentReference document =
            FirestoreManager.Instance
                .ProgressCollection()
                .Document(accountID);

        return await FirestoreManager.Instance
            .ProgressCollection()
            .Firestore
            .RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot snapshot =
                    await transaction.GetSnapshotAsync(document);

                if (!snapshot.Exists)
                {
                    Debug.LogWarning(
                        "No cloud progress exists yet. " +
                        "Guarded upload was not performed."
                    );

                    return false;
                }

                string cloudPlaythroughId = "";

                if (snapshot.ContainsField("playthroughId"))
                {
                    cloudPlaythroughId =
                        snapshot.GetValue<string>("playthroughId");
                }

                if (cloudPlaythroughId != progress.playthroughId)
                {
                    Debug.LogWarning(
                        "Upload rejected: Playthrough ID mismatch."
                    );

                    return false;
                }

                Dictionary<string, object> data =
                    new Dictionary<string, object>
                    {
                    { "username", progress.username },
                    { "accountType", progress.accountType },
                    { "completedLevels", progress.completedLevel },
                    { "lastCampaignScene", progress.lastCampaignScene },
                    { "playthroughId", progress.playthroughId },
                    { "multipleChoiceCorrect", progress.multipleChoiceCorrect },
                    { "multipleChoiceTotal", progress.multipleChoiceTotal },
                    { "identificationCorrect", progress.identificationCorrect },
                    { "identificationTotal", progress.identificationTotal },
                    { "trueFalseCorrect", progress.trueFalseCorrect },
                    { "trueFalseTotal", progress.trueFalseTotal },
                    { "tutorialCompleted", progress.tutorialCompleted },
                    { "quizResultLedgerInitialized", progress.quizResultLedgerInitialized }
                    };

                transaction.Set(document, data);

                return true;
            });
    }

    public static async Task<ProgressData> DownloadProgress(
        string accountID)
    {
        DocumentReference document =
            FirestoreManager.Instance
                .ProgressCollection()
                .Document(accountID);

        DocumentSnapshot snapshot =
            await document.GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            Debug.Log(
                "No Firestore progress found for account: " +
                accountID
            );

            return null;
        }

        ProgressData progress = new ProgressData();

        if (snapshot.ContainsField("username"))
        {
            progress.username =
                snapshot.GetValue<string>("username");
        }

        if (snapshot.ContainsField("accountType"))
        {
            progress.accountType =
                snapshot.GetValue<string>("accountType");
        }

        if (snapshot.ContainsField("completedLevels"))
        {
            progress.completedLevel =
                snapshot.GetValue<List<string>>("completedLevels");
        }

        if (snapshot.ContainsField("lastCampaignScene"))
        {
            progress.lastCampaignScene =
                snapshot.GetValue<string>("lastCampaignScene");
        }

        if (snapshot.ContainsField("playthroughId"))
        {
            progress.playthroughId =
                snapshot.GetValue<string>("playthroughId");
        }

        if (snapshot.ContainsField("multipleChoiceCorrect"))
        {
            progress.multipleChoiceCorrect =
                snapshot.GetValue<int>("multipleChoiceCorrect");
        }

        if (snapshot.ContainsField("multipleChoiceTotal"))
        {
            progress.multipleChoiceTotal =
                snapshot.GetValue<int>("multipleChoiceTotal");
        }

        if (snapshot.ContainsField("identificationCorrect"))
        {
            progress.identificationCorrect =
                snapshot.GetValue<int>("identificationCorrect");
        }

        if (snapshot.ContainsField("identificationTotal"))
        {
            progress.identificationTotal =
                snapshot.GetValue<int>("identificationTotal");
        }

        if (snapshot.ContainsField("trueFalseCorrect"))
        {
            progress.trueFalseCorrect =
                snapshot.GetValue<int>("trueFalseCorrect");
        }

        if (snapshot.ContainsField("trueFalseTotal"))
        {
            progress.trueFalseTotal =
                snapshot.GetValue<int>("trueFalseTotal");
        }

        if (snapshot.ContainsField("tutorialCompleted"))
        {
            progress.tutorialCompleted =
                snapshot.GetValue<bool>("tutorialCompleted");
        }

        if (snapshot.ContainsField("quizResultLedgerInitialized"))
        {
            progress.quizResultLedgerInitialized =
                snapshot.GetValue<bool>("quizResultLedgerInitialized");
        }

        Debug.Log(
            "Downloaded Progress for: " +
            progress.username
        );

        return progress;
    }


    public static async Task<bool> ResetPlaythrough(
        string accountID,
        ProgressData newProgress,
        string previousPlaythroughId)
    {
        DocumentReference document =
            FirestoreManager.Instance
                .ProgressCollection()
                .Document(accountID);

        return await FirestoreManager.Instance
            .ProgressCollection()
            .Firestore
            .RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot snapshot =
                    await transaction.GetSnapshotAsync(document);

                string cloudPlaythroughId = "";

                if (snapshot.Exists &&
                    snapshot.ContainsField("playthroughId"))
                {
                    cloudPlaythroughId =
                        snapshot.GetValue<string>("playthroughId");
                }

                // The requested reset has already been applied.
                // Do not overwrite any progress made since that reset.
                if (cloudPlaythroughId == newProgress.playthroughId)
                {
                    Debug.Log(
                        "New Game reset already exists in Firestore. " +
                        "Treating retry as successful."
                    );

                    return true;
                }

                // Another playthrough is active, and it is not the one
                // this reset was originally intended to replace.
                if (cloudPlaythroughId != previousPlaythroughId)
                {
                    Debug.LogWarning(
                        "New Game reset rejected: Firestore has a different playthrough."
                    );

                    return false;
                }

                Dictionary<string, object> data =
                    new Dictionary<string, object>
                    {
                    { "username", newProgress.username },
                    { "accountType", newProgress.accountType },
                    { "completedLevels", newProgress.completedLevel },
                    { "lastCampaignScene", newProgress.lastCampaignScene },
                    { "playthroughId", newProgress.playthroughId },
                    { "multipleChoiceCorrect", 0 },
                    { "multipleChoiceTotal", 0 },
                    { "identificationCorrect", 0 },
                    { "identificationTotal", 0 },
                    { "trueFalseCorrect", 0 },
                    { "trueFalseTotal", 0 },
                    { "tutorialCompleted", false },
                    { "quizResultLedgerInitialized", true }
                    };

                transaction.Set(document, data);

                return true;
            });
    }

    private static CollectionReference QuizResultsCollection(string accountID, string playthroughId)
    {
        return FirestoreManager.Instance
            .ProgressCollection()
            .Document(accountID)
            .Collection("playthroughs")
            .Document(playthroughId)
            .Collection("results");
    }

    public static async Task<bool> UploadQuizResult( string accountID, QuizResultData result)
    {
        if (result == null || string.IsNullOrEmpty(result.resultId) || string.IsNullOrEmpty(result.playthroughId))
        {
            Debug.LogWarning("Invalid quiz result.");
            return false;
        }

        DocumentReference progressDocument =
            FirestoreManager.Instance
                .ProgressCollection()
                .Document(accountID);

        DocumentReference resultDocument =
            QuizResultsCollection(
                accountID,
                result.playthroughId
            ).Document(result.resultId);

        return await progressDocument.Firestore.RunTransactionAsync(async transaction =>
            {
                // Read both documents before writing.
                DocumentSnapshot progressSnapshot =
                    await transaction.GetSnapshotAsync(progressDocument);

                DocumentSnapshot resultSnapshot =
                    await transaction.GetSnapshotAsync(resultDocument);

                if (!progressSnapshot.Exists)
                    return false;

                string cloudPlaythroughId =
                    progressSnapshot.ContainsField("playthroughId")
                        ? progressSnapshot.GetValue<string>("playthroughId")
                        : "";

                // Reject results from previous playthroughs.
                if (cloudPlaythroughId != result.playthroughId)
                {
                    Debug.LogWarning("Quiz result rejected: Playthrough mismatch.");
                    return false;
                }

                // Only accept results from an initialized ledger.
                bool ledgerInitialized =
                    progressSnapshot.ContainsField("quizResultLedgerInitialized") &&
                    progressSnapshot.GetValue<bool>("quizResultLedgerInitialized");

                if (!ledgerInitialized)
                {
                    Debug.LogWarning("Quiz result rejected: Firestore ledger is not initialized.");

                    return false;
                }

                // Already recorded: no need to increment again.
                if (resultSnapshot.Exists)
                    return true;

                string correctField;
                string totalField;

                switch (result.questionType)
                {
                    case "MultipleChoice":
                        correctField = "multipleChoiceCorrect";
                        totalField = "multipleChoiceTotal";
                        break;

                    case "Identification":
                        correctField = "identificationCorrect";
                        totalField = "identificationTotal";
                        break;

                    case "TrueOrFalse":
                        correctField = "trueFalseCorrect";
                        totalField = "trueFalseTotal";
                        break;

                    default:
                        Debug.LogWarning(
                            "Unknown question type: " + result.questionType
                        );
                        return false;
                }

                Dictionary<string, object> updates =
                    new Dictionary<string, object>
                    {
                    { totalField, FieldValue.Increment(1) }
                    };

                if (result.isCorrect)
                {
                    updates[correctField] =
                        FieldValue.Increment(1);
                }

                transaction.Update(progressDocument, updates);

                transaction.Set(
                    resultDocument,
                    new Dictionary<string, object>
                    {
                    { "playthroughId", result.playthroughId },
                    { "questionType", result.questionType },
                    { "isCorrect", result.isCorrect }
                    }
                );

                return true;
            });

    }

    public static async Task<HashSet<string>> GetUploadedQuizResultIds(
        string accountID,
        string playthroughId,
        List<QuizResultData> pendingResults)
    {
        HashSet<string> uploadedIds = new HashSet<string>();

        if (string.IsNullOrEmpty(accountID) ||
            string.IsNullOrEmpty(playthroughId) ||
            pendingResults == null)
        {
            return uploadedIds;
        }

        CollectionReference resultsCollection =
            QuizResultsCollection(accountID, playthroughId);

        foreach (QuizResultData result in pendingResults)
        {
            if (result == null ||
                string.IsNullOrEmpty(result.resultId) ||
                result.playthroughId != playthroughId)
            {
                continue;
            }

            DocumentSnapshot snapshot =
                await resultsCollection
                    .Document(result.resultId)
                    .GetSnapshotAsync(Source.Server);

            if (snapshot.Exists)
            {
                uploadedIds.Add(result.resultId);
            }
        }

        return uploadedIds;
    }


    public static async Task<QuizReconciliationSnapshot> GetQuizReconciliationSnapshot(
            string accountID,
            string playthroughId,
            List<QuizResultData> pendingResults)
    {
        if (string.IsNullOrEmpty(accountID) ||
            string.IsNullOrEmpty(playthroughId))
        {
            return null;
        }

        FirebaseFirestore database =
            FirestoreManager.Instance.ProgressCollection().Firestore;

        DocumentReference progressDocument =
            FirestoreManager.Instance.ProgressCollection()
                .Document(accountID);

        CollectionReference resultsCollection =
            QuizResultsCollection(accountID, playthroughId);

        return await database.RunTransactionAsync(async transaction =>
        {
            DocumentSnapshot progressSnapshot =
                await transaction.GetSnapshotAsync(progressDocument);

            if (!progressSnapshot.Exists ||
                !progressSnapshot.ContainsField("playthroughId") ||
                progressSnapshot.GetValue<string>("playthroughId") != playthroughId)
            {
                return null;
            }

            bool ledgerInitialized =
                progressSnapshot.ContainsField("quizResultLedgerInitialized") &&
                progressSnapshot.GetValue<bool>("quizResultLedgerInitialized");

            if (!ledgerInitialized)
                return null;

            QuizReconciliationSnapshot snapshot =
                new QuizReconciliationSnapshot();

            snapshot.cloudProgress = new ProgressData();
            snapshot.cloudProgress.playthroughId = playthroughId;
            snapshot.cloudProgress.quizResultLedgerInitialized = true;

            snapshot.cloudProgress.multipleChoiceCorrect =
                progressSnapshot.GetValue<int>("multipleChoiceCorrect");

            snapshot.cloudProgress.multipleChoiceTotal =
                progressSnapshot.GetValue<int>("multipleChoiceTotal");

            snapshot.cloudProgress.identificationCorrect =
                progressSnapshot.GetValue<int>("identificationCorrect");

            snapshot.cloudProgress.identificationTotal =
                progressSnapshot.GetValue<int>("identificationTotal");

            snapshot.cloudProgress.trueFalseCorrect =
                progressSnapshot.GetValue<int>("trueFalseCorrect");

            snapshot.cloudProgress.trueFalseTotal =
                progressSnapshot.GetValue<int>("trueFalseTotal");

            HashSet<string> checkedIds = new HashSet<string>();

            if (pendingResults != null)
            {
                foreach (QuizResultData result in pendingResults)
                {
                    if (result == null ||
                        string.IsNullOrEmpty(result.resultId) ||
                        result.playthroughId != playthroughId ||
                        !checkedIds.Add(result.resultId))
                    {
                        continue;
                    }

                    DocumentSnapshot resultSnapshot =
                        await transaction.GetSnapshotAsync(
                            resultsCollection.Document(result.resultId)
                        );

                    if (resultSnapshot.Exists)
                    {
                        snapshot.uploadedResultIds.Add(result.resultId);
                    }
                }
            }

            return snapshot;
        });
    }

}


public class QuizReconciliationSnapshot
{
    public ProgressData cloudProgress;
    public HashSet<string> uploadedResultIds = new HashSet<string>();
}
