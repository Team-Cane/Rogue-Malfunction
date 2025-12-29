using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class SimpleCharacterController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpForce = 6f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private Vector3 velocity;

    private InputAction moveAction;
    private InputAction jumpAction;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");

        jumpAction = new InputAction("Jump", InputActionType.Button, "<Keyboard>/space");
    }

    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
    }

    void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        controller.Move(move * moveSpeed * Time.deltaTime);

        if (controller.isGrounded)
        {
            if (velocity.y < 0f)
                velocity.y = -2f;

            if (jumpAction.WasPressedThisFrame())
                velocity.y = jumpForce;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
