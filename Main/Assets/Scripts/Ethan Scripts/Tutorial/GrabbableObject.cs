using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class GrabbableObject : MonoBehaviour, IGrabbable
{
    public float moveSmoothness = 12f;
    public float pushCancelStrength = 15f;

    private Rigidbody rb;
    private Collider objectCollider;
    private Collider playerCollider;

    private bool isGrabbed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        objectCollider = GetComponent<Collider>();

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void OnGrab(Transform holder)
    {
        isGrabbed = true;
        playerCollider = holder.GetComponentInParent<Collider>();
    }

    public void MoveTo(Vector3 targetPosition)
    {
        if (!isGrabbed)
            return;

        Vector3 desired = new Vector3(
            targetPosition.x,
            rb.position.y,
            targetPosition.z
        );

        Vector3 newPos = Vector3.Lerp(
            rb.position,
            desired,
            Time.fixedDeltaTime * moveSmoothness
        );

        rb.MovePosition(newPos);
    }

    public void OnRelease()
    {
        isGrabbed = false;
        playerCollider = null;
    }

    // ---------------- PUSH PREVENTION ----------------

    void OnCollisionStay(Collision collision)
    {
        if (playerCollider == null)
            return;

        if (collision.collider != playerCollider)
            return;

        // Remove horizontal velocity caused by player push
        Vector3 vel = rb.linearVelocity;
        vel.x = Mathf.Lerp(vel.x, 0f, pushCancelStrength * Time.fixedDeltaTime);
        vel.z = Mathf.Lerp(vel.z, 0f, pushCancelStrength * Time.fixedDeltaTime);
        rb.linearVelocity = vel;
    }
}
