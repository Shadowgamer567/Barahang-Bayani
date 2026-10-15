using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ModuleContentPage : MonoBehaviour
{
    private enum PageDirection
    {
        Previous,
        Next
    }

    [SerializeField] private ScrollablePanelDisplay panelDisplay;
    [SerializeField] private PageDirection direction = PageDirection.Next;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnButtonClicked);
        BringButtonAboveScrollView();
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }

    public void OnButtonClicked()
    {
        if (panelDisplay == null)
        {
            Debug.LogError("ScrollablePanelDisplay is not assigned.", this);
            return;
        }

        if (panelDisplay.ContentCount == 0)
        {
            Debug.LogWarning("ScrollablePanelDisplay has no content items.", this);
            return;
        }

        int currentIndex = panelDisplay.CurrentContentIndex;
        if (currentIndex < 0)
        {
            currentIndex = 0;
        }

        int offset = direction == PageDirection.Next ? 1 : -1;
        int nextIndex = currentIndex + offset;

        if (nextIndex < 0 || nextIndex >= panelDisplay.ContentCount)
        {
            return;
        }

        panelDisplay.DisplayContentAt(nextIndex);
    }

    private void BringButtonAboveScrollView()
    {
        if (panelDisplay == null || panelDisplay.ScrollRect == null)
        {
            return;
        }

        Transform scrollView = panelDisplay.ScrollRect.transform;
        Transform scrollViewParent = scrollView.parent;
        if (scrollViewParent == null)
        {
            return;
        }

        if (button.transform.IsChildOf(scrollView))
        {
            button.transform.SetParent(scrollViewParent, true);
        }

        Transform sortingRoot = button.transform;
        while (sortingRoot.parent != null && sortingRoot.parent != scrollViewParent)
        {
            sortingRoot = sortingRoot.parent;
        }

        if (sortingRoot.parent == scrollViewParent)
        {
            sortingRoot.SetAsLastSibling();
        }
    }
}
