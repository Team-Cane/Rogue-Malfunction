using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerRespawn : MonoBehaviour
{
    [SerializeField] private Transform startingCheckpoint;
    [SerializeField] private bool resetRotation = true;

    private Rigidbody rb;
    private Vector3 checkpointPos;
    private Quaternion checkpointRot;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        if (startingCheckpoint != null)
            SetCheckpoint(startingCheckpoint);
        else
            SetCheckpoint(transform);
    }

    public void SetCheckpoint(Transform checkpoint)
    {
        checkpointPos = checkpoint.position;
        checkpointRot = checkpoint.rotation;
    }

    public void Respawn()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.position = checkpointPos;

        if (resetRotation)
            rb.rotation = checkpointRot;

        rb.Sleep();
        rb.WakeUp();
    }
}
