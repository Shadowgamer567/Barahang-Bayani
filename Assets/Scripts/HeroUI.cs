using UnityEngine;

public class HeroUI : MonoBehaviour
{
    public HeroStats hero;

    [Header("Canvas Position")]
    public float heightOffset = 3.15f;

    private Camera mainCamera;


    void Start()
    {
        if (hero == null)
        {
            hero = GetComponentInParent<HeroStats>();
        }

        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (hero == null)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        // Follow the hero's position above their head.
        transform.position =
            hero.transform.position + Vector3.up * heightOffset;

        // Face the camera, matching EnemyUI.
        transform.forward = mainCamera.transform.forward;
    }
}
