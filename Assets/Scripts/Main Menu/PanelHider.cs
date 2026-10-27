using System;
using System.Collections.Generic;
using UnityEngine;

public class PanelHider : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuTitle;
    [SerializeField] private GameObject[] additionalObjectsToHide;
    [SerializeField] private bool hideSiblingPanels = true;

    private readonly Dictionary<GameObject, bool> panelActiveStates = new Dictionary<GameObject, bool>();
    private readonly Dictionary<GameObject, bool> backdropActiveStates = new Dictionary<GameObject, bool>();
    private bool hasPanelSnapshot;
    private bool mainMenuTitleWasActive;
    private bool hasMainMenuTitleState;

    private void OnEnable()
    {
        HidePanels();
    }

    public void HidePanels()
    {
        Transform panelContainer = transform.parent;
        if (panelContainer == null)
        {
            Debug.LogError("PanelHider requires a parent panel container.", this);
            return;
        }

        if (!hasPanelSnapshot)
        {
            CapturePanelStates(panelContainer);
            CaptureBackdropStates(panelContainer);
            hasPanelSnapshot = true;
        }

        if (hideSiblingPanels)
        {
            HideSiblingPanels(panelContainer);
        }
        else
        {
            HideMainMenuTitle();
        }

        HideAdditionalObjects();
        RestoreBackdropStates();
    }

    private void OnDisable()
    {
        if (!hideSiblingPanels && hasMainMenuTitleState)
        {
            RestoreMainMenuTitle();
            panelActiveStates.Clear();
            backdropActiveStates.Clear();
            hasPanelSnapshot = false;
        }
    }

    public void RestorePanels()
    {
        if (!hasPanelSnapshot)
        {
            return;
        }

        foreach (KeyValuePair<GameObject, bool> panelState in panelActiveStates)
        {
            if (panelState.Key != null)
            {
                panelState.Key.SetActive(panelState.Value);
            }
        }

        RestoreBackdropStates();
        panelActiveStates.Clear();
        backdropActiveStates.Clear();
        hasPanelSnapshot = false;
        hasMainMenuTitleState = false;
    }

    private void CapturePanelStates(Transform panelContainer)
    {
        if (hideSiblingPanels)
        {
            for (int i = 0; i < panelContainer.childCount; i++)
            {
                GameObject sibling = panelContainer.GetChild(i).gameObject;
                if (IsPanelToHide(sibling))
                {
                    panelActiveStates.Add(sibling, sibling.activeSelf);
                }
            }
        }

        if (mainMenuTitle != null)
        {
            mainMenuTitleWasActive = mainMenuTitle.activeSelf;
            hasMainMenuTitleState = true;

            if (hideSiblingPanels && !panelActiveStates.ContainsKey(mainMenuTitle))
            {
                panelActiveStates.Add(mainMenuTitle, mainMenuTitleWasActive);
            }
        }

        if (additionalObjectsToHide == null)
        {
            return;
        }

        foreach (GameObject target in additionalObjectsToHide)
        {
            if (target != null && !panelActiveStates.ContainsKey(target))
            {
                panelActiveStates.Add(target, target.activeSelf);
            }
        }
    }

    private void CaptureBackdropStates(Transform panelContainer)
    {
        for (int i = 0; i < panelContainer.childCount; i++)
        {
            GameObject sibling = panelContainer.GetChild(i).gameObject;
            if (IsBackdrop(sibling))
            {
                backdropActiveStates[sibling] = sibling.activeSelf;
            }
        }

        foreach (BackdropHandler handler in panelContainer.GetComponentsInChildren<BackdropHandler>(true))
        {
            GameObject backdrop = handler.BackdropObject;
            if (backdrop != null)
            {
                backdropActiveStates[backdrop] = backdrop.activeSelf;
            }
        }
    }

    private void HideSiblingPanels(Transform panelContainer)
    {
        for (int i = 0; i < panelContainer.childCount; i++)
        {
            GameObject sibling = panelContainer.GetChild(i).gameObject;
            if (IsPanelToHide(sibling))
            {
                sibling.SetActive(false);
            }
        }

        HideMainMenuTitle();
    }

    private void HideMainMenuTitle()
    {
        if (mainMenuTitle != null)
        {
            mainMenuTitle.SetActive(false);
        }
    }

    private void HideAdditionalObjects()
    {
        if (additionalObjectsToHide == null)
        {
            return;
        }

        foreach (GameObject target in additionalObjectsToHide)
        {
            if (target != null)
            {
                target.SetActive(false);
            }
        }
    }

    private void RestoreMainMenuTitle()
    {
        if (mainMenuTitle != null)
        {
            mainMenuTitle.SetActive(mainMenuTitleWasActive);
        }

        hasMainMenuTitleState = false;
    }

    private bool IsPanelToHide(GameObject sibling)
    {
        return sibling != gameObject &&
               !IsBackdrop(sibling) &&
               sibling.name.EndsWith("Panel", StringComparison.Ordinal);
    }

    private static bool IsBackdrop(GameObject target)
    {
        return target.name.IndexOf("backdrop", StringComparison.OrdinalIgnoreCase) >= 0 ||
               target.name.IndexOf("background", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void RestoreBackdropStates()
    {
        foreach (KeyValuePair<GameObject, bool> backdropState in backdropActiveStates)
        {
            if (backdropState.Key != null)
            {
                backdropState.Key.SetActive(backdropState.Value);
            }
        }
    }
}
