using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Teleportable : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;

    public Transform Root => transform;
    public Rigidbody Rigidbody => rb;

    void Reset()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void OnPreTeleport()
    {
        rb.isKinematic = true;
    }

    public void OnPostTeleport(Vector3 restoredVelocity)
    {
        rb.isKinematic = false;
        rb.linearVelocity = restoredVelocity;
    }
}
