using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AriansPlayerController : MonoBehaviour
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

    [Header("Input Smoothing")]
    public float inputSmoothing = 10f;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Vector3 smoothMoveInput;
    private Vector3 lastMoveDirection = Vector3.forward;

    private bool isGrounded;
    private bool jumpQueued;

    private IGrabbable grabbedObject;
    private Vector3 grabDirection;
    private float grabDistance;

    // NEW: for freezing input during door puzzles, etc.
    private bool inputLocked = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Update()
    {
        if (inputLocked)
        {
            // When locked: no movement, no interaction, no jump queue
            moveInput = Vector3.zero;
            smoothMoveInput = Vector3.zero;
            jumpQueued = false;
            return;
        }

        ReadInput();
        HandleInteraction();

        // jump queue (no buffering / stacking)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !jumpQueued)
        {
            jumpQueued = true;
        }
    }

    void FixedUpdate()
    {
        CheckGround();
        HandleMovement();
        HandleWallSliding();
        HandleRotation();
        HandleJump();
    }

    // ---------------- INPUT ----------------

    void ReadInput()
    {
        float h = 0f;
        float v = 0f;

        // If we have a ControlRewireManager, use its mapping (WASD remap)
        if (ControlRewireManager.Instance != null)
        {
            KeyCode upKey = ControlRewireManager.Instance.UpKey;
            KeyCode downKey = ControlRewireManager.Instance.DownKey;
            KeyCode leftKey = ControlRewireManager.Instance.LeftKey;
            KeyCode rightKey = ControlRewireManager.Instance.RightKey;

            if (Input.GetKey(rightKey)) h += 1f;
            if (Input.GetKey(leftKey)) h -= 1f;
            if (Input.GetKey(upKey)) v += 1f;
            if (Input.GetKey(downKey)) v -= 1f;
        }
        else
        {
            // Fallback: default Unity axes if no manager is present
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
        }

        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 input = (forward * v + right * h).normalized;

        // Smooth towards the new input direction
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

        if (velocityChange.sqrMagnitude > 0.0001f &&
            Physics.SphereCast(transform.position, radius, velocityChange.normalized, out hit, distance, ~0, QueryTriggerInteraction.Ignore))
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
        if (groundCheck == null)
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

    // ---------------- JUMP ----------------

    void HandleJump()
    {
        if (!jumpQueued || !isGrounded)
            return;

        jumpQueued = false;

        // Cancel grab if jumping
        if (grabbedObject != null && !isGrounded)
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
        if (inputLocked)
            return;

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
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            grabRange,
            interactLayer
        );

        foreach (Collider hit in hits)
        {
            IGrabbable grabbable = hit.GetComponent<IGrabbable>();
            if (grabbable == null) continue;

            // --- side-only check ---
            float verticalOffset = Mathf.Abs(hit.transform.position.y - transform.position.y);
            float maxGrabHeight = 0.5f; // adjust for your character height
            if (verticalOffset > maxGrabHeight)
                continue; // too high or low, skip this object

            // Determine world-space grab axis (X or Z)
            Vector3 toObject = hit.transform.position - transform.position;
            toObject.y = 0f;

            if (Mathf.Abs(toObject.x) > Mathf.Abs(toObject.z))
                grabDirection = new Vector3(Mathf.Sign(toObject.x), 0f, 0f);
            else
                grabDirection = new Vector3(0f, 0f, Mathf.Sign(toObject.z));

            grabDistance = Mathf.Abs(Vector3.Dot(toObject, grabDirection));

            grabbable.OnGrab(holdPoint);
            grabbedObject = grabbable;
            break;
        }
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

    // ---------------- WALL SLIDING / ANTI-STICK ----------------

    void HandleWallSliding()
    {
        if (isGrounded) return;

        RaycastHit hit;
        float radius = 0.3f; // adjust to player collider
        Vector3 moveDir = rb.linearVelocity.normalized;
        float distance = 0.5f;

        if (moveDir.sqrMagnitude < 0.0001f)
            return;

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

    // ---------------- EXTERNAL CONTROL ----------------

    /// <summary>
    /// Called by your door/puzzle system to freeze/unfreeze player movement & interaction.
    /// </summary>
    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;

        if (locked)
        {
            moveInput = Vector3.zero;
            smoothMoveInput = Vector3.zero;
            jumpQueued = false;
        }
    }
}
