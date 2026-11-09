using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelect : MonoBehaviour
{
    //Depreacted
    /*
     public void SelectLevel()
    {
        SceneManager.LoadScene("Level");
    }
    */

    public void SelectLevel_Debug()
    {
        Debug.Log("Level has not been made");
    }

    public void SelectLevel_Back()
    {
        SceneTransitionManager.Instance.SwitchScene("MapMenu", true);
    }

}
