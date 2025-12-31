using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class IsometricPlayerController : MonoBehaviour
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
    [SerializeField] private bool invertHorizontal = false;
    [SerializeField] private bool invertVertical = false;

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
    public float grabRange = 1.2f;
    [SerializeField] private Transform grabOrigin;
    [SerializeField] private float grabMaxAngle = 55f;
    [SerializeField] private LayerMask grabBlockerMask;
    [SerializeField] private bool requireLineOfSight = true;

    [Header("Interaction")]
    public LayerMask interactLayer;

    private IGrabbable grabbedObject;

    private Rigidbody rb;
    private Vector3 moveInput;
    private bool isGrounded;
    private bool jumpQueued;
    private Vector3 lastMoveDirection = Vector3.forward;

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

    void ReadInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        ApplyControlScheme(ref h, ref v);

        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

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

    void ApplyControlScheme(ref float h, ref float v)
    {
        if (invertHorizontal) h = -h;
        if (invertVertical) v = -v;

        if (controlScheme == ControlScheme.SwapWASD)
        {
            // Swap axes: W/S become horizontal, A/D become vertical
            float oldH = h;
            h = v;
            v = oldH;
        }
    }

    void HandleMovement()
    {
        float control = isGrounded ? 1f : airControlPercent;
        float accel = isGrounded ? acceleration : acceleration * airAccelerationMultiplier;

        Vector3 targetVelocity = moveInput * moveSpeed * control;
        Vector3 velocity = rb.linearVelocity;

        Vector3 velocityChange = new Vector3(
            targetVelocity.x - velocity.x,
            0f,
            targetVelocity.z - velocity.z
        );

        velocityChange = Vector3.ClampMagnitude(velocityChange, accel);
        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    void HandleRotation()
    {
        if (lastMoveDirection.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(lastMoveDirection);
        Quaternion smoothRotation = Quaternion.Slerp(
            rb.rotation,
            targetRotation,
            rotationSpeed * Time.fixedDeltaTime
        );

        rb.MoveRotation(smoothRotation);
    }

    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    void OnCollisionStay(Collision collision)
    {
        if (isGrounded) return;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (Vector3.Dot(contact.normal, Vector3.up) < 0.2f)
            {
                Vector3 vel = rb.linearVelocity;
                Vector3 intoWall = Vector3.Project(vel, -contact.normal);
                rb.linearVelocity = vel - intoWall;
            }
        }
    }

    void HandleJump()
    {
        if (!jumpQueued || !isGrounded)
            return;

        jumpQueued = false;

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void HandleInteraction()
    {
        if (Input.GetMouseButton(0))
        {
            if (grabbedObject == null)
                TryGrabObject();
        }
        else
        {
            ReleaseObject();
        }

        MoveGrabbedObject();
    }

    void TryGrabObject()
    {
        Vector3 origin = grabOrigin ? grabOrigin.position : transform.position;

        Collider[] hits = Physics.OverlapSphere(
            origin,
            grabRange,
            interactLayer,
            QueryTriggerInteraction.Ignore
        );

        if (hits == null || hits.Length == 0)
            return;

        Vector3 facing = lastMoveDirection.sqrMagnitude > 0.01f ? lastMoveDirection : transform.forward;
        facing.y = 0f;
        facing.Normalize();

        float cosLimit = Mathf.Cos(grabMaxAngle * Mathf.Deg2Rad);

        float bestScore = float.NegativeInfinity;
        Collider bestCol = null;
        IGrabbable bestGrab = null;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (!c) continue;

            IGrabbable g = c.GetComponentInParent<IGrabbable>();
            if (g == null) continue;

            Vector3 to = c.bounds.center - origin;
            to.y = 0f;

            float dist = to.magnitude;
            if (dist <= 0.0001f) continue;

            Vector3 dir = to / dist;
            float facingDot = Vector3.Dot(facing, dir);

            if (facingDot < cosLimit)
                continue;

            if (requireLineOfSight)
            {
                Vector3 rayFrom = origin;
                Vector3 rayTo = c.bounds.center;
                Vector3 rayDir = rayTo - rayFrom;
                float rayDist = rayDir.magnitude;

                if (rayDist > 0.0001f)
                {
                    rayDir /= rayDist;

                    if (Physics.Raycast(rayFrom, rayDir, out RaycastHit hit, rayDist, grabBlockerMask, QueryTriggerInteraction.Ignore))
                    {
                        var hitGrabbable = hit.collider ? hit.collider.GetComponentInParent<IGrabbable>() : null;
                        if (hitGrabbable != g)
                            continue;
                    }
                }
            }

            float score = (facingDot * 2f) - dist;
            if (score > bestScore)
            {
                bestScore = score;
                bestCol = c;
                bestGrab = g;
            }
        }

        if (bestGrab == null || bestCol == null)
            return;

        grabbedObject = bestGrab;

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
        if (grabbedObject == null)
            return;

        float moveAmount = Vector3.Dot(moveInput, grabDirection);

        Vector3 targetPos = transform.position
                            + grabDirection * grabDistance
                            + grabDirection * moveAmount * 0.5f;

        grabbedObject.MoveTo(targetPos);
    }

    void ReleaseObject()
    {
        if (grabbedObject == null) return;

        grabbedObject.OnRelease();
        grabbedObject = null;
    }

    // Optional: call this from your puzzle manager at runtime when the game swaps inputs
    public void SetControlScheme(ControlScheme scheme) => controlScheme = scheme;
}
