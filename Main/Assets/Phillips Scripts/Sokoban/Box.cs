using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class SokobanGrabbable : MonoBehaviour, IGrabbable, IGrabMoveable
{
    [Header("Grid Step")]
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float stepTime = 0.08f;
    [SerializeField] private float pushThreshold = 0.35f;

    [Header("Blocking")]
    [SerializeField] private LayerMask blockingMask;
    [SerializeField] private float boundsPadding = 0.02f;

    [Header("Debug")]
    [SerializeField] private bool debugBox;

    private Rigidbody body;
    private Collider col;

    private bool grabbed;

    private bool stepping;
    private Vector3 stepFrom;
    private Vector3 stepTo;
    private float stepT;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        col.isTrigger = false;

        body.useGravity = false;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void OnGrab(Transform holder)
    {
        grabbed = true;
        stepping = false;
        stepT = 0f;

        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;

        if (debugBox) Debug.Log($"[BOX] Grabbed: {name}");
    }

    public void MoveTo(Vector3 targetPosition)
    {
        if (!grabbed) return;
        if (stepping) return;

        Vector3 current = body.position;
        targetPosition.y = current.y;

        Vector3 delta = targetPosition - current;
        delta.y = 0f;

        if (delta.sqrMagnitude < 0.0001f) return;

        Vector3 dir = GetCardinal(delta);
        if (dir == Vector3.zero) return;

        float along = Vector3.Dot(delta, dir);
        if (Mathf.Abs(along) < pushThreshold) return;

        Vector3 desired = current + dir * cellSize;
        desired.y = current.y;

        if (IsBlocked(desired))
        {
            if (debugBox) Debug.Log($"[BOX] Blocked step at {desired} ({name}) mask={blockingMask.value}");
            return;
        }

        stepping = true;
        stepFrom = current;
        stepTo = desired;
        stepT = 0f;

        if (debugBox) Debug.Log($"[BOX] Step {name}: {stepFrom} -> {stepTo}");
    }

    public void OnRelease()
    {
        grabbed = false;
        stepping = false;

        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;

        if (debugBox) Debug.Log($"[BOX] Released: {name}");
    }

    private void FixedUpdate()
    {
        if (!stepping) return;

        stepT += Time.fixedDeltaTime / Mathf.Max(0.0001f, stepTime);
        float t = Mathf.Clamp01(stepT);

        body.MovePosition(Vector3.Lerp(stepFrom, stepTo, t));

        if (t >= 1f)
        {
            body.MovePosition(stepTo);
            stepping = false;
            stepT = 0f;
        }
    }

    private Vector3 GetCardinal(Vector3 v)
    {
        v.y = 0f;

        if (Mathf.Abs(v.x) > Mathf.Abs(v.z))
            return v.x >= 0f ? Vector3.right : Vector3.left;

        return v.z >= 0f ? Vector3.forward : Vector3.back;
    }

    private bool IsBlocked(Vector3 destination)
    {
        Bounds b = col.bounds;

        Vector3 half = b.extents;
        half.x = Mathf.Max(0.01f, half.x - boundsPadding);
        half.z = Mathf.Max(0.01f, half.z - boundsPadding);

        Vector3 centerOffset = b.center - transform.position;
        Vector3 checkCenter = destination + centerOffset;

        Collider[] hits = Physics.OverlapBox(
            checkCenter,
            half,
            transform.rotation,
            blockingMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i]) continue;
            if (hits[i] == col) continue;
            return true;
        }

        return false;
    }
}
