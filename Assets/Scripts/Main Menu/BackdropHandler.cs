using UnityEngine;
using UnityEngine.UI;

public class BackdropHandler : MonoBehaviour
{
    [SerializeField] private Image backdrop;
    public GameObject BackdropObject => backdrop != null ? backdrop.gameObject : null;

    private void OnEnable()
    {
        SetBackdropVisible(true);
    }

    private void OnDisable()
    {
        SetBackdropVisible(false);
    }

    private void SetBackdropVisible(bool visible)
    {
        if (backdrop == null)
        {
            Debug.LogError("Backdrop image is not assigned.", this);
            return;
        }

        if (visible)
        {
            Color color = backdrop.color;
            color.a = 1f;
            backdrop.color = color;
        }

        backdrop.gameObject.SetActive(visible);
    }
}
