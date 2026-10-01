using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModuleInfoUI : MonoBehaviour
{
    public static ModuleInfoUI Instance;

    [Header("Canvas")]
    public GameObject moduleCanvas;

    [Header("UI")]
    public Image moduleImage;

    public TextMeshProUGUI infoTitle;

    public TextMeshProUGUI infoDescription;

    private void Awake()
    {
        Instance = this;

        moduleCanvas.SetActive(false);
    }

    public void OpenModule(ModuleInfoData data)
    {
        moduleCanvas.SetActive(true);

        infoTitle.text = data.moduleTitle;

        infoDescription.text = data.moduleDescription;

        moduleImage.sprite = data.moduleImage;
    }

    public void CloseModule()
    {
        moduleCanvas.SetActive(false);
    }
}
