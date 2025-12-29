using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class GrabbableObject : MonoBehaviour, IGrabbable
{
    public float moveSmoothness = 12f;

    private Rigidbody rb;
    private Collider objectCollider;
    private Collider playerCollider;

    private bool isGrabbed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        objectCollider = GetComponent<Collider>();

        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    // ---------------- GRAB ----------------

    public void OnGrab(Transform holder)
    {
        isGrabbed = true;
        rb.isKinematic = false;

        // Ignore collision with player while grabbed
        playerCollider = holder.GetComponentInParent<Collider>();
        if (playerCollider != null)
        {
            Physics.IgnoreCollision(objectCollider, playerCollider, true);
        }
    }

    public void MoveTo(Vector3 targetPosition)
    {
        if (!isGrabbed)
            return;

        // Never move vertically
        targetPosition.y = rb.position.y;

        Vector3 newPos = Vector3.Lerp(
            rb.position,
            targetPosition,
            Time.fixedDeltaTime * moveSmoothness
        );

        rb.MovePosition(newPos);
    }

    public void OnRelease()
    {
        isGrabbed = false;
        rb.isKinematic = true;

        // Restore collision
        if (playerCollider != null)
        {
            Physics.IgnoreCollision(objectCollider, playerCollider, false);
            playerCollider = null;
        }
    }
}
