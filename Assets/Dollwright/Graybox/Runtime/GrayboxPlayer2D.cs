using UnityEngine;

namespace Dollwright.Graybox
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
    public sealed class GrayboxPlayer2D : MonoBehaviour
    {
        [Min(0.1f)] public float moveSpeed = 4.2f;
        [Min(0.1f)] public float jumpSpeed = 11.5f;
        public float coyoteTime = 0.1f;
        public float jumpBuffer = 0.12f;
        public float maxFallSpeed = 18f;
        public GrayboxRun run;
        public bool IsGrounded { get; private set; }
        public int JumpCount { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public bool InputOverride { get; private set; }
        CapsuleCollider2D capsule;
        readonly RaycastHit2D[] hits = new RaycastHit2D[12];
        ContactFilter2D solidFilter;
        float horizontal, bufferRemaining, groundGrace;
        bool wasGrounded;

        void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            capsule = GetComponent<CapsuleCollider2D>();
            solidFilter = new ContactFilter2D { useTriggers = false };
        }

        void Update()
        {
            if (!InputOverride)
            {
                horizontal = Input.GetAxisRaw("Horizontal");
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W)
                    || Input.GetKeyDown(KeyCode.UpArrow)) QueueJump();
                if ((Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W)
                    || Input.GetKeyUp(KeyCode.UpArrow)) && Body.velocity.y > 4f)
                    Body.velocity = new Vector2(Body.velocity.x, Body.velocity.y * 0.55f);
            }
            if (transform.position.y < -5f && run != null) run.Respawn();
        }

        void FixedUpdate()
        {
            IsGrounded = false;
            int count = capsule.Cast(Vector2.down, solidFilter, hits, 0.07f);
            for (int i = 0; i < count; i++)
                if (hits[i].collider != null && hits[i].normal.y > 0.65f) IsGrounded = true;
            if (Body.velocity.y > 0.1f) IsGrounded = false;
            if (IsGrounded && !wasGrounded) groundGrace = coyoteTime;
            else if (!IsGrounded) groundGrace -= Time.fixedDeltaTime;
            if (IsGrounded) groundGrace = coyoteTime;
            wasGrounded = IsGrounded;
            bufferRemaining -= Time.fixedDeltaTime;
            if (run != null && run.Completed)
            {
                Body.velocity = new Vector2(0, Body.velocity.y);
                return;
            }
            var velocity = Body.velocity;
            velocity.x = horizontal * moveSpeed;
            velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
            if (bufferRemaining > 0 && groundGrace > 0)
            {
                velocity.y = jumpSpeed;
                bufferRemaining = 0;
                groundGrace = 0;
                IsGrounded = false;
                JumpCount++;
            }
            Body.velocity = velocity;
        }

        public void SetInputOverride(float axis, bool jump = false)
        {
            InputOverride = true;
            horizontal = Mathf.Clamp(axis, -1f, 1f);
            if (jump) QueueJump();
        }
        public void ClearInputOverride() { InputOverride = false; horizontal = 0; }
        void QueueJump() { bufferRemaining = jumpBuffer; }

        public void Teleport(Vector2 position)
        {
            // Update the visible transform immediately as well as the physics body.
            // Interpolation otherwise leaves Update reading the previous falling position.
            transform.position = new Vector3(position.x, position.y, transform.position.z);
            Body.position = position;
            Body.velocity = Vector2.zero;
            bufferRemaining = 0;
            groundGrace = 0;
            wasGrounded = false;
            Physics2D.SyncTransforms();
        }
    }
}
