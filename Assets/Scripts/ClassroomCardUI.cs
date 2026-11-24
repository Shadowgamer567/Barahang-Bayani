using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClassroomCardUI : MonoBehaviour
{
    public TMP_Text classroomNameText;

    [Header("Selection Indicator")]
    [Tooltip("Optional border image. If not assigned, the Button's Target Graphic is used.")]
    public Image borderImage;
    public Color selectedColor = Color.yellow;

    private string classroomID;
    private Graphic selectionGraphic;
    private Color normalColor;

    public void Setup(string classroomID)
    {
        Setup(classroomID, classroomID);
    }

    public void Setup(string classroomID, string classroomName)
    {
        this.classroomID = classroomID;

        if (classroomNameText != null)
        {
            classroomNameText.text = string.IsNullOrWhiteSpace(classroomName)
                ? classroomID
                : classroomName;
        }

        Button button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(SelectClassroom);
        }
        else
        {
            Debug.LogError("Classroom card requires a Button component.", this);
        }

        // Match the selection treatment used by the previous account cards.
        // An explicitly assigned border is preferred so the whole card does
        // not need to change color.
        selectionGraphic = borderImage != null
            ? borderImage
            : (button != null ? button.targetGraphic : null);

        if (selectionGraphic != null)
            normalColor = selectionGraphic.color;
    }

    public string GetClassroomID()
    {
        return classroomID;
    }

    public void SetSelected(bool selected)
    {
        if (selectionGraphic == null)
            return;

        selectionGraphic.color = selected ? selectedColor : normalColor;
    }

    private void SelectClassroom()
    {
        if (FirestoreClassroomManager.Instance != null)
            FirestoreClassroomManager.Instance.SelectClassroom(classroomID);
    }
}
