using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class GrabbableObject : MonoBehaviour, IGrabbable
{
    [Header("Grab Follow")]
    public float followSpeed = 10f;

    [Header("Block Detection")]
    public float blockReleaseDot = 0.6f;

    [Header("Button Compatibility")]
    public LayerMask buttonLayer; // Assign layer(s) that PressureButton uses

    private Rigidbody rb;
    private Collider objectCollider;
    private Collider playerCollider;
    private Transform playerTransform;

    private bool isGrabbed;

    // Locked world-space offset (XZ only)
    private Vector3 grabOffsetWorld;

    private int originalLayer;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        objectCollider = GetComponent<Collider>();

        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    // ================== GRAB ==================
    public void OnGrab(Transform anchor)
    {
        Collider pc = anchor.GetComponentInParent<Collider>();
        if (pc == null)
            return;

        if (IsPlayerStandingOnObject(pc))
            return;

        isGrabbed = true;
        playerCollider = pc;
        playerTransform = pc.transform;

        // Lock XZ offset ONCE
        grabOffsetWorld = rb.position - playerTransform.position;
        grabOffsetWorld.y = 0f;

        // Prevent pushing other objects while held
        originalLayer = gameObject.layer;

        // Set to a layer that ignores other objects but still collides with button triggers
        gameObject.layer = LayerMask.NameToLayer("HeldObject");
    }

    public void OnRelease()
    {
        isGrabbed = false;
        playerCollider = null;
        playerTransform = null;

        // Restore original layer so it can trigger buttons normally
        gameObject.layer = originalLayer;
    }

    // ================== FOLLOW ==================
    void FixedUpdate()
    {
        if (!isGrabbed || playerTransform == null)
            return;

        // Get the player's grounded state
        PlayerGroundedChecker groundedChecker = playerTransform.GetComponent<PlayerGroundedChecker>();
        bool playerGrounded = groundedChecker != null && groundedChecker.isGrounded;

        // Only follow the player if grounded
        if (!playerGrounded)
            return;

        // Calculate the intended target position in XZ
        Vector3 targetXZ = playerTransform.position + grabOffsetWorld;

        // Only move if the player is actually moving (prevents snapping on landing)
        if ((targetXZ - rb.position).sqrMagnitude < 0.001f)
            return;

        Vector3 current = rb.position;
        Vector3 desired = new Vector3(
            targetXZ.x,
            current.y, // Y controlled by physics only
            targetXZ.z
        );

        if (IsBlocked(current, desired))
        {
            OnRelease();
            return;
        }

        rb.MovePosition(
            Vector3.MoveTowards(
                current,
                desired,
                followSpeed * Time.fixedDeltaTime
            )
        );
    }

    // ================== BLOCK DETECTION ==================
    private bool IsBlocked(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float distance = dir.magnitude;
        if (distance < 0.001f) return false;

        dir.Normalize();

        // Cast a small box in XZ to detect blocking objects
        Vector3 halfExtents = objectCollider.bounds.extents;
        halfExtents.y = 0.1f; // small height for XZ only

        RaycastHit hit;
        if (Physics.BoxCast(
            from,
            halfExtents,
            dir,
            out hit,
            Quaternion.identity,
            distance,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            // Ignore collisions with player
            if (hit.collider != playerCollider)
                return true;
        }

        return false;
    }

    // ================== HELPERS ==================
    private bool IsPlayerStandingOnObject(Collider playerCol)
    {
        return playerCol.bounds.min.y > objectCollider.bounds.max.y - 0.05f;
    }
}
