using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Loads and selects only classrooms that the authenticated website user is
/// authorized to access. Classroom creation is delegated to the website's
/// createClassroom callable function.
/// </summary>
public class FirestoreClassroomManager : MonoBehaviour
{
    public static FirestoreClassroomManager Instance;

    private bool selectingClassroom;
    private bool loadingClassrooms;
    private readonly List<WebsiteClassroomData> availableClassrooms = new List<WebsiteClassroomData>();

    public bool IsSelectingClassroom => selectingClassroom;
    public bool IsLoadingClassrooms => loadingClassrooms;

    [Header("Classroom UI")]
    public GameObject classroomSelectionPanel;
    public Transform classroomContainer;
    public GameObject classroomCardPrefab;
    public TMP_Text currentClassroomText;
    public GameObject openClassroomCreationButton;

    [Header("Classroom Creation UI")]
    public GameObject classroomCreationPanel;
    public TMP_InputField classroomNameInput;
    public Button confirmCreationButton;
    public Button cancelCreationButton;

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

    public async void LoadClassrooms()
    {
        if (loadingClassrooms)
            return;

        if (!ValidateUIReferences())
            return;

        GameAuthManager auth = GameAuthManager.Instance;
        if (auth == null || !auth.IsSignedIn)
        {
            Debug.LogWarning("Classroom loading requires a signed-in website account.");
            auth?.BeginWebsiteLogin();
            return;
        }

        loadingClassrooms = true;
        ClearClassroomCards();
        availableClassrooms.Clear();

        try
        {
            string functionName;
            if (auth.IsStudent)
                functionName = "getMyClassrooms";
            else if (auth.IsTeacher)
                functionName = "getTeacherClassrooms";
            else
                throw new InvalidOperationException("This website account role cannot access the game.");

            WebsiteClassroomListResult result =
                await WebsiteCallableClient.CallAsync<WebsiteClassroomListResult>(functionName);

            if (result == null || !result.success)
                throw new Exception("The website did not return a successful classroom response.");

            if (result.classrooms != null)
                availableClassrooms.AddRange(result.classrooms);

            foreach (WebsiteClassroomData classroom in availableClassrooms)
            {
                if (classroom == null || string.IsNullOrWhiteSpace(classroom.id))
                    continue;

                GameObject cardObject = Instantiate(classroomCardPrefab, classroomContainer);
                cardObject.SetActive(true);

                ClassroomCardUI card = cardObject.GetComponent<ClassroomCardUI>();
                if (card == null)
                {
                    Debug.LogError("Classroom card prefab is missing ClassroomCardUI.");
                    Destroy(cardObject);
                    continue;
                }

                string displayName = string.IsNullOrWhiteSpace(classroom.name)
                    ? classroom.id
                    : classroom.name;
                card.Setup(classroom.id, displayName);
            }

            // Automatically select a classroom if none is selected
            // or if the previous selection is no longer available.
            string currentSelectedID = auth.SelectedClassroomID;

            WebsiteClassroomData classroomToSelect = null;

            if (!string.IsNullOrWhiteSpace(currentSelectedID))
            {
                classroomToSelect = availableClassrooms.Find(
                    item => item != null && item.id == currentSelectedID
                );
            }

            // If there is no valid previous selection, use the first available classroom.
            if (classroomToSelect == null)
            {
                classroomToSelect = availableClassrooms.Find(
                    item => item != null && !string.IsNullOrWhiteSpace(item.id)
                );
            }

            if (classroomToSelect != null)
            {
                SelectClassroom(classroomToSelect.id);
            }
            else
            {
                // No classrooms are available. Keep the selected classroom empty.
                auth.ClearSelectedClassroom();
                if (DatabaseManager.Instance != null)
                    DatabaseManager.Instance.SetDatabase("DefaultDatabase");

                if (currentClassroomText != null)
                    currentClassroomText.text = "No Classroom Selected";
            }

            if (openClassroomCreationButton != null)
                openClassroomCreationButton.SetActive(auth.IsTeacher);

            Debug.Log("Loaded " + availableClassrooms.Count +
                " authorized website classroom(s) for " + auth.Role + ".");
        }
        catch (Exception exception)
        {
            Debug.LogError("Failed to load website classrooms: " + exception);
        }
        finally
        {
            loadingClassrooms = false;
        }
    }

