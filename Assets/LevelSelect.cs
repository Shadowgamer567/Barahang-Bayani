using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelect : MonoBehaviour
{
     public void SelectLevel()
    {
        SceneManager.LoadScene("Level");
    }

    public void SelectLevel_Debug()
    {
        Debug.Log("Level has not been made");
    }

}
