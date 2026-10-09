using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Firebase.Firestore;
using TMPro;
using System.Linq.Expressions;

public class FirestoreClassroomManager : MonoBehaviour
{
    public static FirestoreClassroomManager Instance;

    [Header("Classroom UI")]
    public GameObject classroomSelectionPanel;
    public Transform classroomContainer;
    public GameObject classroomCardPrefab;
    public TMP_Text currentClassroomText;

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

    public async Task<bool> CreateClassroom(string classroomID)
    {
        if (string.IsNullOrWhiteSpace(classroomID))
        {
            Debug.LogError("Classroom ID cannot be empty.");
            return false;
        }

        classroomID = classroomID.Trim();

        DocumentReference classroomRef = FirestoreManager.Instance.DB.Collection("Classroom").Document(classroomID);

        try
        {
            DocumentSnapshot snapshot = await classroomRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                Debug.LogWarning("Classroom Already Exists:" + classroomID);
            }

            return false;



            Dictionary<string, object> data = new Dictionary<string, object>();

            data["created"] = true;

            await FirestoreManager.Instance.DB
                .Collection("Classroom")
                .Document(classroomID)
                .SetAsync(data);

            Debug.Log("Classroom created: " + classroomID);

            return true;
        }

        catch (System.Exception e)
        {
            Debug.LogError("Failed to create classroom: " + e.Message);
            return false;
        }
    }

    public async void LoadClassrooms()
    {
        if (classroomContainer == null)
        {
            Debug.LogError("Classroom container is not assigned.");
            return;
        }

        if (classroomCardPrefab == null)
        {
            Debug.LogError("Classroom card prefab is not assigned.");
            return;
        }

        QuerySnapshot snapshot = await FirestoreManager.Instance.DB
            .Collection("Classroom")
            .GetSnapshotAsync();

        // Clear existing classroom cards
        for (int i = classroomContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(classroomContainer.GetChild(i).gameObject);
        }

        foreach (DocumentSnapshot document in snapshot.Documents)
        {
            GameObject obj =
                Instantiate(classroomCardPrefab, classroomContainer);

            obj.SetActive(true);

            ClassroomCardUI card =
                obj.GetComponent<ClassroomCardUI>();

            if (card == null)
            {
                Debug.LogError(
                    "ClassroomCard prefab is missing ClassroomCardUI."
                );

                continue;
            }

            card.Setup(document.Id);
        }

        Debug.Log(
            "Loaded " + snapshot.Count + " classrooms from Firestore."
        );
    }

    public void OpenClassroomSelection()
    {
        if (classroomSelectionPanel == null)
            return;

        classroomSelectionPanel.SetActive(true);

        LoadClassrooms();
    }

    public void CloseClassroomSelection()
    {
        if (classroomSelectionPanel == null)
            return;

        classroomSelectionPanel.SetActive(false);
    }

    public async void SelectClassroom(string classroomID)
    {
        if (string.IsNullOrWhiteSpace(classroomID))
        {
            return;
        }

        Debug.Log("Selecting Classroom: " + classroomID);

        DatabaseManager.Instance.SetDatabase(classroomID);

        if (currentClassroomText != null)
        {
            currentClassroomText.text = classroomID;
        }

        CloseClassroomSelection();

        await FirestoreAccountManager.Instance.DownloadAccounts();

        // Refresh account UI
        if (AccountManager.Instance != null)
        {
            AccountManager.Instance.RefreshUI();
        }

        Debug.Log("Classroom switched successfully: " + classroomID);
    }

    public async void ConfirmClassroomCreation()
    {
        if(classroomNameInput == null)
        {
            Debug.Log("Classroom name input is not assigned");
            return;
        }

        string classroomID = classroomNameInput.text.Trim();

        if (string.IsNullOrWhiteSpace(classroomID))
        {
            Debug.LogWarning("Please Enter a classroom name");
            return;
        }

        await CreateClassroom(classroomID);

        classroomNameInput.text = "";

        if (classroomCreationPanel != null) 
        {
            classroomSelectionPanel.SetActive(true);
        }

        LoadClassrooms();
    }

    public void OpenClassroomCreation()
    {
        if (classroomSelectionPanel != null)
            classroomSelectionPanel.SetActive(false);

        if (classroomCreationPanel != null)
            classroomCreationPanel.SetActive(true);
    }

    public void CancelClassroomCreation()
    {
        if (classroomCreationPanel != null)
            classroomCreationPanel.SetActive(false);

        if (classroomSelectionPanel != null)
            classroomSelectionPanel.SetActive(true);

        if (classroomNameInput != null)
            classroomNameInput.text = "";
    }

    public void ConnectClassroomButtons(AccountUIReferences ui)
    {
        Debug.Log("=== Connecting Classroom Buttons ===");

        ConnectButton(ui.openClassroomSelectionButton, OpenClassroomSelection, "Open Classroom Selection");

        ConnectButton(ui.openClassroomCreationButton, OpenClassroomCreation, "Open Classroom Creation");

        ConnectButton(ui.cancelCreationButton, CancelClassroomCreation, "Cancel Classroom Creation");

        ConnectButton(ui.cancelClassroomSelectionButton, CloseClassroomSelection, "Cancel Classroom Selection");

        ConnectButton(ui.confirmCreationButton, ConfirmClassroomCreation, "Confirm Create Classroom");
    }

    private void ConnectButton(
        GameObject buttonObject,
        UnityEngine.Events.UnityAction action,
        string buttonName)
    {
        if (buttonObject == null)
        {
            Debug.LogError(buttonName + ": UI reference is NULL!");
            return;
        }

        Button button = buttonObject.GetComponent<Button>();

        if (button == null)
        {
            Debug.LogError(
                buttonName + ": No Button component on " +
                buttonObject.name
            );
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);

        Debug.Log(
            buttonName + ": Connected to " +
            buttonObject.name +
            " | Active: " + buttonObject.activeInHierarchy +
            " | Interactable: " + button.interactable
        );
    }

    public void ConnectUI(AccountUIReferences ui)
    {
        // Classroom selection
        classroomSelectionPanel = ui.classroomSelectionPanel;
        classroomContainer = ui.classroomContainer;
        classroomCardPrefab = ui.classroomCardPrefab;
        currentClassroomText = ui.currentClassroomNameText;

        // Classroom creation
        classroomCreationPanel = ui.classroomCreationPanel;
        classroomNameInput = ui.classroomNameInput;

        // Restore the displayed classroom name
        if (currentClassroomText != null &&
            DatabaseManager.Instance != null)
        {
            currentClassroomText.text =
                DatabaseManager.Instance.CurrentDatabase;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        if (currentClassroomText != null &&
            DatabaseManager.Instance != null)
        {
            currentClassroomText.text =
                DatabaseManager.Instance.CurrentDatabase;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