    public void OpenClassroomSelection()
    {
        if (ProgressManager.Instance != null &&
            ProgressManager.Instance.IsAccountDeletionInProgress)
            return;

        if (classroomSelectionPanel == null)
        {
            Debug.LogError("Classroom selection panel is not assigned.");
            return;
        }

        // Keep the Account Menu visible.
        classroomSelectionPanel.SetActive(true);

        LoadClassrooms();
    }

    public void CloseClassroomSelection()
    {
        // Classroom selection is now part of the Account Menu.
        // Do not close the entire classroom area when a classroom is selected.
    }

    public async void SelectClassroom(string classroomID)
    {
        if (selectingClassroom || string.IsNullOrWhiteSpace(classroomID))
            return;

        if (ProgressManager.Instance != null && ProgressManager.Instance.IsAccountDeletionInProgress)
            return;

        // A classroom card is not trusted by itself. The ID must be present in
        // the list returned for the currently authenticated website account.
        WebsiteClassroomData selected = availableClassrooms.Find(
            item => item != null && string.Equals(item.id, classroomID, StringComparison.Ordinal));

        if (selected == null)
        {
            Debug.LogError("Rejected classroom selection: it was not returned for this website user.");
            return;
        }

        GameAuthManager auth = GameAuthManager.Instance;
        if (auth == null || !auth.IsSignedIn)
        {
            Debug.LogError("Cannot select a classroom without an authenticated website session.");
            return;
        }

        selectingClassroom = true;
        try
        {
            DatabaseManager.Instance.SetDatabase(selected.id);
            auth.SetSelectedClassroom(selected.id);

            if (currentClassroomText != null)
                currentClassroomText.text = string.IsNullOrWhiteSpace(selected.name)
                    ? selected.id
                    : selected.name;

            // The account database is intentionally not downloaded here.
            // Progress loading is kept out of this stage until its production
            // security path has been implemented and tested.
            Debug.Log("Selected authorized website classroom: " + selected.id +
                " | User: " + auth.UserUID + " | Role: " + auth.Role);

            AccountManager.Instance?.ContinueAfterWebsiteClassroomSelected();
        }
        catch (Exception exception)
        {
            Debug.LogError("Failed to select website classroom: " + exception);
        }
        finally
        {
            selectingClassroom = false;
        }
    }

    public async void ConfirmClassroomCreation()
    {
        GameAuthManager auth = GameAuthManager.Instance;
        if (auth == null || !auth.IsSignedIn || !auth.IsTeacher)
        {
            Debug.LogWarning("Only an authenticated website teacher can create a classroom.");
            return;
        }

        if (classroomNameInput == null)
        {
            Debug.LogError("Classroom name input is not assigned.");
            return;
        }

        string classroomName = classroomNameInput.text.Trim();
        if (string.IsNullOrWhiteSpace(classroomName))
        {
            Debug.LogWarning("Please enter a classroom name.");
            return;
        }

        try
        {
            WebsiteCreateClassroomRequest payload = new WebsiteCreateClassroomRequest
            {
                name = classroomName
            };

            WebsiteCreateClassroomResult result =
                await WebsiteCallableClient.CallAsync<WebsiteCreateClassroomResult>(
                    "createClassroom", JsonUtility.ToJson(payload));

            if (result == null || !result.success || result.classroom == null ||
                string.IsNullOrWhiteSpace(result.classroom.id))
            {
                throw new Exception("The website did not return the created classroom details.");
            }

            classroomNameInput.text = "";
            if (classroomCreationPanel != null)
                classroomCreationPanel.SetActive(false);

            // Refresh the server-authorized list before selecting the new ID.
            await LoadClassroomsAsync();
            SelectClassroom(result.classroom.id);

            Debug.Log("Website classroom created: " + result.classroom.name +
                " | Access code: " + result.classroom.accessCode);
        }
        catch (Exception exception)
        {
            Debug.LogError("Website classroom creation failed: " + exception);
        }
    }

