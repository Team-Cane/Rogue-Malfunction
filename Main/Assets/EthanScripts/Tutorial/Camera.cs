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

    [Header("Collision Settings")]
    public LayerMask obstacleLayers;      // Walls and obstacles
    public float collisionPushAmount = 0.5f; // How much to push down/forward on collision
    public float minCameraHeight = 2f;    // Minimum camera height above target

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
        Vector3 desiredPosition = target.position + isometricOffset;

        // Look-ahead based on movement direction
        if (enableLookAhead)
        {
            Vector3 movementDelta = target.position - lastTargetPosition;
            desiredPosition += new Vector3(
                movementDelta.x,
                0f,
                movementDelta.z
            ) * lookAheadStrength;
        }

        // --- Collision Check ---
        RaycastHit hit;
        Vector3 directionToTarget = (desiredPosition - target.position).normalized;
        float distance = Vector3.Distance(desiredPosition, target.position);

        if (Physics.Raycast(target.position, directionToTarget, out hit, distance, obstacleLayers))
        {
            // Push camera forward toward player and down slightly
            desiredPosition = hit.point - directionToTarget * collisionPushAmount;
            desiredPosition.y = Mathf.Max(desiredPosition.y, target.position.y + minCameraHeight);
        }

        // Smooth follow
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            followSmoothTime
        );

        // Clamp to bounds if enabled
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