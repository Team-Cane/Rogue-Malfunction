using UnityEngine;

public class PlayerGroundedChecker : MonoBehaviour
{
    public bool isGrounded;
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;

    void FixedUpdate()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
    }
}