    private async Task LoadClassroomsAsync()
    {
        GameAuthManager auth = GameAuthManager.Instance;
        string functionName = auth.IsTeacher ? "getTeacherClassrooms" : "getMyClassrooms";
        WebsiteClassroomListResult result =
            await WebsiteCallableClient.CallAsync<WebsiteClassroomListResult>(functionName);

        if (result == null || !result.success)
            throw new Exception("The website did not return a successful classroom response.");

        availableClassrooms.Clear();
        if (result != null && result.classrooms != null)
            availableClassrooms.AddRange(result.classrooms);

        if (openClassroomCreationButton != null)
            openClassroomCreationButton.SetActive(auth.IsTeacher);

        ClearClassroomCards();
        foreach (WebsiteClassroomData classroom in availableClassrooms)
        {
            if (classroom == null || string.IsNullOrWhiteSpace(classroom.id))
                continue;

            GameObject cardObject = Instantiate(classroomCardPrefab, classroomContainer);
            cardObject.SetActive(true);
            ClassroomCardUI card = cardObject.GetComponent<ClassroomCardUI>();
            if (card != null)
                card.Setup(classroom.id, string.IsNullOrWhiteSpace(classroom.name) ? classroom.id : classroom.name);
            else
                Destroy(cardObject);
        }
    }

    public void OpenClassroomCreation()
    {
        GameAuthManager auth = GameAuthManager.Instance;

        if (auth == null || !auth.IsSignedIn || !auth.IsTeacher)
        {
            Debug.LogWarning("Classroom creation is available only to website teachers.");
            return;
        }

        if (classroomCreationPanel != null)
            classroomCreationPanel.SetActive(true);
    }

    public void CancelClassroomCreation()
    {
        if (classroomCreationPanel != null)
            classroomCreationPanel.SetActive(false);

        if (classroomNameInput != null)
            classroomNameInput.text = "";
    }

    public void ConnectClassroomButtons(AccountUIReferences ui)
    {
        ConnectButton(ui.openClassroomSelectionButton, OpenClassroomSelection, "Open Classroom Selection");
        ConnectButton(ui.openClassroomCreationButton, OpenClassroomCreation, "Open Classroom Creation");
        ConnectButton(ui.cancelCreationButton, CancelClassroomCreation, "Cancel Classroom Creation");
        ConnectButton(ui.cancelClassroomSelectionButton, CloseClassroomSelection, "Cancel Classroom Selection");
        ConnectButton(ui.confirmCreationButton, ConfirmClassroomCreation, "Confirm Classroom Creation");
    }

    private void ConnectButton(GameObject buttonObject, UnityEngine.Events.UnityAction action, string buttonName)
    {
        if (buttonObject == null)
        {
            Debug.LogWarning(buttonName + ": UI reference is not assigned.");
            return;
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError(buttonName + ": No Button component on " + buttonObject.name);
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    public void ConnectUI(AccountUIReferences ui)
    {
        classroomSelectionPanel = ui.classroomSelectionPanel;
        classroomContainer = ui.classroomContainer;
        classroomCardPrefab = ui.classroomCardPrefab;
        currentClassroomText = ui.currentClassroomNameText;
        openClassroomCreationButton = ui.openClassroomCreationButton;

        classroomCreationPanel = ui.classroomCreationPanel;
        classroomNameInput = ui.classroomNameInput;

        if (currentClassroomText != null && GameAuthManager.Instance != null &&
            !string.IsNullOrWhiteSpace(GameAuthManager.Instance.SelectedClassroomID))
        {
            WebsiteClassroomData selected = availableClassrooms.Find(
                item => item != null && item.id == GameAuthManager.Instance.SelectedClassroomID);
            currentClassroomText.text = selected != null ? selected.name : GameAuthManager.Instance.SelectedClassroomID;
        }
    }

    private bool ValidateUIReferences()
    {
        if (classroomContainer == null || classroomCardPrefab == null)
        {
            Debug.LogError("Classroom container or classroom card prefab is not assigned.");
            return false;
        }
        return true;
    }

    private void ClearClassroomCards()
    {
        if (classroomContainer == null)
            return;

        for (int i = classroomContainer.childCount - 1; i >= 0; i--)
            Destroy(classroomContainer.GetChild(i).gameObject);
    }
}
