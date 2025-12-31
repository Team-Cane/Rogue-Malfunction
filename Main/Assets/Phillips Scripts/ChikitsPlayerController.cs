using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ChikitsPlayerController : MonoBehaviour
{
    public enum ControlScheme
    {
        Normal,
        SwapWASD
    }

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float acceleration = 15f;
    public float rotationSpeed = 12f;

    [Header("Controls")]
    [SerializeField] private ControlScheme controlScheme = ControlScheme.Normal;
    [SerializeField] private bool invertHorizontal;
    [SerializeField] private bool invertVertical;

    [Header("Jump")]
    public float jumpForce = 6f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;

    [Header("Air Control")]
    [Range(0f, 1f)]
    public float airControlPercent = 0.35f;
    public float airAccelerationMultiplier = 0.5f;

    [Header("Grab")]
    public Transform holdPoint;
    public Transform grabOrigin;
    public float grabRange = 1.2f;
    public float grabMaxAngle = 55f;
    public LayerMask grabBlockerMask;
    public LayerMask interactLayer;
    public bool requireLineOfSight = true;

    [Header("Grab Move")]
    [SerializeField] private float grabMoveStrength = 0.5f;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 lastMoveDirection = Vector3.forward;
    private Vector3 grabOffsetLocal;

    private bool isGrounded;
    private bool jumpQueued;

    private IGrabbable grabbedObject;
    private IGrabMoveable grabbedMover;

    private Vector3 grabDirection;
    private float grabDistance;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Update()
    {
        ReadInput();
        HandleInteraction();

        if (Input.GetKeyDown(KeyCode.Space))
            jumpQueued = true;
    }

    void FixedUpdate()
    {
        CheckGround();
        HandleMovement();
        HandleRotation();
        HandleJump();
    }

    void LateUpdate()
    {
        if (grabbedObject == null || holdPoint == null)
            return;

        holdPoint.position = transform.position + (transform.rotation * grabOffsetLocal);
    }

    void ReadInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (invertHorizontal) h = -h;
        if (invertVertical) v = -v;

        if (controlScheme == ControlScheme.SwapWASD)
        {
            float temp = h;
            h = v;
            v = temp;
        }

        Camera cam = Camera.main;
        if (!cam)
        {
            moveInput = Vector3.zero;
            return;
        }

        Vector3 forward = cam.transform.forward;
        Vector3 right = cam.transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 input = (forward * v + right * h).normalized;

        if (input.sqrMagnitude > 0.01f)
        {
            moveInput = input;
            lastMoveDirection = input;
        }
        else
        {
            moveInput = Vector3.zero;
        }
    }

    void HandleMovement()
    {
        float control = isGrounded ? 1f : airControlPercent;
        float accel = isGrounded ? acceleration : acceleration * airAccelerationMultiplier;

        Vector3 targetVelocity = moveInput * moveSpeed * control;
        Vector3 velocity = rb.linearVelocity;

        Vector3 change = new Vector3(
            targetVelocity.x - velocity.x,
            0f,
            targetVelocity.z - velocity.z
        );

        change = Vector3.ClampMagnitude(change, accel);
        rb.AddForce(change, ForceMode.VelocityChange);
    }

    void HandleRotation()
    {
        if (lastMoveDirection.sqrMagnitude < 0.01f)
            return;

        Quaternion target = Quaternion.LookRotation(lastMoveDirection);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, rotationSpeed * Time.fixedDeltaTime));
    }

    void CheckGround()
    {
        if (!groundCheck)
        {
            isGrounded = false;
            return;
        }

        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    void HandleJump()
    {
        if (!jumpQueued || !isGrounded)
            return;

        jumpQueued = false;

        if (grabbedObject != null)
            ReleaseObject();

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void HandleInteraction()
    {
        if (Input.GetMouseButton(0))
        {
            if (grabbedObject == null)
                TryGrabObject();
            else if (grabbedMover != null)
                MoveGrabbedObject();
        }
        else
        {
            ReleaseObject();
        }
    }

    void TryGrabObject()
    {
        Vector3 origin = grabOrigin ? grabOrigin.position : transform.position;

        Collider[] hits = Physics.OverlapSphere(origin, grabRange, interactLayer, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return;

        Vector3 facing = lastMoveDirection.sqrMagnitude > 0.01f ? lastMoveDirection : transform.forward;
        facing.y = 0f;
        facing.Normalize();

        float cosLimit = Mathf.Cos(grabMaxAngle * Mathf.Deg2Rad);

        float bestScore = float.MinValue;
        IGrabbable bestGrab = null;
        Collider bestCol = null;

        foreach (Collider c in hits)
        {
            if (!c) continue;

            IGrabbable grab = c.GetComponentInParent<IGrabbable>();
            if (grab == null) continue;

            Vector3 to = c.bounds.center - origin;
            to.y = 0f;

            float dist = to.magnitude;
            if (dist < 0.01f) continue;

            Vector3 dir = to / dist;
            float dot = Vector3.Dot(facing, dir);

            if (dot < cosLimit) continue;

            if (requireLineOfSight)
            {
                if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, grabBlockerMask, QueryTriggerInteraction.Ignore))
                {
                    IGrabbable hitGrab = hit.collider ? hit.collider.GetComponentInParent<IGrabbable>() : null;
                    if (hitGrab != grab)
                        continue;
                }
            }

            float score = dot * 2f - dist;
            if (score > bestScore)
            {
                bestScore = score;
                bestGrab = grab;
                bestCol = c;
            }
        }

        if (bestGrab == null || bestCol == null || holdPoint == null) return;

        grabbedObject = bestGrab;
        grabbedMover = bestGrab as IGrabMoveable;

        Vector3 offsetWorld = facing * Vector3.Distance(transform.position, holdPoint.position);
        grabOffsetLocal = Quaternion.Inverse(transform.rotation) * offsetWorld;

        Vector3 toObject = bestCol.transform.position - transform.position;
        toObject.y = 0f;

        if (Mathf.Abs(toObject.x) > Mathf.Abs(toObject.z))
            grabDirection = new Vector3(Mathf.Sign(toObject.x), 0f, 0f);
        else
            grabDirection = new Vector3(0f, 0f, Mathf.Sign(toObject.z));

        grabDistance = Mathf.Abs(Vector3.Dot(toObject, grabDirection));

        grabbedObject.OnGrab(holdPoint);
    }

    void MoveGrabbedObject()
    {
        if (grabbedMover == null)
            return;

        float moveAmount = Vector3.Dot(moveInput, grabDirection);

        Vector3 targetPos = transform.position
                            + grabDirection * grabDistance
                            + grabDirection * (moveAmount * grabMoveStrength);

        grabbedMover.MoveTo(targetPos);
    }

    void ReleaseObject()
    {
        if (grabbedObject == null) return;

        grabbedObject.OnRelease();
        grabbedObject = null;
        grabbedMover = null;

        grabOffsetLocal = Vector3.zero;
        grabDirection = Vector3.zero;
        grabDistance = 0f;
    }

    public void SetControlScheme(ControlScheme scheme)
    {
        controlScheme = scheme;
    }
}
