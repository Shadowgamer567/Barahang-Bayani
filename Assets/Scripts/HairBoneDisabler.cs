using System;
using UnityEngine;

public class HairBoneDisabler : MonoBehaviour
{
    private const string HairBonePrefix = "J_Sec_Hair";

    private void Awake()
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);

        foreach (Transform bone in transforms)
        {
            if (bone != transform && bone.name.StartsWith(HairBonePrefix, StringComparison.Ordinal))
            {
                bone.gameObject.SetActive(false);
            }
        }
    }
}
