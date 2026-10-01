using UnityEngine;

public class ModuleButton : MonoBehaviour
{
    public ModuleInfoData moduleData;

    public void OpenModule()
    {
        ModuleInfoUI.Instance.OpenModule(moduleData);
    }
}