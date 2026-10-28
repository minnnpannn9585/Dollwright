using UnityEngine;

namespace Dollwright.Graybox
{
    // A grounded, bounded crate: contact + walking pushes it, jumping does not.
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class GrayboxPushCrate : MonoBehaviour
    {
        public GrayboxPlayer2D player;
        public float minX, maxX;
        public float pushSpeed = 2.4f;
        [Tooltip("Allow this crate to leave its supporting platform and return after falling.")]
        public bool fallAndRespawn;
        public BoxCollider2D supportPlatform;
        public float respawnDrop = 4f;
        public bool IsFalling { get; private set; }
        public int ResetCount { get; private set; }
        Vector2 spawn;
        float fallingSince;
        public bool IsBeingPushed { get; private set; }
        Rigidbody2D body;
        BoxCollider2D shape;
        float railY;
        readonly RaycastHit2D[] moveHits=new RaycastHit2D[12];
        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<BoxCollider2D>();
            railY = body.position.y;
            spawn = body.position;
        }
        public Vector2 CheckpointPosition => IsFalling ? spawn : GetComponent<Rigidbody2D>().position;
        public void RestoreCheckpoint(Vector2 position)
        {
            if(body==null)body=GetComponent<Rigidbody2D>();
            body.bodyType=RigidbodyType2D.Kinematic;
            body.velocity=Vector2.zero;body.angularVelocity=0;body.rotation=0;
            body.position=position;
            transform.position=new Vector3(position.x,position.y,transform.position.z);
            railY=position.y;IsFalling=false;IsBeingPushed=false;
        }
        void FixedUpdate()
        {
            IsBeingPushed = false;
            if (player != null && player.run.Rewind.IsRewinding) return;
            if (IsFalling)
            {
                if (body.position.y < spawn.y - respawnDrop || Time.time - fallingSince > 3f)
                {
                    // Do not rematerialize a solid box inside the player.
                    var playerShape = player != null ? player.GetComponent<Collider2D>() : null;
                    var spawnBounds = new Bounds(new Vector3(spawn.x, spawn.y, 0), shape.bounds.size + new Vector3(.12f, .12f, 0));
                    if (playerShape == null || !spawnBounds.Intersects(playerShape.bounds))
                    {
                        body.velocity = Vector2.zero;
                        body.angularVelocity = 0;
                        body.bodyType = RigidbodyType2D.Kinematic;
                        body.position = spawn;
                        transform.position = new Vector3(spawn.x, spawn.y, transform.position.z);
                        body.rotation = 0;
                        IsFalling = false;
                        ResetCount++;
                    }
                }
                return;
            }
            if (fallAndRespawn && supportPlatform != null &&
                (body.position.x > supportPlatform.bounds.max.x || body.position.x < supportPlatform.bounds.min.x))
            {
                IsFalling = true;
                fallingSince = Time.time;
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = 2.8f;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.velocity = new Vector2(body.position.x > supportPlatform.bounds.max.x ? pushSpeed : -pushSpeed, 0);
                return;
            }
            if (player == null || !player.IsGrounded || player.run.Completed || player.run.IsRecovering) return;
            float axis = player.MoveIntent;
            float dx = player.Body.position.x - body.position.x;
            float sideReach = shape.bounds.extents.x + 0.325f + 0.13f;
            float playerFeet = player.Body.position.y - 0.7f;
            bool nearSide = Mathf.Abs(dx) <= sideReach && Mathf.Abs(dx) > shape.bounds.extents.x;
            bool onFloor = Mathf.Abs(playerFeet - shape.bounds.min.y) < 0.18f;
            bool towardCrate = (dx < 0 && axis > 0.1f) || (dx > 0 && axis < -0.1f);
            if (!nearSide || !onFloor || !towardCrate) return;
            float nextX = body.position.x + Mathf.Sign(axis) * pushSpeed * Time.fixedDeltaTime;
            if (!fallAndRespawn) nextX = Mathf.Clamp(nextX, minX, maxX);
            float distance=Mathf.Abs(nextX-body.position.x);
            float direction=Mathf.Sign(nextX-body.position.x);
            int blockers=shape.Cast(new Vector2(direction,0),new ContactFilter2D { useTriggers=false },moveHits,distance);
            for(int i=0;i<blockers;i++)
            {
                var hit=moveHits[i];
                if(hit.collider==null||hit.collider.GetComponent<GrayboxPlayer2D>()!=null)continue;
                if(hit.normal.x*direction<-.5f)distance=Mathf.Min(distance,Mathf.Max(0,hit.distance-.002f));
            }
            nextX=body.position.x+direction*distance;
            IsBeingPushed = Mathf.Abs(nextX - body.position.x) > 0.0001f;
            body.MovePosition(new Vector2(nextX, railY));
        }
    }
}
