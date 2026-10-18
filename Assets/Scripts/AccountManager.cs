using JetBrains.Annotations;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Threading.Tasks;

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
    private bool waitingToStartGame = false;
    private bool waitingToLoadGame = false;

    private AccountData assigningTeacher;
    private List<AccountData> pendingStudents = new();

    [Header("Teacher Info")]
    public TextMeshProUGUI teacherNameText;

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

    private string registryPath => Application.persistentDataPath + "/accounts.json";

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

    public async Task<bool> CreateAccount(AccountType type)
    {
        string username = usernameInputField.text.Trim();

        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogWarning("No Username Entered");
            return false;
        }

        AccountData newAccount = new AccountData();

        newAccount.id = System.Guid.NewGuid().ToString();

        newAccount.username = username;

        string password = passwordInputField.text.Trim();

        if (string.IsNullOrWhiteSpace(password))
        {
            Debug.LogWarning("No Password Entered");
            return false;
        }

        newAccount.passwordHash = HashPassword(password);

        newAccount.accountType = type;

        // Generate recovery code.
        string recoveryCode = GenerateRecoveryCode();

        //Temp store the readeable code for display 
        pendingRecoveryCode = recoveryCode;

        //store hashed code in account 
        newAccount.recoveryCodeHash = HashRecoveryCode(recoveryCode);
        newAccount.recoveryCodeEnabled = true;

        try
        {
            // Wait for Firestore to save the account successfully.
            await FirestoreAccountManager.UploadAccount(newAccount);

            // Only register the account locally after a successful upload.
            registry.accounts.Add(newAccount);

            SaveAccount();

            Debug.Log("Created Account: " + newAccount.username);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Account creation failed during saving: " + ex.Message);

            pendingRecoveryCode = null;

            return false;
        }

        usernameInputField.text = "";
        passwordInputField.text = "";

        RefreshUI();
        return true;
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

        Debug.Log("RefreshUI Finished");
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
            creatingAccount = false;
        }
    }

    public void ConfirmRecoveryCode()
    {
        pendingRecoveryCode = null;

        recoveryCodeText.text = "";

        accountRecoveryDisplay.SetActive(false);

        createAccountPanel.SetActive(false);

        accountSelectPanel.SetActive(true);

        accountManagementPanel.SetActive(true);

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
            {
                return;
            }

            if (pendingStudents.Contains(clickedAccount))
            {
                pendingStudents.Remove(clickedAccount);

                card.SetSelected(false);
            }

            else
            {
                pendingStudents.Add(clickedAccount);

                card.SetSelected(true);
            }

                return;
        }

        if (selectedCard != null)
        {
            selectedCard.SetSelected(false);
        }

        selectedCard = card;

        selectedCard.SetSelected(true);

        pendingLoginAccount = card.GetAccountData();

        loginUsernameText.text = "Login: " + pendingLoginAccount.username;

        loginPasswordInput.text = "";

        loginPanel.SetActive(true);
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

        loginPanel.SetActive(false);

        Debug.Log("Logged in as: " + selectedAccount.username);
        
        if (selectedAccount.accountType == AccountType.Student)
        {
            await PullStudentProgress.PullProgress(selectedAccount);
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
        loginPanel.SetActive(true);
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
            loginPanel.SetActive(true);
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

        assignStudentsButton.SetActive(false);

        confirmAssignButton.SetActive(true);

        Debug.Log("Assignment Mode Started");
    }

    public void ConfirmAssignStudents()
    {
        if (assigningTeacher == null)
            return;

        assigningTeacher.assignedStudentIDs.Clear();

        foreach (AccountData student in pendingStudents)
        {
            assigningTeacher.assignedStudentIDs.Add(student.id);
        }

        SaveAccount();

        assigningStudents = false;

        assigningTeacher = null;

        pendingStudents.Clear();

        assignStudentsButton.SetActive(true);

        confirmAssignButton.SetActive(false);

        Debug.Log("Students Assigned");
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

        accountManagementPanel.SetActive(true);

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

        teacherNameText.text = account.username;
        
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
    public void CloseAccountMenu()
    {
        waitingToStartGame = false;
        waitingToLoadGame = false;

        accountButton.SetActive(true);

        mainMenu.SetActive(true);

        accountSelectPanel.SetActive(false);

        accountManagementPanel.SetActive(false);

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

    }
}
