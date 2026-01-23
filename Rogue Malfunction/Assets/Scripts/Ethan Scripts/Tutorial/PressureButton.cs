using UnityEngine;

public class PressureButton : MonoBehaviour
{
    [Header("Detection")]
    public LayerMask boxLayer;

    [Header("Button Visual")]
    public Transform buttonTop;
    public float pressedHeight = 0.05f;
    public float pressSpeed = 8f;

    [Header("Connected Doors")]
    public Door[] connectedDoors;   // <-- CHANGED

    private int boxCount;
    private Vector3 initialButtonPos;

    void Start()
    {
        if (buttonTop != null)
            initialButtonPos = buttonTop.localPosition;
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & boxLayer) == 0)
            return;

        boxCount++;
        UpdateState();
    }

    void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & boxLayer) == 0)
            return;

        boxCount = Mathf.Max(0, boxCount - 1);
        UpdateState();
    }

    void UpdateState()
    {
        bool pressed = boxCount > 0;

        if (connectedDoors == null)
            return;

        foreach (Door door in connectedDoors)
        {
            if (door == null)
                continue;

            if (pressed)
                door.Open();
            else
                door.Close();
        }
    }

    void Update()
    {
        if (buttonTop == null)
            return;

        Vector3 targetPos = initialButtonPos;

        if (boxCount > 0)
            targetPos.y -= pressedHeight;

        buttonTop.localPosition = Vector3.Lerp(
            buttonTop.localPosition,
            targetPos,
            Time.deltaTime * pressSpeed
        );
    }
}
