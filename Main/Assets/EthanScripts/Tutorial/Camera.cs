using UnityEngine;

public class IsometricCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Isometric Settings")]
    public Vector3 isometricOffset = new Vector3(0f, 12f, -12f);
    public Vector3 isometricRotation = new Vector3(35f, 45f, 0f);

    [Header("Follow Settings")]
    public float followSmoothTime = 0.15f;
    private Vector3 velocity;

    [Header("Look Ahead")]
    public bool enableLookAhead = true;
    public float lookAheadStrength = 2f;
    private Vector3 lastTargetPosition;

    [Header("Zoom (Design Time)")]
    [Range(0f, 1f)] public float zoom = 0.5f;
    public float minZoomMultiplier = 0.7f;
    public float maxZoomMultiplier = 1.8f;

    [Header("Collision Settings")]
    public LayerMask obstacleLayers;
    public float collisionPushAmount = 0.5f;
    public float minCameraHeight = 2f;

    [Header("Camera Bounds (Optional)")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    void Start()
    {
        if (!target)
        {
            Debug.LogError("IsometricCameraController: No target assigned.");
            enabled = false;
            return;
        }

        transform.rotation = Quaternion.Euler(isometricRotation);
        lastTargetPosition = target.position;
    }

    void LateUpdate()
    {
        float zoomMultiplier = Mathf.Lerp(minZoomMultiplier, maxZoomMultiplier, zoom);
        Vector3 zoomedOffset = isometricOffset * zoomMultiplier;

        Vector3 desiredPosition = target.position + zoomedOffset;

        if (enableLookAhead)
        {
            Vector3 movementDelta = target.position - lastTargetPosition;
            desiredPosition += new Vector3(movementDelta.x, 0f, movementDelta.z) * lookAheadStrength;
        }

        RaycastHit hit;
        Vector3 directionToTarget = (desiredPosition - target.position).normalized;
        float distance = Vector3.Distance(desiredPosition, target.position);

        if (Physics.Raycast(target.position, directionToTarget, out hit, distance, obstacleLayers))
        {
            desiredPosition = hit.point - directionToTarget * collisionPushAmount;
            desiredPosition.y = Mathf.Max(desiredPosition.y, target.position.y + minCameraHeight);
        }

        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            followSmoothTime
        );

        if (useBounds)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minBounds.x, maxBounds.x);
            smoothedPosition.z = Mathf.Clamp(smoothedPosition.z, minBounds.y, maxBounds.y);
        }

        transform.position = smoothedPosition;
        transform.rotation = Quaternion.Euler(isometricRotation);

        lastTargetPosition = target.position;
    }
}
