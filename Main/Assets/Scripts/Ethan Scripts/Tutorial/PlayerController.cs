using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class IsometricPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float acceleration = 15f;
    public float rotationSpeed = 12f;

    [Header("Air Control")]
    [Range(0f, 1f)] public float airControlPercent = 0.35f;
    public float airAccelerationMultiplier = 0.5f;

    [Header("Jump")]
    public float jumpForce = 6f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;

    [Header("Grab")]
    public Transform holdPoint;
    public float grabRange = 1.2f;

    [Header("Interaction")]
    public LayerMask interactLayer;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 smoothMoveInput;
    private Vector3 lastMoveDirection = Vector3.forward;
    private Vector3 grabOffsetLocal;

    private bool isGrounded;
    private bool jumpQueued;

    private IGrabbable grabbedObject;

    public float inputSmoothing = 10f;

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

        // ❌ no jump buffering, no stacking
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !jumpQueued)
        {
            jumpQueued = true;
        }
    }

    void FixedUpdate()
    {
        CheckGround();
        HandleMovement();
        HandleWallSliding(); // add this
        HandleRotation();
        HandleJump();
    }
    void LateUpdate()
    {
        if (grabbedObject == null)
            return;

        // Hold point stays in front of player
        holdPoint.position =
        transform.position + transform.rotation * grabOffsetLocal;
    }

    void HandleWallSliding()
    {
        if (isGrounded) return;

        RaycastHit hit;
        float radius = 0.3f; // adjust to player collider
        Vector3 moveDir = rb.linearVelocity.normalized;
        float distance = 0.5f;

        if (Physics.SphereCast(transform.position, radius, moveDir, out hit, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (Vector3.Dot(hit.normal, Vector3.up) < 0.2f)
            {
                Vector3 vel = rb.linearVelocity;
                Vector3 intoWall = Vector3.Project(vel, -hit.normal);
                rb.linearVelocity = vel - intoWall;
            }
        }
    }

    // ---------------- INPUT ----------------

    void ReadInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        PlayerControlRandomizer randomizer = GetComponent<PlayerControlRandomizer>();
        if (randomizer != null)
        {
            Vector2 modified = randomizer.ProcessMovementInput(h, v);
            h = modified.x;
            v = modified.y;
        }

        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 input = (forward * v + right * h).normalized;

        smoothMoveInput = Vector3.Lerp(
            smoothMoveInput,
            input,
            inputSmoothing * Time.deltaTime
        );

        if (smoothMoveInput.sqrMagnitude > 0.01f)
        {
            moveInput = smoothMoveInput;
            lastMoveDirection = smoothMoveInput;
        }
        else
        {
            moveInput = Vector3.zero;
        }
    }

    // ---------------- MOVEMENT ----------------

    void HandleMovement()
    {
        float control = isGrounded ? 1f : airControlPercent;
        float accel = isGrounded ? acceleration : acceleration * airAccelerationMultiplier;

        Vector3 targetVelocity = moveInput * moveSpeed * control;
        Vector3 velocity = rb.linearVelocity;

        // Calculate desired change
        Vector3 velocityChange = new Vector3(
            targetVelocity.x - velocity.x,
            0f,
            targetVelocity.z - velocity.z
        );

        velocityChange = Vector3.ClampMagnitude(velocityChange, accel);

        // --- Wall anti-stick ---
        RaycastHit hit;
        float radius = 0.3f; // player collider radius approximation
        float distance = 0.5f;

        if (Physics.SphereCast(transform.position, radius, velocityChange.normalized, out hit, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            if (Vector3.Dot(hit.normal, Vector3.up) < 0.2f)
            {
                // Project movement along wall plane
                velocityChange = Vector3.ProjectOnPlane(velocityChange, hit.normal);
            }
        }

        rb.AddForce(velocityChange, ForceMode.VelocityChange);
    }

    // ---------------- ROTATION ----------------

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

    // ---------------- GROUND CHECK ----------------

    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    // ---------------- JUMP ----------------

    void HandleJump()
    {
        if (!jumpQueued || !isGrounded)
            return;

        jumpQueued = false;

        // Cancel grab if jumping
        if (grabbedObject != null)
        {
            grabbedObject.OnRelease();
            grabbedObject = null;
        }

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    // ---------------- INTERACTION ----------------

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
    }

    void TryGrabObject()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            grabRange,
            interactLayer,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            IGrabbable grabbable = hit.GetComponent<IGrabbable>();
            if (grabbable == null) continue;

            // No height gating — allows grabbing while touching
            // Store initial grab offset in player-local space
            Vector3 worldOffset = hit.transform.position - transform.position;
            worldOffset.y = 0f;

            grabOffsetLocal = Quaternion.Inverse(transform.rotation) * worldOffset.normalized * 0.9f;

            grabbable.OnGrab(holdPoint);
            grabbedObject = grabbable;
            break;
        }
    }

    void ReleaseObject()
    {
        if (grabbedObject == null) return;

        grabbedObject.OnRelease();
        grabbedObject = null;
        grabOffsetLocal = Vector3.zero;
    }

    // ---------------- WALL ANTI-STICK ----------------
    void OnCollisionStay(Collision collision)
    {
        if (isGrounded) return; // only apply in air

        foreach (ContactPoint contact in collision.contacts)
        {
            // Only consider mostly vertical surfaces
            if (Vector3.Dot(contact.normal, Vector3.up) < 0.2f)
            {
                Vector3 vel = rb.linearVelocity;

                // Remove velocity going into the wall
                Vector3 intoWall = Vector3.Project(vel, -contact.normal);
                rb.linearVelocity = vel - intoWall;
            }
        }
    }
}
