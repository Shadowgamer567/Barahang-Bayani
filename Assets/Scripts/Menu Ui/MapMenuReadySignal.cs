
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapMenuReadySignal : MonoBehaviour
{
    private IEnumerator Start()
    {
        // Allow the newly loaded scene's Start methods
        // and initial UI layout to run.
        yield return null;

        // Allow UI layout and rendering preparation to settle.
        yield return new WaitForEndOfFrame();

        if (SceneManager.GetActiveScene().name != "MapMenu")
        {
            yield break;
        }

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.NotifySceneReady("MapMenu");

            Debug.Log("[MapMenu] Initial scene setup completed. " + "Transition can reveal MapMenu.");
        }
    }
}
