using UnityEngine;
namespace Dollwright.Graybox
{
    public sealed class DreamPressurePlate : MonoBehaviour
    {
        public float halfWidth=.65f;
        public bool IsPressed { get; private set; }
        public DreamSawGate saw;
        GrayboxRun run;
        SpriteRenderer visual;
        void Awake(){run=FindObjectOfType<GrayboxRun>();visual=GetComponent<SpriteRenderer>();}
        void FixedUpdate(){if(run==null||!run.Rewind.IsRewinding)Evaluate();}
        public void Evaluate()
        {
            bool pressed=false;
            foreach(var crate in FindObjectsOfType<GrayboxPushCrate>())
            {
                if(crate.IsFalling)continue;
                Vector2 p=crate.GetComponent<Rigidbody2D>().position;
                float bottom=p.y-crate.transform.lossyScale.y*.5f;
                if(Mathf.Abs(p.x-transform.position.x)<halfWidth+.15f && Mathf.Abs(bottom-transform.position.y)<.16f){pressed=true;break;}
            }
            if(!pressed && run!=null && run.player!=null )
            {
                var p=run.player.Body.position;
                pressed=Mathf.Abs(p.x-transform.position.x)<halfWidth+.22f && Mathf.Abs(p.y-.7f-transform.position.y)<.16f;
            }
            IsPressed=pressed;
            if(visual!=null)visual.color=pressed?new Color(.25f,.85f,.58f):new Color(.30f,.55f,.47f);
        }
    }
}
