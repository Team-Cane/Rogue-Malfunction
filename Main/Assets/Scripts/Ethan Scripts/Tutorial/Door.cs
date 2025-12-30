using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Door Movement")]
    public Transform doorMesh;
    public Vector3 openOffset = new Vector3(0f, 3f, 0f);
    public float openSpeed = 4f;

    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isOpen;

    void Start()
    {
        if (doorMesh == null)
            doorMesh = transform;

        closedPosition = doorMesh.position;
        openPosition = closedPosition + openOffset;
    }

    public void Open()
    {
        isOpen = true;
    }

    public void Close()
    {
        isOpen = false;
    }

    void Update()
    {
        Vector3 target = isOpen ? openPosition : closedPosition;

        doorMesh.position = Vector3.Lerp(
            doorMesh.position,
            target,
            Time.deltaTime * openSpeed
        );
    }
}
