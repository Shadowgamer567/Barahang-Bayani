using JetBrains.Annotations;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AccountManager : MonoBehaviour
{
    public Transform studentContainer;
    public Transform teacherContainer;

    public AccountCardUI selectedCard;

    public AccountData selectedAccount;

    public GameObject accountCardPrefab;

    public TMP_InputField usernameInputField;
    public TMP_InputField passwordInputField;

    public TMP_InputField classroomNameInput;

    private AccountType pendingAccountType;

    public static AccountManager Instance;

    [Header("Panels")]
    public GameObject accountButton;
    public GameObject backButton;
    public GameObject accountSelectPanel;
    public GameObject accountManagementPanel;
    public GameObject createAccountPanel;
    public GameObject mainMenu;
    public GameObject classroomPanel;
    public GameObject classroomSelectionPanel;
    public GameObject classroomCreationPanel;

    [Header("Teacher Monitoring")]
    public GameObject teacherInfoPanel;
    public GameObject studentInfoPanel;

    public GameObject assignStudentsButton;
    public GameObject confirmAssignButton;

    private bool assigningStudents = false;
    public bool IsAssigningStudents => assigningStudents;

    private bool waitingToStartGame = false;
    private bool waitingToLoadGame = false;

    private AccountData assigningTeacher;
    private List<AccountData> pendingStudents = new();

    [Header("Teacher Info")]
    public TextMeshProUGUI teacherNameText;
    public TextMeshProUGUI teacherSectionText;
    public TMP_Text teacherProgressText;
    public TMP_Text teacherMetricsText;

    public Transform assignedStudentContainer;

    [Header("Student Info")]
    public TextMeshProUGUI completedLevelsText;

    public TextMeshProUGUI quizStatsText;

    public AccountRegistry registry = new();

    private AccountData pendingLoginAccount;
    public GameObject loginPanel;
    public TMP_InputField loginPasswordInput;
    public TMP_Text loginUsernameText;

    public GameObject cardDeck;

    [Header("Recovery Code Display")]
    public GameObject accountRecoveryDisplay;
    public TMP_Text recoveryCodeText;
    public Button confirmRecoveryCodeButton;

    [Header("Account Recovery")]
    public GameObject accountRecoveryPanel;
    public Button accountRecoveryButton;
    public Button cancelAccountRecoveryButton;
    public TMP_InputField recoveryCodeInputField;
    public TMP_InputField newPasswordInputField;
    public TMP_InputField confirmPasswordInputField;
    public Button confirmAccountRecoveryButton;

    private string pendingRecoveryCode;
    private bool creatingAccount = false;
    private bool synchronizingPendingAccounts = false;

    private string registryPath => Application.persistentDataPath + "/accounts.json";

    private string pendingAccountsPath => Application.persistentDataPath + "/pending_accounts.json";

    private PendingAccountUploads pendingAccountUploads = new PendingAccountUploads();
    private string pendingAssignmentsPath => Application.persistentDataPath + "/pending_assignments.json";

    private PendingAccountUploads pendingAssignmentUploads = new PendingAccountUploads();

    private bool synchronizingAssignments = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);

            EnsureAccountsFileExists();
        }

        else
        {
            Destroy(gameObject);
        }

        EnsureAccountsFileExists();
        LoadPendingAccountUploads();
        LoadPendingAssignmentUploads();
    }

    public void LoadAccount()
    {
        if (!File.Exists(registryPath))
        {
            SaveAccount();
            return;
        }

        string json = File.ReadAllText(registryPath);

        registry = JsonUtility.FromJson<AccountRegistry>(json);

        if (registry == null)
        {
            registry = new AccountRegistry();
        }
    }

    public void SaveAccount()
    {
        string json = JsonUtility.ToJson(registry, true);

        File.WriteAllText(registryPath, json);
    }
    public async void CreateStudentAccount()
    {
        bool success = await CreateAccount(AccountType.Student);

        if (!success)
        {
            Debug.LogWarning("Student account creation failed.");
        }
    }

    public async void CreateTeacherAccount()
    {
        bool success = await CreateAccount(AccountType.Teacher);

        if (!success)
        {
            Debug.LogWarning("Teacher account creation failed.");
        }
    }

    public Task<bool> CreateAccount(AccountType type)
    {
        string username = usernameInputField.text.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogWarning("No Username Entered");
            return Task.FromResult(false);
        }

        string password = passwordInputField.text.Trim();

        if (string.IsNullOrWhiteSpace(password))
        {
            Debug.LogWarning("No Password Entered");
            return Task.FromResult(false);
        }

        AccountData newAccount = new AccountData();

        newAccount.id = System.Guid.NewGuid().ToString();
        newAccount.username = username;
        newAccount.passwordHash = HashPassword(password);
        newAccount.accountType = type;

        string recoveryCode = GenerateRecoveryCode();

        newAccount.recoveryCodeHash = HashRecoveryCode(recoveryCode);
        newAccount.recoveryCodeEnabled = true;

        try
        {
            // Save locally first, without waiting for Firestore.
            registry.accounts.Add(newAccount);

            // Record the account as awaiting Firestore upload.
            pendingAccountUploads.accountIDs.Add(newAccount.id);

            try
            {
                SavePendingAccountUploads();
                SaveAccount();
            }

            catch
            {
                pendingAccountUploads.accountIDs.Remove(newAccount.id);
                SavePendingAccountUploads();
                throw;
            }

            // Only expose the recovery code after local saving succeeds.
            pendingRecoveryCode = recoveryCode;

            Debug.Log(
                "Created account locally: " + newAccount.username
            );
        }
        catch (System.Exception ex)
        {
            registry.accounts.Remove(newAccount);

            pendingRecoveryCode = null;

            Debug.LogError(
                "Failed to save account locally: " + ex.Message
            );

            return Task.FromResult(false);
        }

        usernameInputField.text = "";
        passwordInputField.text = "";

        RefreshUI();

        return Task.FromResult(true);
    }


    public void DeleteSelected()
    {
        if (selectedAccount == null)
            return;

        string saveFile = Application.persistentDataPath + "/progress_" + selectedAccount.id + ".json";

        if (File.Exists(saveFile))
        {
            File.Delete(saveFile);
        }

        registry.accounts.RemoveAll(a => a.id == selectedAccount.id);

        foreach (AccountData account in registry.accounts)
        {
            if (account.assignedStudentIDs != null)
            {
                account.assignedStudentIDs.Remove(selectedAccount.id);
            }
        }

        selectedAccount = null;
        selectedCard = null;

        if (IsAccountPendingUpload(selectedAccount.id))
        {
            Debug.LogWarning("Cannot delete an account while its initial upload is pending.");
            return;
        }

        SaveAccount();

        RefreshUI();
    }

    public void RefreshUI()
    {
        Debug.Log("RefreshUI Started");

        if (studentContainer == null)
        {
            Debug.LogError("Student Container NULL");
            return;
        }

        if (teacherContainer == null)
        {
            Debug.LogError("Teacher Container NULL");
            return;
        }

        if (accountCardPrefab == null)
        {
            Debug.LogError("Account Card Prefab NULL");
            return;
        }

        // Clear old cards
        for (int i = studentContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(studentContainer.GetChild(i).gameObject);
        }

        for (int i = teacherContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(teacherContainer.GetChild(i).gameObject);
        }

        Debug.Log("Cleared old cards");

        foreach (AccountData account in registry.accounts)
        {
            Transform parent =
                account.accountType == AccountType.Student
                ? studentContainer
                : teacherContainer;

            GameObject obj =
                Instantiate(accountCardPrefab, parent);

            obj.SetActive(true);

            Debug.Log("Instantiated card object");

            AccountCardUI card =
                obj.GetComponent<AccountCardUI>();

            if (card == null)
            {
                Debug.LogError("AccountCardUI component missing");
                continue;
            }

            card.Setup(account);

            Debug.Log("Spawned UI Card for: " + account.username);
        }

        RefreshAccountHighlights();
        Debug.Log("RefreshUI Finished");
    }

    public void RefreshAccountHighlights()
    {
        string activeAccountID =
            CurrentAccount.ActiveAccount != null
            ? CurrentAccount.ActiveAccount.id
            : null;

        RefreshContainerHighlights(studentContainer, activeAccountID);
        RefreshContainerHighlights(teacherContainer, activeAccountID);
    }

    private void RefreshContainerHighlights(
        Transform container,
        string activeAccountID)
    {
        if (container == null)
            return;

        foreach (Transform child in container)
        {
            AccountCardUI card = child.GetComponent<AccountCardUI>();

            if (card == null)
                continue;

            AccountData account = card.GetAccountData();

            if (account == null)
                continue;

            bool shouldHighlight;

            if (assigningStudents &&
                account.accountType == AccountType.Student)
            {
                shouldHighlight = pendingStudents.Exists(
                    student => student.id == account.id
                );
            }
            else
            {
                shouldHighlight =
                    !string.IsNullOrEmpty(activeAccountID) &&
                    account.id == activeAccountID;
            }

            card.SetSelected(shouldHighlight);
        }
    }

    public async void SubmitUsername()
    {
        // Prevent multiple submissions.
        if (creatingAccount)
        {
            return;
        }

        creatingAccount = true;

        try
        {
            bool accountCreated = await CreateAccount(pendingAccountType);

            if (!accountCreated)
            {
                Debug.LogWarning("Account creation failed.");
                return;
            }

            // Prevent additional submissions after successful creation.
            Button submitButton = null;

            if (usernameInputField != null)
            {
                submitButton = usernameInputField.GetComponentInParent<Button>();
            }

            // The account has already been created successfully.
            // Keep the submission guard active until the recovery code is confirmed.

            // Display the generated recovery code.
            recoveryCodeText.text = pendingRecoveryCode;

            accountRecoveryDisplay.SetActive(true);

            Debug.Log("Account created. Recovery code displayed.");
        }

        catch (System.Exception ex)
        {
            Debug.LogError("Unexpected account creation error: " + ex.Message);
        }

        finally
        {
            // Keep submissions blocked while the recovery code is displayed.
            if (!accountRecoveryDisplay.activeSelf)
            {
                creatingAccount = false;
            }
        }
    }

    public void ConfirmRecoveryCode()
    {
        pendingRecoveryCode = null;

        recoveryCodeText.text = "";

        creatingAccount = false;

        accountRecoveryDisplay.SetActive(false);

        createAccountPanel.SetActive(false);

        accountSelectPanel.SetActive(true);

        ShowAccountManagementPanel();

        backButton.SetActive(true);

        RefreshUI();
    }

    private void ReIndexAccounts()
    {
        for (int i = 0; i < registry.accounts.Count; i++)
        {
            registry.accounts[i].id = i.ToString();
        }
    }

    public void SelectAccount(AccountCardUI card)
    {
        AccountData clickedAccount = card.GetAccountData();

        if (assigningStudents)
        {
            if (clickedAccount.accountType != AccountType.Student)
                return;

            bool alreadySelected = pendingStudents.Exists(
                student => student.id == clickedAccount.id
            );

            if (alreadySelected)
            {
                pendingStudents.RemoveAll(
                    student => student.id == clickedAccount.id
                );

                card.SetSelected(false);
            }
            else
            {
                pendingStudents.Add(clickedAccount);

                card.SetSelected(true);
            }

            return;
        }

        selectedCard = card;

        RefreshAccountHighlights();

        pendingLoginAccount = card.GetAccountData();

        loginUsernameText.text = "Login: " + pendingLoginAccount.username;

        loginPasswordInput.text = "";

        ShowLoginPanel();
    }

    public async void ConfirmLogin()
    {
        if (pendingLoginAccount == null)
        {
            return;
        }

        string enteredPassword = loginPasswordInput.text.Trim();

        string enteredHash = AccountManager.HashPassword(enteredPassword);

        if (enteredHash != pendingLoginAccount.passwordHash)
        {
            Debug.Log("Incorrect Password");
            return;
        }

        selectedAccount = pendingLoginAccount;

        CurrentAccount.ActiveAccount = selectedAccount;

        ProgressManager.Instance.SetActiveAccount(selectedAccount);

        RefreshAccountHighlights();

        loginPanel.SetActive(false);
        RestorePanelsAfterLogin();

        Debug.Log("Logged in as: " + selectedAccount.username);


        if (selectedAccount.accountType == AccountType.Student ||
            selectedAccount.accountType == AccountType.Teacher)
        {
            await ProgressManager.Instance.LoadProgressForActiveAccount();
        }


        if (waitingToStartGame)
        {
            waitingToStartGame = false;

            MainMenu.StartNewGame();
        }

        else if (waitingToLoadGame)
        {
            waitingToLoadGame = false;

            MainMenu mainMenuScript = FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);

            if (mainMenuScript != null)
            {
                mainMenuScript.LoadGame();
            }
            else
            {
                Debug.LogError("AccountManager: MainMenu script not found!");
            }
        }
    }

    public void CancelLogin()
    {
        pendingLoginAccount = null;

        loginPasswordInput.text = "";

        loginPanel.SetActive(false);
        RestorePanelsAfterLogin();
    }

    private void RestorePanelsAfterLogin()
    {
        PanelHider panelHider = loginPanel.GetComponent<PanelHider>();
        if (panelHider == null)
        {
            Debug.LogError("AccountManager: PanelHider is missing from LoginPanel.", loginPanel);
            return;
        }

        panelHider.RestorePanels();

        if (accountManagementPanel != null && accountManagementPanel.activeSelf)
        {
            HideAccountManagementTitle();
        }
    }

    private void ShowLoginPanel()
    {
        loginPanel.SetActive(true);

        PanelHider panelHider = loginPanel.GetComponent<PanelHider>();
        if (panelHider == null)
        {
            Debug.LogError("AccountManager: PanelHider is missing from LoginPanel.", loginPanel);
            return;
        }

        panelHider.HidePanels();
    }

    private void ShowAccountManagementPanel()
    {
        accountManagementPanel.SetActive(true);
        HideAccountManagementTitle();
    }

    private void HideAccountManagementTitle()
    {
        SetAccountManagementTitleVisible(false);
    }

    private void ShowAccountManagementTitle()
    {
        SetAccountManagementTitleVisible(true);
    }

    private void SetAccountManagementTitleVisible(bool visible)
    {
        Transform canvas = accountManagementPanel.transform.parent;
        if (canvas == null)
        {
            Debug.LogError("AccountManager: AccountManagementPanel has no parent Canvas.", accountManagementPanel);
            return;
        }

        Transform title = canvas.Find("Title");
        if (title == null)
        {
            Debug.LogError("AccountManager: Could not find the Title object under the Canvas.", accountManagementPanel);
            return;
        }

        title.gameObject.SetActive(visible);
    }

    public void OpenAccountRecovery()
    {
        if (pendingLoginAccount == null)
        {
            Debug.LogWarning("AccountManager: No account selected for recovery.");
            return;
        }

        loginPanel.SetActive(false);
        accountRecoveryPanel.SetActive(true);

        Debug.Log("Opening recovery for: " + pendingLoginAccount.username);
    }

    public void CancelAccountRecovery()
    {
        accountRecoveryPanel.SetActive(false);
        ShowLoginPanel();
    }


    private bool resettingPassword = false;

    public async void ConfirmAccountRecovery()
    {
        if (resettingPassword)
            return;

        if (pendingLoginAccount == null)
        {
            Debug.LogWarning("No account selected for recovery.");
            return;
        }

        string enteredCode = recoveryCodeInputField.text.Trim().ToUpperInvariant();
        string newPassword = newPasswordInputField.text.Trim();
        string confirmPassword = confirmPasswordInputField.text.Trim();

        // Reject missing fields.
        if (string.IsNullOrWhiteSpace(enteredCode) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            Debug.LogWarning("Account Recovery: All fields are required.");
            return;
        }

        // Check password confirmation.
        if (newPassword != confirmPassword)
        {
            Debug.LogWarning("Account Recovery: Passwords do not match.");
            return;
        }

        // Check whether this account supports recovery.
        if (!pendingLoginAccount.recoveryCodeEnabled ||
            string.IsNullOrEmpty(pendingLoginAccount.recoveryCodeHash))
        {
            Debug.LogWarning("Account Recovery: No recovery code configured.");
            return;
        }

        // Verify the recovery code.
        string enteredCodeHash = HashRecoveryCode(enteredCode);

        if (enteredCodeHash != pendingLoginAccount.recoveryCodeHash)
        {
            Debug.LogWarning("Account Recovery: Incorrect recovery code.");
            return;
        }

        resettingPassword = true;
        confirmAccountRecoveryButton.interactable = false;

        string previousPasswordHash = pendingLoginAccount.passwordHash;

        try
        {
            // Update the selected account's password hash.
            pendingLoginAccount.passwordHash = HashPassword(newPassword);

            // Save the updated account to Firestore.
            await FirestoreAccountManager.UploadAccount(pendingLoginAccount);

            // Update local account storage.
            SaveAccount();

            Debug.Log("Account Recovery: Password successfully reset.");

            // Clear sensitive inputs.
            recoveryCodeInputField.text = "";
            newPasswordInputField.text = "";
            confirmPasswordInputField.text = "";
            loginPasswordInput.text = "";

            // Return to the login panel.
            accountRecoveryPanel.SetActive(false);
            ShowLoginPanel();
        }
        catch (System.Exception ex)
        {
            // Restore the previous password if uploading failed.
            pendingLoginAccount.passwordHash = previousPasswordHash;

            Debug.LogError("Account Recovery failed: " + ex.Message);
        }
        finally
        {
            resettingPassword = false;
            confirmAccountRecoveryButton.interactable = true;
        }
    }


    public void StartAssignStudents()
    {
        if (selectedAccount == null)
            return;

        if (selectedAccount.accountType != AccountType.Teacher)
            return;

        assigningStudents = true;
        assigningTeacher = selectedAccount;

        pendingStudents.Clear();

        if (assigningTeacher.assignedStudentIDs == null)
            assigningTeacher.assignedStudentIDs = new List<string>();

        // Start with students already assigned to this teacher.
        foreach (AccountData student in registry.accounts)
        {
            if (student.accountType == AccountType.Student &&
                assigningTeacher.assignedStudentIDs.Contains(student.id))
            {
                pendingStudents.Add(student);
            }
        }

        // Highlight students currently selected for assignment.
        foreach (Transform child in studentContainer)
        {
            AccountCardUI card = child.GetComponent<AccountCardUI>();

            if (card == null)
                continue;

            AccountData student = card.GetAccountData();

            card.SetSelected(
                pendingStudents.Exists(s => s.id == student.id)
            );
        }

        assignStudentsButton.SetActive(false);
        confirmAssignButton.SetActive(true);

        Debug.Log("Assignment Mode Started");
    }

    public void ConfirmAssignStudents()
    {
        if (assigningTeacher == null)
            return;

        if (assigningTeacher.assignedStudentIDs == null)
            assigningTeacher.assignedStudentIDs = new List<string>();

        // Save the final selection, including existing assignments
        // that were not deselected.
        assigningTeacher.assignedStudentIDs =
            pendingStudents.ConvertAll(student => student.id);

        SaveAccount();
        QueueAssignmentUpload(assigningTeacher.id);

        assigningStudents = false;
        assigningTeacher = null;

        pendingStudents.Clear();

        assignStudentsButton.SetActive(true);
        confirmAssignButton.SetActive(false);

        // Clear temporary selection colors.
        RefreshAccountHighlights();

        Debug.Log("Student assignments updated.");
    }

    public void PromptForAccountThenStartGame()
    {
        waitingToStartGame = true;

        OpenAccountMenu();
    }

    public void PromptForAccountThenLoadGame()
    {
        waitingToLoadGame = true;
        waitingToStartGame = false;

        OpenAccountMenu();
    }

    void HideAllPanels()
    {
        if (accountSelectPanel != null)
            accountSelectPanel.SetActive(false);

        if (accountManagementPanel != null)
            accountManagementPanel.SetActive(false);

        if (createAccountPanel != null)
            createAccountPanel.SetActive(false);

        if (teacherInfoPanel != null)
            teacherInfoPanel.SetActive(false);

        if (studentInfoPanel != null)
            studentInfoPanel.SetActive(false);

        if (classroomPanel != null)
            classroomPanel.SetActive(false);
    }

    public void OpenAccountMenu()
    {
        HideAllPanels();

        mainMenu.SetActive(false);

        cardDeck.SetActive(false);

        accountButton.SetActive(false);

        accountSelectPanel.SetActive(true);

        ShowAccountManagementPanel();

        backButton.SetActive(true);

        classroomPanel.SetActive(true);
    }

    public void OpenCreateStudentMenu()
    {
        pendingAccountType = AccountType.Student;

        accountSelectPanel.SetActive(false);

        accountManagementPanel.SetActive(false);

        accountRecoveryDisplay.SetActive(false);

        createAccountPanel.SetActive(true);

        backButton.SetActive(false);
    }

    public void OpenCreateTeacherMenu()
    {
        pendingAccountType = AccountType.Teacher;

        accountSelectPanel.SetActive(false);

        accountManagementPanel.SetActive(false);

        accountRecoveryDisplay.SetActive(false);

        createAccountPanel.SetActive(true);

        backButton.SetActive(false);
    }

    public void OpenTeacherInfo(AccountCardUI card)
    {
        AccountData account = card.GetAccountData();

        if (account.accountType != AccountType.Teacher)
            return;

        HideAllPanels();

        teacherInfoPanel.SetActive(true);

        backButton.SetActive(true);

        teacherNameText.text = "Name: " + account.username;

        string section = "No Classroom Selected";

        if (DatabaseManager.Instance != null &&
            !string.IsNullOrWhiteSpace(DatabaseManager.Instance.CurrentDatabase))
        {
            section = DatabaseManager.Instance.CurrentDatabase;
        }

        teacherSectionText.text = "Section: " + section;

        LoadTeacherStats(account);

        foreach (Transform child in assignedStudentContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (string studentID in account.assignedStudentIDs)
        {
            AccountData student = registry.accounts.Find(a => a.id == studentID);

            if (student == null)
                continue;

            GameObject obj = Instantiate(accountCardPrefab, assignedStudentContainer);

            obj.SetActive(true);

            AccountCardUI ui = obj.GetComponent<AccountCardUI>();

            ui.Setup(student, AccountCardMode.TeacherStudentView);
        }
    }

    private void LoadTeacherStats(AccountData teacher)
    {
        string path = Application.persistentDataPath + "/progress_" + teacher.id + ".json";

        if (!System.IO.File.Exists(path))
        {
            teacherProgressText.text = "No Progress has been made on this Account";

            teacherMetricsText.text = "No Progress has been made on this Account";

            return;
        }

        string json = System.IO.File.ReadAllText(path);
        ProgressData data = JsonUtility.FromJson<ProgressData>(json);

        if (data == null)
        {
            teacherProgressText.text = "No Progress has been made on this Account";

            teacherMetricsText.text = "No Progress has been made on this Account";

            return;
        }

        // Completed Levels
        if (data.completedLevel == null || data.completedLevel.Count == 0)
        {
            teacherProgressText.text = "No Completed Levels";
        }

        else
        {
            teacherProgressText.text = "Completed Levels:\n";

            foreach (string level in data.completedLevel)
            {
                teacherProgressText.text += level + "\n";
            }
        }

        // Question Metrics
        int totalAnswered =
            data.multipleChoiceTotal +
            data.identificationTotal +
            data.trueFalseTotal;

        int totalCorrect =
            data.multipleChoiceCorrect +
            data.identificationCorrect +
            data.trueFalseCorrect;

        if (totalAnswered == 0)
        {
            teacherMetricsText.text = "No Progress has been made on this Account";

            return;
        }

        teacherMetricsText.text =
            "Multiple Choice: " +
            data.multipleChoiceCorrect + "/" +
            data.multipleChoiceTotal + "\n\n" +

            "Identification: " +
            data.identificationCorrect + "/" +
            data.identificationTotal + "\n\n" +

            "True/False: " +
            data.trueFalseCorrect + "/" +
            data.trueFalseTotal + "\n\n" +

            "Total: " + totalCorrect + "/" + totalAnswered;
    }


    public void OpenStudentInfo(AccountData student)
    {
        studentInfoPanel.SetActive(true);

        string path = Application.persistentDataPath + "/progress_" + student.id + ".json";

        if (!System.IO.File.Exists(path))
        {
            return;
        }    

        string json = System.IO.File.ReadAllText(path);

        ProgressData data = JsonUtility.FromJson<ProgressData>(json);

        completedLevelsText.text = "Completed Levels:\n";

        foreach (string level in data.completedLevel)
        {
            completedLevelsText.text += level + "\n";
        }

        quizStatsText.text = "Multiple Choice: " + data.multipleChoiceCorrect + "/" + data.multipleChoiceTotal + "\n\n" + "Identification: " + data.identificationCorrect + "/" + data.identificationTotal + "\n\n" + "True/False: " + data.trueFalseCorrect + "/" + data.trueFalseTotal;
    }
    public void CloseStudentInfo()
    {
        if (studentInfoPanel == null || !studentInfoPanel.activeSelf)
            return;

        studentInfoPanel.SetActive(false);

        Debug.Log("Student Information Panel closed.");
    }

    public void CloseAccountMenu()
    {
        waitingToStartGame = false;
        waitingToLoadGame = false;

        accountButton.SetActive(true);

        mainMenu.SetActive(true);

        accountSelectPanel.SetActive(false);

        accountManagementPanel.SetActive(false);
        ShowAccountManagementTitle();

        createAccountPanel.SetActive(false);

        backButton.SetActive(false);

        teacherInfoPanel.SetActive(false);

        mainMenu.SetActive(true);

        cardDeck.SetActive(true);

        classroomPanel.SetActive(false);
    }

    public void SelectTeacherStudent(AccountCardUI card)
    {
        OpenStudentInfo(card.GetAccountData());
    }

    private void LoadPendingAccountUploads()
    {
        if (!File.Exists(pendingAccountsPath))
        {
            pendingAccountUploads = new PendingAccountUploads();
            return;
        }

        string json = File.ReadAllText(pendingAccountsPath);

        pendingAccountUploads =
            JsonUtility.FromJson<PendingAccountUploads>(json)
            ?? new PendingAccountUploads();

        if (pendingAccountUploads.accountIDs == null)
            pendingAccountUploads.accountIDs = new List<string>();
    }

    private void LoadPendingAssignmentUploads()
    {
        if (!File.Exists(pendingAssignmentsPath))
        {
            pendingAssignmentUploads = new PendingAccountUploads();
            return;
        }

        string json = File.ReadAllText(pendingAssignmentsPath);

        pendingAssignmentUploads =
            JsonUtility.FromJson<PendingAccountUploads>(json)
            ?? new PendingAccountUploads();

        if (pendingAssignmentUploads.accountIDs == null)
            pendingAssignmentUploads.accountIDs = new List<string>();
    }

    private void SavePendingAssignmentUploads()
    {
        string json = JsonUtility.ToJson(
            pendingAssignmentUploads, true
        );

        File.WriteAllText(pendingAssignmentsPath, json);
    }

    public bool IsAssignmentPendingUpload(string accountID)
    {
        return pendingAssignmentUploads.accountIDs.Contains(accountID);
    }

    public void QueueAssignmentUpload(string teacherID)
    {
        if (!pendingAssignmentUploads.accountIDs.Contains(teacherID))
        {
            pendingAssignmentUploads.accountIDs.Add(teacherID);
            SavePendingAssignmentUploads();
        }

        _ = SynchronizePendingAssignments();
    }

    public async Task SynchronizePendingAssignments()
    {
        if (synchronizingAssignments)
            return;

        if (pendingAssignmentUploads.accountIDs.Count == 0)
            return;

        if (FirestoreManager.Instance == null)
            return;

        synchronizingAssignments = true;

        try
        {
            List<string> pendingIDs = new List<string>(
                pendingAssignmentUploads.accountIDs
            );

            foreach (string teacherID in pendingIDs)
            {
                AccountData teacher = registry.accounts.Find(
                    a => a.id == teacherID
                );

                if (teacher == null)
                    continue;

                // An account still awaiting its initial creation upload
                // will be handled by the existing account queue.
                if (IsAccountPendingUpload(teacherID))
                    continue;

                try
                {
                    List<string> assignmentSnapshot =
                        new List<string>(teacher.assignedStudentIDs);

                    await FirestoreAccountManager.UploadAssignments(
                        teacherID,
                        assignmentSnapshot
                    );

                    bool verified =
                        await FirestoreAccountManager.VerifyAssignments(
                            teacherID,
                            assignmentSnapshot
                        );

                    if (!verified)
                        continue;

                    // Don't clear the pending entry if the user changed
                    // assignments while this upload was in progress.
                    if (!new HashSet<string>(teacher.assignedStudentIDs)
                        .SetEquals(assignmentSnapshot))
                    {
                        continue;
                    }

                    pendingAssignmentUploads.accountIDs.Remove(teacherID);
                    SavePendingAssignmentUploads();

                    Debug.Log(
                        "Teacher assignments synchronized: " +
                        teacher.username
                    );
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning(
                        "Assignment sync pending for " +
                        teacher.username + ": " + ex.Message
                    );
                }
            }
        }
        finally
        {
            synchronizingAssignments = false;
        }
    }

    private void SavePendingAccountUploads()
    {
        string json = JsonUtility.ToJson(pendingAccountUploads, true);
        File.WriteAllText(pendingAccountsPath, json);
    }

    public bool IsAccountPendingUpload(string accountID)
    {
        return pendingAccountUploads.accountIDs.Contains(accountID);
    }

    public async Task SynchronizePendingAccounts()
    {
        if (synchronizingPendingAccounts)
            return;

        if (pendingAccountUploads.accountIDs.Count == 0)
            return;

        if (FirestoreManager.Instance == null)
        {
            Debug.LogWarning(
                "Account synchronization skipped: Firestore is not initialized."
            );
            return;
        }

        synchronizingPendingAccounts = true;

        try
        {
            // Snapshot the queue so changes do not affect iteration.
            List<string> pendingIDs =
                new List<string>(pendingAccountUploads.accountIDs);

            foreach (string accountID in pendingIDs)
            {
                AccountData account = registry.accounts.Find(
                    a => a.id == accountID
                );

                if (account == null)
                {
                    Debug.LogWarning(
                        "Pending account not found locally: " + accountID
                    );

                    // Keep the pending ID for investigation.
                    continue;
                }

                try
                {
                    await FirestoreAccountManager.UploadAccount(account);

                    // Verify that the account exists on the server.
                    bool uploaded = await FirestoreAccountManager.IsAccountUploaded(accountID);

                    if (!uploaded)
                    {
                        Debug.LogWarning(
                            "Account upload not yet confirmed by Firestore: " +
                            account.username
                        );

                        continue;
                    }

                    // Remove only after Firestore confirms the upload.
                    pendingAccountUploads.accountIDs.Remove(accountID);
                    SavePendingAccountUploads();

                    Debug.Log(
                        "Pending account synchronized: " + account.username
                    );
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning(
                        "Account synchronization failed for " +
                        account.username + ": " + ex.Message
                    );

                    // Preserve the pending entry for the next attempt.
                }
            }
        }
        finally
        {
            synchronizingPendingAccounts = false;
        }
    }

    public void ConnectUI(AccountUIReferences ui)
    {
        accountButton = ui.accountButton;
        backButton = ui.backButton;

        accountSelectPanel = ui.accountSelectPanel;
        accountManagementPanel = ui.accountManagementPanel;
        createAccountPanel = ui.createAccountPanel;

        teacherInfoPanel = ui.teacherInfoPanel;
        studentInfoPanel = ui.studentInfoPanel;

        classroomPanel = ui.classroomPanel;

        classroomSelectionPanel = ui.classroomSelectionPanel;
        classroomCreationPanel = ui.classroomCreationPanel;
        classroomNameInput = ui.classroomNameInput;

        mainMenu = ui.mainMenu;

        classroomPanel = ui.classroomPanel;

        assignStudentsButton = ui.assignStudentsButton;
        confirmAssignButton = ui.confirmAssignButton;

        studentContainer = ui.studentContainer;
        teacherContainer = ui.teacherContainer;
        assignedStudentContainer = ui.assignedStudentContainer;

        usernameInputField = ui.usernameInputField;
        passwordInputField = ui.passwordInputField;

        teacherNameText = ui.teacherNameText;
        teacherSectionText = ui.teacherSectionText;
        teacherProgressText = ui.teacherProgressText;
        teacherMetricsText = ui.teacherMetricsText;

        completedLevelsText = ui.completedLevelsText;
        quizStatsText = ui.quizStatsText;

        loginPanel = ui.loginPanel;
        loginPasswordInput = ui.loginPasswordInput;
        loginUsernameText = ui.loginUsernameText;

        // Recovery Code Display
        accountRecoveryDisplay = ui.accountRecoveryDisplay;
        recoveryCodeText = ui.recoveryCodeText;
        confirmRecoveryCodeButton = ui.confirmRecoveryCodeButton;

        // Account Recovery Panel
        accountRecoveryPanel = ui.accountRecoveryPanel;
        accountRecoveryButton = ui.accountRecoveryButton;
        cancelAccountRecoveryButton = ui.cancelAccountRecoveryButton;
        recoveryCodeInputField = ui.recoveryCodeInputField;
        newPasswordInputField = ui.newPasswordInputField;
        confirmPasswordInputField = ui.confirmPasswordInputField;
        confirmAccountRecoveryButton = ui.confirmAccountRecoveryButton;

        cardDeck = ui.cardDeck;

        confirmAccountRecoveryButton.onClick.RemoveAllListeners();
        confirmAccountRecoveryButton.onClick.AddListener(ConfirmAccountRecovery);


        // Accounts Button
        Button accountBtn = accountButton.GetComponent<Button>();

        accountBtn.onClick.RemoveAllListeners();

        accountBtn.onClick.AddListener(OpenAccountMenu);


        // Back Button
        Button backBtn = backButton.GetComponent<Button>();

        backBtn.onClick.RemoveAllListeners();

        backBtn.onClick.AddListener(CloseAccountMenu);

        // Open Account Recovery
        accountRecoveryButton.onClick.RemoveAllListeners();
        accountRecoveryButton.onClick.AddListener(OpenAccountRecovery);

        // Cancel Account Recovery
        cancelAccountRecoveryButton.onClick.RemoveAllListeners();
        cancelAccountRecoveryButton.onClick.AddListener(CancelAccountRecovery);

        // Create Student Button
        Button createStudentBtn = ui.createStudentButton.GetComponent<Button>();

        createStudentBtn.onClick.RemoveAllListeners();

        createStudentBtn.onClick.AddListener(OpenCreateStudentMenu);


        // Create Teacher Button
        Button createTeacherBtn =
            ui.createTeacherButton.GetComponent<Button>();

        createTeacherBtn.onClick.RemoveAllListeners();

        createTeacherBtn.onClick.AddListener(OpenCreateTeacherMenu);


        // Submit Username Button
        Button submitBtn =
            ui.submitUsernameButton.GetComponent<Button>();

        submitBtn.onClick.RemoveAllListeners();

        submitBtn.onClick.AddListener(SubmitUsername);

        // Confirm Recovery Code Button
        confirmRecoveryCodeButton.onClick.RemoveAllListeners();

        confirmRecoveryCodeButton.onClick.AddListener(ConfirmRecoveryCode);


        // Delete Account Button
        Button deleteBtn =
            ui.deleteAccountButton.GetComponent<Button>();

        deleteBtn.onClick.RemoveAllListeners();

        deleteBtn.onClick.AddListener(DeleteSelected);


        // Assign Students Button
        Button assignBtn =
            assignStudentsButton.GetComponent<Button>();

        assignBtn.onClick.RemoveAllListeners();

        assignBtn.onClick.AddListener(StartAssignStudents);


        // Confirm Assign Button
        Button confirmBtn =
            confirmAssignButton.GetComponent<Button>();

        confirmBtn.onClick.RemoveAllListeners();

        confirmBtn.onClick.AddListener(ConfirmAssignStudents);

        //Confirm Login Button
        Button loginBtn = 
            ui.confirmLoginButton.GetComponent<Button>();

        loginBtn.onClick.RemoveAllListeners();

        loginBtn.onClick.AddListener(ConfirmLogin);

        //Cancel Login Button
        Button cancelBtn =
            ui.cancelLoginButton.GetComponent<Button>();

        cancelBtn.onClick.RemoveAllListeners();

        cancelBtn.onClick.AddListener(CancelLogin);

        LoadAccount();

        HideAllPanels();

        accountButton.SetActive(true);

        backButton.SetActive(false);

        RefreshUI();

        Debug.Log("UI Connected Successfully");
    }

    private static string HashPassword(string password)
    {
        SHA256 sha = SHA256.Create();

        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));

        StringBuilder builder = new StringBuilder();

        foreach (byte b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString();

    }

    private static string GenerateRecoveryCode()
    {
        const string characters = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        byte[] randomBytes = new byte[24];

        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        StringBuilder code = new StringBuilder();

        for (int i = 0; i < randomBytes.Length; i++)
        {
            if (i > 0 && i % 4 == 0)
            {
                code.Append("-");
            }

            code.Append(characters[randomBytes[i] % characters.Length]);
        }

        return code.ToString();
    }

    private static string HashRecoveryCode(string code)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(
                Encoding.UTF8.GetBytes(code)
            );

            StringBuilder builder = new StringBuilder();

            foreach (byte b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }

            return builder.ToString();
        }
    }

    private void EnsureAccountsFileExists()
    {
        if (!File.Exists(registryPath))
        {
            registry = new AccountRegistry();

            SaveAccount();

            Debug.Log("accounts.json created");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (studentInfoPanel == null || !studentInfoPanel.activeInHierarchy)
            return;

        if (Mouse.current == null ||
            !Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (EventSystem.current == null)
            return;

        // Check which UI elements were clicked.
        PointerEventData pointerData =
            new PointerEventData(EventSystem.current);

        pointerData.position = Mouse.current.position.ReadValue();

        List<RaycastResult> results = new List<RaycastResult>();

        EventSystem.current.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            // Ignore clicks on scrollable UI controls.
            if (result.gameObject.GetComponentInParent<ScrollRect>() != null &&
                result.gameObject.transform.IsChildOf(studentInfoPanel.transform))
            {
                return;
            }
        }

        CloseStudentInfo();
    }
}

[System.Serializable]
public class PendingAccountUploads
{
    public List<string> accountIDs = new List<string>();
}
