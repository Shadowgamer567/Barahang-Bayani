using UnityEngine;

[CreateAssetMenu(menuName = "Modules/Module Info")]
public class ModuleInfoData : ScriptableObject
{
    public string moduleTitle;

    [TextArea(5, 15)]
    public string moduleDescription;

    public Sprite moduleImage;
}
