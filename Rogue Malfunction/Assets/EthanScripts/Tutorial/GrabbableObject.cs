using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class GrabbableObject : MonoBehaviour, IGrabbable
{
    public float moveSmoothness = 12f;

    private Rigidbody rb;
    private bool isGrabbed;
    private Vector3 grabOffset;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // ✅ Kinematic by default so player collisions don't move it
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void OnGrab(Transform holder)
    {
        isGrabbed = true;

        // Compute grab offset
        grabOffset = transform.position - holder.position;

        // Freeze rotation while grabbed
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        // Make kinematic false only for MovePosition-based collision
        rb.isKinematic = false;
    }

    public void MoveTo(Vector3 targetPosition)
    {
        if (!isGrabbed)
            return;

        targetPosition.y = rb.position.y;

        // Move the object with collisions respected
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

        // Make kinematic again to prevent physics pushing
        rb.isKinematic = true;

        // Unlock rotation
        rb.constraints = RigidbodyConstraints.None;
    }
}
