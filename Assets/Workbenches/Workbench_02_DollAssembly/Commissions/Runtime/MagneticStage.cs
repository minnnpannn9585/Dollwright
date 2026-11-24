using UnityEngine;

namespace Dollwright.RetrievalDemo
{
    public sealed class MagneticStage : MonoBehaviour
    {
        public const string SceneName="Route08_MagneticDrop";
        public BoxCollider2D[] rails;
        public BoxCollider2D panel;
        public Transform retryPoint;
        public bool Riding => rail;
        public bool Slamming {get; private set;}
        public bool PanelOpen => !panel.gameObject.activeSelf;
        public bool AtCheckpoint {get; private set;}
        BoxCollider2D rail;
        Renderer head;
        PaperRoute route;
        bool hasBoots;
        float catchWindow;
        public void Bind(PaperRoute owner)
        {
            route=owner;
            head=owner.parts[FreeAssembly.Magnet].GetComponent<Renderer>();
            hasBoots=owner.parts[FreeAssembly.Boots].gameObject.activeSelf;
        }
        public void ResetStage()
        {
            rail=null;Slamming=false;catchWindow=0;
            panel.gameObject.SetActive(true);
        }
        public bool Step(float axis,bool grab,bool release,bool stomp,bool grounded)
        {
            var body=route.body; var at=body.position;
            if(!AtCheckpoint && grounded && Mathf.Abs(at.x-retryPoint.position.x)<1 && Mathf.Abs(at.y-retryPoint.position.y)<.35f) AtCheckpoint=true;
            if(grab)catchWindow=.14f;
            if(release)catchWindow=0;
            if(!rail && !Slamming && head.gameObject.activeInHierarchy && catchWindow>0)
            {
                catchWindow-=Time.fixedDeltaTime;
                foreach(var candidate in rails)
                {
                    var b=candidate.bounds;
                    float gap=b.min.y-head.bounds.max.y;
                    if(gap<-.45f || gap>.82f || head.bounds.center.x<b.min.x-.08f || head.bounds.center.x>b.max.x+.08f)continue;
                    rail=candidate;catchWindow=0;break;
                }
            }
            if(stomp && hasBoots && (rail || !grounded)) {rail=null;Slamming=true;catchWindow=0;}
            if(rail && release) {rail=null;body.gravityScale=2;body.velocity=Vector2.zero;return true;}
            if(rail)
            {
                var b=rail.bounds;float x=head.bounds.center.x;
                float dx=Mathf.Clamp(x+axis*3.65f*Time.fixedDeltaTime,b.min.x,b.max.x)-x;
                float dy=b.min.y-head.bounds.max.y-.025f;
                body.gravityScale=0;body.velocity=new Vector2(dx/Time.fixedDeltaTime,Mathf.Clamp(dy/Time.fixedDeltaTime,-5.5f,5.5f));
                return true;
            }
            if(!Slamming)return false;
            body.gravityScale=2;
            var hit=Physics2D.BoxCast(at+Vector2.down*.575f,new Vector2(.51f,.035f),0,Vector2.down,14*Time.fixedDeltaTime+.055f,1<<8);
            if(hit.collider==panel && hit.normal.y>.5f && Mathf.Abs(at.x-panel.bounds.center.x)<panel.bounds.extents.x-.22f)
                panel.gameObject.SetActive(false);
            else if(hit.collider){Slamming=false;body.velocity=Vector2.zero;return true;}
            body.velocity=Vector2.down*14;
            return true;
        }
        public void DrawGuide()
        {
            route.thread.enabled=Riding;
            if(!Riding)return;
            var foot=route.body.position+Vector2.down*.6f;
            var hit=Physics2D.Raycast(foot,Vector2.down,12,1<<8);
            if(!hit){route.thread.enabled=false;return;}
            route.thread.SetPosition(0,new Vector3(foot.x,foot.y,-.58f));
            route.thread.SetPosition(1,new Vector3(hit.point.x,hit.point.y,-.58f));
        }
    }
}
