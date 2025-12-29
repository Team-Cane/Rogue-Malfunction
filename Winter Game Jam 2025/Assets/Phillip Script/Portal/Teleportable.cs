using UnityEngine;

public class Teleportable : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;

    public Transform Root => transform;
    public CharacterController Controller => characterController;

    void Reset()
    {
        characterController = GetComponent<CharacterController>();
    }

    public void OnPreTeleport()
    {
        if (characterController) characterController.enabled = false;
    }

    public void OnPostTeleport()
    {
        if (characterController) characterController.enabled = true;
    }
}
