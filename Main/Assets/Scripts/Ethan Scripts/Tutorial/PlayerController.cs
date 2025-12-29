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

    private bool isGrounded;
    private bool jumpQueued;

    private IGrabbable grabbedObject;
    private Vector3 grabDirection;
    private float grabDistance;

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
        HandleRotation();
        HandleJump();
    }

    // ---------------- INPUT ----------------

    void ReadInput()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

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

        Vector3 velocityChange = new Vector3(
            targetVelocity.x - velocity.x,
            0f,
            targetVelocity.z - velocity.z
        );

        velocityChange = Vector3.ClampMagnitude(velocityChange, accel);
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

    // ---------------- WALL ANTI-STICK ----------------

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
}
