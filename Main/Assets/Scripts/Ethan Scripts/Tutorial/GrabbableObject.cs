using UnityEngine;

namespace Ethan.Tutorial
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class GrabbableObject : MonoBehaviour, IGrabbable
    {
        [Header("Grab Follow")]
        public float followSpeed = 10f;

        [Header("Block Detection")]
        public float blockReleaseDot = 0.6f;

        [Header("Button Compatibility")]
        public LayerMask buttonLayer;

        private Rigidbody rb;
        private Collider objectCollider;
        private Collider playerCollider;
        private Transform playerTransform;

        private bool isGrabbed;
        private Vector3 grabOffsetWorld;
        private int originalLayer;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            objectCollider = GetComponent<Collider>();

            rb.useGravity = true;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            originalLayer = gameObject.layer;
        }

        public void OnGrab(Transform anchor)
        {
            Collider pc = anchor.GetComponentInParent<Collider>();
            if (pc == null)
                return;

            if (IsPlayerStandingOnObject(pc))
                return;

            isGrabbed = true;
            playerCollider = pc;
            playerTransform = pc.transform;

            grabOffsetWorld = rb.position - playerTransform.position;
            grabOffsetWorld.y = 0f;

            originalLayer = gameObject.layer;

            int heldLayer = LayerMask.NameToLayer("HeldObject");
            if (heldLayer != -1)
                gameObject.layer = heldLayer;
        }

        public void OnRelease()
        {
            isGrabbed = false;
            playerCollider = null;
            playerTransform = null;

            gameObject.layer = originalLayer;
        }

        void FixedUpdate()
        {
            if (!isGrabbed || playerTransform == null)
                return;

            PlayerGroundedChecker groundedChecker = playerTransform.GetComponent<PlayerGroundedChecker>();
            bool playerGrounded = groundedChecker != null && groundedChecker.isGrounded;

            if (!playerGrounded)
                return;

            Vector3 targetXZ = playerTransform.position + grabOffsetWorld;

            if ((targetXZ - rb.position).sqrMagnitude < 0.001f)
                return;

            Vector3 current = rb.position;
            Vector3 desired = new Vector3(targetXZ.x, current.y, targetXZ.z);

            if (IsBlocked(current, desired))
            {
                OnRelease();
                return;
            }

            rb.MovePosition(Vector3.MoveTowards(current, desired, followSpeed * Time.fixedDeltaTime));
        }

        private bool IsBlocked(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            float distance = dir.magnitude;
            if (distance < 0.001f) return false;

            dir.Normalize();

            Vector3 halfExtents = objectCollider.bounds.extents;
            halfExtents.y = 0.1f;

            if (Physics.BoxCast(from, halfExtents, dir, out RaycastHit hit, Quaternion.identity, distance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != playerCollider)
                    return true;
            }

            return false;
        }

        private bool IsPlayerStandingOnObject(Collider playerCol)
        {
            return playerCol.bounds.min.y > objectCollider.bounds.max.y - 0.05f;
        }
    }
}
