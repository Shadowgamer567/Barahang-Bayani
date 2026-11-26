
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class TeacherStudentProgressManager : MonoBehaviour
{
    public static TeacherStudentProgressManager Instance;

    [Header("Student Selection UI")]
    [SerializeField] private Transform studentContainer;
    [SerializeField] private GameObject studentCardPrefab;

    [Header("Student Progress UI")]
    private TMP_Text progressDisplayText;

    // Cached logged-in student's own progress display.
    // This survives scene changes with this manager.
    private string cachedOwnStudentProgressText;
    private string cachedOwnStudentUID;

    private readonly List<StudentProgressOverview> students =
        new List<StudentProgressOverview>();

    private int progressRequestVersion;

    public string SelectedStudentAccountID { get; private set; }
    public string SelectedStudentUsername { get; private set; }

    public StudentProgressResult SelectedStudentProgress { get; private set; }

    [Serializable]
    private class ClassroomRequest
    {
        public string classroomId;
    }

    [Serializable]
    private class StudentProgressRequest
    {
        public string classroomId;
        public string studentUID;
    }

    [Serializable]
    private class StudentAchievementsResponse
    {
        public bool success;
        public string studentUID;
        public string[] unlockedIds;
    }

    [Serializable]
    private class StudentOverviewResult
    {
        public bool success;
        public List<StudentProgressOverview> students;
    }

    [Serializable]
    public class StudentProgressOverview
    {
        public string accountId;
        public string username;
        public string accountType;

        public string[] completedLevels;

        public bool tutorialCompleted;
        public string lastCampaignScene;
        public string playthroughId;

        public int totalCorrect;
        public int totalQuestions;
        public float accuracy;
    }

    [Serializable]
    public class StudentProgressResult
    {
        public bool success;
        public string studentUID;
        public string username;

        public bool hasProgress;
        public string status;

        public string[] completedLevels;

        public bool tutorialCompleted;
        public string lastCampaignScene;
        public string playthroughId;

        public QuestionStats questionStats;
        public List<StudentQuestionResult> results;
    }

    [Serializable]
    public class QuestionStats
    {
        public QuestionTypeStats multipleChoice;
        public QuestionTypeStats identification;
        public QuestionTypeStats trueFalse;
        public QuestionTypeStats multipleSelect;
        public QuestionTypeStats fillInTheBlank;
    }

    [Serializable]
    public class QuestionTypeStats
    {
        public int correct;
        public int total;
    }

    [Serializable]
    public class StudentQuestionResult
    {
        public string resultId;
        public string playthroughId;
        public string questionType;
        public bool isCorrect;
    }

    [Serializable]
    private class OwnGameProgressResponse
    {
        public bool success;
        public bool exists;
        public OwnGameProgressData progress;
    }

    [Serializable]
    private class OwnGameProgressData
    {
        public string username;
        public string accountType;
        public string[] completedLevels;
        public bool tutorialCompleted;
        public string lastCampaignScene;
        public string playthroughId;

        public int multipleChoiceCorrect;
        public int multipleChoiceTotal;

        public int identificationCorrect;
        public int identificationTotal;

        public int trueFalseCorrect;
        public int trueFalseTotal;

        public int multipleSelectCorrect;
        public int multipleSelectTotal;

        public int fillInTheBlankCorrect;
        public int fillInTheBlankTotal;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ConnectUI(AccountUIReferences ui)
    {
        studentContainer = ui.studentProgressContainer;
        studentCardPrefab = ui.studentProgressCardPrefab;

        progressDisplayText = ui.progressDisplayText;

        // Restore the logged-in student's cached progress
        // when the Main Menu UI reconnects.
        GameAuthManager auth = GameAuthManager.Instance;

        if (progressDisplayText != null &&
            auth != null &&
            auth.IsSignedIn &&
            auth.IsStudent &&
            !string.IsNullOrEmpty(cachedOwnStudentProgressText) &&
            cachedOwnStudentUID == auth.UserUID)
        {
            progressDisplayText.text = cachedOwnStudentProgressText;

            Debug.Log(
                "[Student Progress] Restored cached progress display."
            );
        }
    }

    public async void LoadClassroomStudents()
    {
        GameAuthManager auth = GameAuthManager.Instance;

        if (auth == null || !auth.IsSignedIn || !auth.IsTeacher)
            return;

        string classroomID = auth.SelectedClassroomID;

        // No selected classroom: safely do nothing.
        if (string.IsNullOrWhiteSpace(classroomID))
            return;

        if (studentContainer == null || studentCardPrefab == null)
        {
            Debug.LogError(
                "[Teacher Progress] Student UI references are missing."
            );
            return;
        }

        ClearStudentCards();

        try
        {
            ClassroomRequest request = new ClassroomRequest
            {
                classroomId = classroomID
            };

            StudentOverviewResult result =
                await WebsiteCallableClient.CallAsync<StudentOverviewResult>(
                    "getClassroomProgressOverview",
                    JsonUtility.ToJson(request)
                );

            Debug.Log(
                "[Teacher Progress] Raw overview response:\n" +
                JsonUtility.ToJson(result)
            );

            if (result != null && result.students != null)
            {
                Debug.Log(
                    "[Teacher Progress] Returned student count: " +
                    result.students.Count
                );

                foreach (var student in result.students)
                {
                    Debug.Log(
                        "[Teacher Progress] Student UIDLoadClassroomStudents(): " +
                        (student?.accountId ?? "NULL") +
                        " | Username: " +
                        (student?.username ?? "NULL")
                    );
                }
            }

            if (result == null || !result.success)
                throw new Exception("Unable to retrieve classroom students.");

            students.Clear();

            if (result.students != null)
                students.AddRange(result.students);

            foreach (StudentProgressOverview student in students)
            {
                if (student == null ||
                    string.IsNullOrWhiteSpace(student.accountId))
                    continue;

                GameObject cardObject = Instantiate(
                    studentCardPrefab,
                    studentContainer
                );

                WebsiteStudentCardUI card =
                    cardObject.GetComponent<WebsiteStudentCardUI>();

                if (card == null)
                {
                    Debug.LogError(
                        "WebsiteStudentCard prefab is missing WebsiteStudentCardUI."
                    );

                    Destroy(cardObject);
                    continue;
                }

                card.Setup(
                    student.accountId,
                    student.username,
                    SelectStudent
                );

                cardObject.SetActive(true);
            }

            Debug.Log(
                "[Teacher Progress] Loaded " +
                students.Count +
                " student progress record(s)."
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "[Teacher Progress] Failed to load students: " +
                exception
            );

            string errorMessage = exception.ToString();

            bool permissionDenied =
                errorMessage.IndexOf(
                    "permission",
                    StringComparison.OrdinalIgnoreCase
                ) >= 0;

            if (permissionDenied)
            {
                // Close Student Selection and open the Progress panel.
                AccountManager.Instance?.ShowSelectedStudentProgress();

                // Display the permission warning.
                if (progressDisplayText != null)
                {
                    progressDisplayText.text =
                        "You do not have permission to view student progress in this classroom.";
                }

                Debug.LogWarning(
                    "[Teacher Progress] Access denied: Missing classroom progress permission."
                );
            }
        }
    }

    private void CacheOwnStudentProgress()
    {
        GameAuthManager auth = GameAuthManager.Instance;

        if (auth == null ||
            !auth.IsSignedIn ||
            !auth.IsStudent ||
            progressDisplayText == null)
        {
            return;
        }

        cachedOwnStudentUID = auth.UserUID;
        cachedOwnStudentProgressText = progressDisplayText.text;
    }

    private void SelectStudent(string accountID)
    {
        StudentProgressOverview selected = students.Find(
            student => student.accountId == accountID
        );

        if (selected == null)
        {
            Debug.LogError(
                "[Teacher Progress] Selected student was not found."
            );
            return;
        }

        SelectedStudentAccountID = selected.accountId;
        SelectedStudentUsername = selected.username;

        SelectedStudentProgress = null;

        Debug.Log(
            "[Teacher Progress] Selected student: " +
            selected.username
        );

        AccountManager.Instance?.ShowSelectedStudentProgress();

        LoadSelectedStudentProgress();
    }

    private async void LoadSelectedStudentProgress()
    {
        GameAuthManager auth = GameAuthManager.Instance;

        if (auth == null || !auth.IsSignedIn || !auth.IsTeacher)
            return;

        string classroomID = auth.SelectedClassroomID;
        string studentUID = SelectedStudentAccountID;

        if (string.IsNullOrWhiteSpace(classroomID) ||
            string.IsNullOrWhiteSpace(studentUID))
            return;

        int requestVersion = ++progressRequestVersion;

        SetProgressLoadingState();

        try
        {
            StudentProgressRequest request = new StudentProgressRequest
            {
                classroomId = classroomID,

                // The overview's accountId contains the website Firebase UID.
                studentUID = studentUID
            };

            StudentProgressResult result =
                await WebsiteCallableClient.CallAsync<StudentProgressResult>(
                    "getStudentClassroomProgress",
                    JsonUtility.ToJson(request)
                );

            // Ignore responses from an earlier student selection.
            if (requestVersion != progressRequestVersion)
                return;

            if (result == null || !result.success)
                throw new Exception("Unable to retrieve student progress.");

            SelectedStudentProgress = result;

            DisplayProgress(result, "Student");
            await AppendSelectedStudentAchievements(classroomID, studentUID, requestVersion);

            Debug.Log(
                "[Teacher Progress] Successfully loaded progress for " +
                result.username
            );
        }
        catch (Exception exception)
        {
            if (requestVersion != progressRequestVersion)
                return;

            Debug.LogError(
                "[Teacher Progress] Failed to load individual progress: " +
                exception
            );

            if (progressDisplayText != null)
                progressDisplayText.text = "Unable to load student progress.";
        }
    }


    private async Task AppendSelectedStudentAchievements(
        string classroomID,
        string studentUID,
        int requestVersion)
    {
        if (progressDisplayText == null)
            return;

        try
        {
            StudentProgressRequest request = new StudentProgressRequest
            {
                classroomId = classroomID,
                studentUID = studentUID
            };

            StudentAchievementsResponse response =
                await WebsiteCallableClient.CallAsync<StudentAchievementsResponse>(
                    "getStudentGameAchievements",
                    JsonUtility.ToJson(request)
                );

            if (requestVersion != progressRequestVersion ||
                SelectedStudentAccountID != studentUID ||
                response == null || !response.success)
                return;

            HashSet<string> unlocked = new HashSet<string>(
                response.unlockedIds ?? Array.Empty<string>()
            );

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

            int count = 0;
            System.Text.StringBuilder display = new System.Text.StringBuilder();
            for (int i = 0; i < ids.Length; i++)
            {
                bool isUnlocked = unlocked.Contains(ids[i]);
                if (isUnlocked) count++;
                display.Append(isUnlocked ? "[Unlocked] " : "[Locked] ");
                display.AppendLine(titles[i]);
            }

            progressDisplayText.text +=
                "\n\nSTUDENT ACHIEVEMENTS (" + count + "/" + ids.Length + ")\n" +
                display.ToString();
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "[Teacher Progress] Could not load student achievements: " + exception.Message
            );
        }
    }

    private void SetProgressLoadingState()
    {
        if (progressDisplayText != null)
        {
            progressDisplayText.text =
                "Student: " + SelectedStudentUsername +
                "\n\nLoading student progress...";
        }
    }

    private void DisplayProgress(StudentProgressResult progress, string accountLabel)
    {
        if (progressDisplayText == null)
        {
            Debug.LogError(
                "[Teacher Progress] Progress display TMP is not assigned."
            );
            return;
        }

        int completedLevels = progress.completedLevels != null
            ? progress.completedLevels.Length
            : 0;

        QuestionStats stats = progress.questionStats;

        int correct =
            GetCorrect(stats?.multipleChoice) +
            GetCorrect(stats?.identification) +
            GetCorrect(stats?.trueFalse) +
            GetCorrect(stats?.multipleSelect) +
            GetCorrect(stats?.fillInTheBlank);

        int total =
            GetTotal(stats?.multipleChoice) +
            GetTotal(stats?.identification) +
            GetTotal(stats?.trueFalse) +
            GetTotal(stats?.multipleSelect) +
            GetTotal(stats?.fillInTheBlank);

        float accuracy = total > 0
            ? (float)correct / total * 100f
            : 0f;

        progressDisplayText.text =
            accountLabel + ": " + progress.username + "\n\n" +

            "PROGRESS\n" +
            "Status: " + progress.status + "\n" +
            "Completed Levels: " + completedLevels + "\n" +
            "Tutorial: " +
            (progress.tutorialCompleted ? "Completed" : "Not Completed") + "\n" +
            "Last Campaign Scene: " +
            (string.IsNullOrEmpty(progress.lastCampaignScene)
                ? "None"
                : progress.lastCampaignScene) + "\n\n" +

            "METRICS\n" +
            "Correct Answers: " + correct + "\n" +
            "Total Questions: " + total + "\n" +
            "Accuracy: " + accuracy.ToString("F1") + "%\n\n" +

            "QUIZ PERFORMANCE BREAKDOWN\n\n" +
            FormatQuestionStats("Multiple Choice", stats?.multipleChoice) + "\n\n" +
            FormatQuestionStats("Identification", stats?.identification) + "\n\n" +
            FormatQuestionStats("True/False", stats?.trueFalse) + "\n\n" +
            FormatQuestionStats("Multiple Select", stats?.multipleSelect) + "\n\n" +
            FormatQuestionStats("Fill in the Blank", stats?.fillInTheBlank);
    }

    private void AppendOwnAchievements()
    {
        if (progressDisplayText == null)
            return;

        progressDisplayText.text += "\n\n" +
            AchievementManager.GetAchievementDisplayText();
    }

    private int GetCorrect(QuestionTypeStats stats)
    {
        return stats != null ? stats.correct : 0;
    }

    private int GetTotal(QuestionTypeStats stats)
    {
        return stats != null ? stats.total : 0;
    }

    private string FormatQuestionStats(
    string label,
    QuestionTypeStats stats)
    {
        int correct = GetCorrect(stats);
        int total = GetTotal(stats);

        float accuracy = total > 0
            ? (float)correct / total * 100f
            : 0f;

        return label + "\n" +
               "Correct Answers: " + correct + "\n" +
               "Total Attempts: " + total + "\n" +
               "Accuracy: " + accuracy.ToString("F1") + "%";
    }

    private void ClearStudentCards()
    {
        if (studentContainer == null)
            return;

        foreach (Transform child in studentContainer)
            Destroy(child.gameObject);

        students.Clear();
    }

    public async void LoadStudentOwnProgress()
    {
        GameAuthManager auth = GameAuthManager.Instance;

        if (auth == null || !auth.IsSignedIn || !auth.IsStudent)
            return;

        string classroomID = auth.SelectedClassroomID;

        if (string.IsNullOrWhiteSpace(classroomID))
        {
            Debug.LogWarning("[Student Progress] No classroom is selected.");
            return;
        }

        if (progressDisplayText == null)
        {
            Debug.LogError("[Student Progress] Progress display TMP is not assigned.");
            return;
        }

        int requestVersion = ++progressRequestVersion;
        progressDisplayText.text = "Loading your progress...";

        try
        {
            ClassroomRequest request = new ClassroomRequest
            {
                classroomId = classroomID
            };

            OwnGameProgressResponse response =
                await WebsiteCallableClient.CallAsync<OwnGameProgressResponse>(
                    "getGameProgress",
                    JsonUtility.ToJson(request)
                );

            if (requestVersion != progressRequestVersion)
                return;

            if (response == null || !response.success)
                throw new Exception("Unable to retrieve your progress.");

            if (!response.exists || response.progress == null)
            {
                progressDisplayText.text =
                    "Student: " + auth.Username + "\n\n" +
                    "PROGRESS\nNo progress has been recorded yet.\n\n" +
                    "METRICS\nNo quiz attempts recorded.";

                AppendOwnAchievements();
                CacheOwnStudentProgress();
                return;
            }

            OwnGameProgressData data = response.progress;

            StudentProgressResult result = new StudentProgressResult
            {
                success = true,
                studentUID = auth.UserUID,
                username = string.IsNullOrWhiteSpace(data.username)
                    ? auth.Username
                    : data.username,
                hasProgress = true,
                status = "In Progress",
                completedLevels = data.completedLevels,
                tutorialCompleted = data.tutorialCompleted,
                lastCampaignScene = data.lastCampaignScene,
                playthroughId = data.playthroughId,

                questionStats = new QuestionStats
                {
                    multipleChoice = new QuestionTypeStats
                    {
                        correct = data.multipleChoiceCorrect,
                        total = data.multipleChoiceTotal
                    },
                    identification = new QuestionTypeStats
                    {
                        correct = data.identificationCorrect,
                        total = data.identificationTotal
                    },
                    trueFalse = new QuestionTypeStats
                    {
                        correct = data.trueFalseCorrect,
                        total = data.trueFalseTotal
                    },
                    multipleSelect = new QuestionTypeStats
                    {
                        correct = data.multipleSelectCorrect,
                        total = data.multipleSelectTotal
                    },
                    fillInTheBlank = new QuestionTypeStats
                    {
                        correct = data.fillInTheBlankCorrect,
                        total = data.fillInTheBlankTotal
                    }
                }
            };
            DisplayProgress(result, "Student");
            AppendOwnAchievements();

            CacheOwnStudentProgress();

            Debug.Log("[Student Progress] Successfully loaded own progress.");
        }
        catch (Exception exception)
        {
            if (requestVersion != progressRequestVersion)
                return;

            Debug.LogError(
                "[Student Progress] Failed to load own progress: " +
                exception
            );

            progressDisplayText.text = "Unable to load your progress.";
        }
    }

    public async void LoadTeacherOwnProgress()
    {
        GameAuthManager auth = GameAuthManager.Instance;

        if (auth == null || !auth.IsSignedIn || !auth.IsTeacher)
            return;

        string classroomID = auth.SelectedClassroomID;

        if (string.IsNullOrWhiteSpace(classroomID))
        {
            Debug.LogWarning(
                "[Teacher Progress] No classroom is selected."
            );
            return;
        }

        int requestVersion = ++progressRequestVersion;

        if (progressDisplayText != null)
            progressDisplayText.text = "Loading your progress...";

        try
        {
            ClassroomRequest request = new ClassroomRequest
            {
                classroomId = classroomID
            };

            StudentProgressResult result =
                await WebsiteCallableClient.CallAsync<StudentProgressResult>(
                    "getMyClassroomProgress",
                    JsonUtility.ToJson(request)
                );

            if (requestVersion != progressRequestVersion)
                return;

            if (result == null || !result.success)
                throw new Exception("Unable to retrieve teacher progress.");

            DisplayProgress(result, "Teacher");
            AppendOwnAchievements();

            Debug.Log(
                "[Teacher Progress] Successfully loaded teacher progress."
            );
        }
        catch (Exception exception)
        {
            if (requestVersion != progressRequestVersion)
                return;

            Debug.LogError(
                "[Teacher Progress] Failed to load teacher progress: " +
                exception
            );

            if (progressDisplayText != null)
                progressDisplayText.text = "Unable to load your progress.";
        }
    }

}
