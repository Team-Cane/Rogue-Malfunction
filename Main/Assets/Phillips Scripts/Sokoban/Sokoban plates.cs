using UnityEngine;

public class PressurePlateSensor : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask validBoxLayers;

    [Header("Optional Visual Press")]
    [SerializeField] private Transform plateTop;
    [SerializeField] private float pressDepth = 0.05f;
    [SerializeField] private float pressLerpSpeed = 8f;

    [Header("Counter")]
    [SerializeField] private PressurePlateCounter counter;

    private int boxesOnPlate;
    private bool currentlyPressed;

    private Vector3 startLocalPos;

    private void Start()
    {
        if (plateTop != null)
            startLocalPos = plateTop.localPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & validBoxLayers) == 0)
            return;

        boxesOnPlate++;
        UpdatePressedState();
    }

    private void OnTriggerExit(Collider other)
    {
        if (((1 << other.gameObject.layer) & validBoxLayers) == 0)
            return;

        boxesOnPlate = Mathf.Max(0, boxesOnPlate - 1);
        UpdatePressedState();
    }

    private void UpdatePressedState()
    {
        bool pressedNow = boxesOnPlate > 0;

        if (pressedNow == currentlyPressed)
            return;

        currentlyPressed = pressedNow;

        if (counter == null)
            return;

        if (currentlyPressed) counter.NotifyPressed();
        else counter.NotifyReleased();
    }

    private void Update()
    {
        if (plateTop == null)
            return;

        Vector3 target = startLocalPos;

        if (boxesOnPlate > 0)
            target.y -= pressDepth;

        plateTop.localPosition = Vector3.Lerp(
            plateTop.localPosition,
            target,
            Time.deltaTime * pressLerpSpeed
        );
    }
}
