using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Portal : MonoBehaviour
{
    [Header("Exit")]
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Vector3 exitOffset;

    [Header("Rules")]
    [SerializeField] private float gateSeconds = 0.35f;
    [SerializeField] private bool rotateToExit = false;
    [SerializeField] private bool preserveVelocity = true;

    [Header("Filtering")]
    [SerializeField] private LayerMask validLayers = ~0;

    int portalId;

    void Awake()
    {
        portalId = GetInstanceID();
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (((1 << other.gameObject.layer) & validLayers) == 0)
            return;

        if (!other.TryGetComponent(out Teleportable teleportable))
            teleportable = other.GetComponentInParent<Teleportable>();

        if (!teleportable || !exitPoint)
            return;

        if (!TeleportService.CanUsePortal(teleportable, portalId))
            return;

        var exitPos = exitPoint.position + exitOffset;

        TeleportService.Teleport(
            teleportable,
            portalId,
            exitPos,
            exitPoint.rotation,
            rotateToExit,
            preserveVelocity,
            gateSeconds);
    }
}
