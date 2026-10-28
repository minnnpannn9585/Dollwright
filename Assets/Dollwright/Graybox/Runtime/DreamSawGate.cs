using UnityEngine;
namespace Dollwright.Graybox
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class DreamSawGate : MonoBehaviour
    {
        public DreamPressurePlate plate;
        public Transform blade;
        public float liftDistance=3.5f;
        public bool IsOpen { get; private set; }
        BoxCollider2D blocker;
        GrayboxRun run;
        Vector3 closedBlade;
        void Awake(){blocker=GetComponent<BoxCollider2D>();run=FindObjectOfType<GrayboxRun>();if(blade!=null)closedBlade=blade.position;}
        void FixedUpdate()
        {
            if(run!=null&&run.Rewind.IsRewinding)return;
            Apply(plate!=null&&plate.IsPressed,false);
        }
        void Apply(bool open,bool snap)
        {
            // Opening retracts the whole saw carriage. Closing recovers any player occupying its sweep.
            bool closing=IsOpen&&!open;
            if(closing && run!=null && blocker.bounds.Intersects(run.player.GetComponent<Collider2D>().bounds))run.Rewind.TryRewind();
            IsOpen=open;blocker.isTrigger=open;
            var sr=GetComponent<SpriteRenderer>();if(sr!=null)sr.color=open?new Color(.85f,.9f,.86f,.25f):new Color(.88f,.40f,.42f,.75f);
            if(blade!=null)
            {
                var dest=closedBlade+Vector3.up*(open?liftDistance:0);
                blade.position=snap?dest:Vector3.MoveTowards(blade.position,dest,Time.fixedDeltaTime*18);
                if(!open)blade.Rotate(0,0,-200*Time.fixedDeltaTime,Space.World);
            }
        }
        public void SnapToPlate(){Apply(plate!=null&&plate.IsPressed,true);}
        void OnCollisionEnter2D(Collision2D hit)
        {
            if(!IsOpen&&hit.collider.GetComponent<GrayboxPlayer2D>()!=null)run.Rewind.TryRewind();
        }
    }
}
