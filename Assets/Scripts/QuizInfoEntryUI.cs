using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuizInfoEntryUI : MonoBehaviour
{
    [Header("Question Information")]
    public TMP_Text idText;
    public TMP_Text questionText;

    [Header("Editable Quiz Info")]
    public TMP_InputField infoTitleInput;
    public TMP_InputField entryDescriptionInput;

    [Header("Image")]
    public Button imageButton;
    public Image imagePreview;
    public TMP_Text imageButtonText;

    private string questionID;
    private string imagePath = "";
    private QuizInfoMakerManager manager;

    public string QuestionID => questionID;
    public string ImagePath => imagePath;

    // Called when this entry is created.
    public void Setup(string id, string question, QuizInfoMakerManager quizInfoManager)
    {
        manager = quizInfoManager;

        if (imageButton != null)
        {
            imageButton.onClick.RemoveAllListeners();
            imageButton.onClick.AddListener(() => manager.SelectEntryImage(this));
        }

        questionID = id;

        if (idText != null)
            idText.text = "ID: " + id;

        if (questionText != null)
            questionText.text = question;

        if (infoTitleInput != null)
            infoTitleInput.text = "";

        if (entryDescriptionInput != null)
            entryDescriptionInput.text = "";

        imagePath = "";
        UpdateImageButtonText();
    }

    // Used when editing an existing Quiz Info file.
    public void LoadExistingInfo(QuizInfoData info)
    {
        if (info == null)
            return;

        if (infoTitleInput != null)
            infoTitleInput.text = info.title ?? "";

        if (entryDescriptionInput != null)
            entryDescriptionInput.text = info.description ?? "";

        imagePath = info.imagePath ?? "";
        UpdateImageButtonText();
    }

    // Collects the information entered by the player.
    public QuizInfoData GetInfoData()
    {
        return new QuizInfoData
        {
            id = questionID,
            title = infoTitleInput != null
                ? infoTitleInput.text
                : "",
            description = entryDescriptionInput != null
                ? entryDescriptionInput.text
                : "",
            imagePath = imagePath
        };
    }

    private void UpdateImageButtonText()
    {
        if (imageButtonText == null)
            return;

        if (string.IsNullOrEmpty(imagePath))
        {
            imageButtonText.text = "Select Image";
        }

        else
        {
            imageButtonText.text = System.IO.Path.GetFileName(imagePath);
        }
    }

    // We'll call this after the image browser is implemented.
    public void SetImagePath(string path)
    {
        imagePath = path ?? "";
        UpdateImageButtonText();
    }
}