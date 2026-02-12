using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class GrabbableObject : MonoBehaviour, IGrabbable
{
    public float moveSmoothness = 12f;

    private Rigidbody rb;
    private bool isGrabbed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    public void OnGrab(Transform holder)
    {
        isGrabbed = true;

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.isKinematic = false;
    }

    public void MoveTo(Vector3 targetPosition)
    {
        if (!isGrabbed) return;

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
        rb.constraints = RigidbodyConstraints.None;
    }
}
