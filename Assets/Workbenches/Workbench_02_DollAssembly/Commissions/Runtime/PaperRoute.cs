using UnityEngine;

namespace Dollwright.RetrievalDemo
{
    public sealed class PaperRoute : MonoBehaviour
    {
        public const string SwingScene = "Route03_SwingUpdraft", ForkScene = "Route12_ForkedPassage";
        public bool fork;
        public MagneticStage magnetic;
        JumpWindow jumpWindow;
        float hookQueue,dashFacing;
        bool stompPressed;
        public Transform refitBlock;
        public static bool Refitting {get;set;}
        public bool HasWing {get;private set;}
        public bool HasPleats {get;private set;}
        bool changingScene;
        bool NearBench => fork && refitBlock && Vector2.Distance(body.position,refitBlock.position)<1.65f;
        public bool UseRefitBlock()
        {
            if(!NearBench || Finished || changingScene) return false;
            changingScene=true; Refitting=true;
            WindmillCommission.RememberSample(parts);
            WindmillCommission.BackToBench();return true;
        }
        public bool ContinueToFork()
        {
            if(magnetic || fork || !Finished || changingScene) return false;
            changingScene=true;
            WindmillCommission.RememberSample(parts);
            UnityEngine.SceneManagement.SceneManager.LoadScene(ForkScene);return true;
        }
        public Transform startPoint, prize, visual, fabric, wind, doll;
        public Transform[] rings, parts;
        public LineRenderer thread;
        public Rigidbody2D body;
        public BoxCollider2D hull;
        public bool HasHook { get; private set; }
        public bool HasRunner { get; private set; }
        public bool Finished { get; private set; }
        public Transform Attached { get; private set; }
        public bool Folded { get; private set; }
        public float SwingAngle { get; private set; }
        Camera view;
        bool jump, grip, dash, charged, released;
        float phase, length, carry, dashTime, facing = 1;
        Vector2 tangent;
        Transform previousRing;
        float lastRelease;
        const int Ground = 1 << 8;

        void Start()
        {
            view = Camera.main;
            parts=RouteParts.Extend(parts,visual);
            WindmillCommission.ApplyParts(parts);
            HasHook = !WindmillCommission.HasAssembly || WindmillCommission.Equipped(FreeAssembly.Hook);
            HasWing=!WindmillCommission.HasAssembly || WindmillCommission.Equipped(FreeAssembly.Wing);
            HasPleats=WindmillCommission.Equipped(FreeAssembly.Pleats);
            HasRunner = WindmillCommission.Equipped(FreeAssembly.Runners);
            if (!WindmillCommission.HasAssembly) ShowSample();
            if(magnetic)magnetic.Bind(this);
            ResetRun();
        }
        void ShowSample()
        {
            if(magnetic)
            {
                HasHook=HasRunner=HasWing=HasPleats=false;
                foreach(var part in parts)part.gameObject.SetActive(false);
                Sample(1,true,new Vector3(-.402f,.06f,0),new Vector3(.16f,.30f,.18f));
                Sample(FreeAssembly.Magnet,true,new Vector3(0,.537f,0),new Vector3(.39f,.35f,.29f));
                Sample(FreeAssembly.Boots,true,new Vector3(0,-.482f,0),new Vector3(.49f,.24f,.32f));
                return;
            }
            HasPleats=HasRunner; HasWing=!fork;
            for(int i=1;i<parts.Length;i++)parts[i].gameObject.SetActive(false);
            parts[0].localPosition=new Vector3(0,.562f,0);parts[5].localPosition=new Vector3(0,-.602f,0);
            parts[1].localPosition=new Vector3(-.402f,.06f,0);
            parts[1].gameObject.SetActive(!HasHook);parts[5].gameObject.SetActive(!HasRunner);
            Sample(FreeAssembly.Hook,HasHook,new Vector3(.432f,.06f,0),new Vector3(.22f,.34f,.20f));
            Sample(FreeAssembly.Wing,HasWing,new Vector3(0,0,.222f),new Vector3(.78f,.25f,.14f));
            Sample(FreeAssembly.Runners,HasRunner,new Vector3(0,-.472f,0),new Vector3(.47f,.22f,.30f));
            Sample(FreeAssembly.Pleats,HasPleats,new Vector3(0,0,.212f),new Vector3(.5f,.26f,.12f));
        }
        void Sample(int index,bool active,Vector3 pos,Vector3 size)
        {
            parts[index].localPosition=pos;parts[index].localScale=size;parts[index].gameObject.SetActive(active);
        }
        public void ResetRun()
        {
            Finished = jump = grip = dash = released = false;
            Attached = previousRing = null;
            thread.enabled = false;
            charged = true;
            dashTime = carry = 0;
            body.simulated = true;
            jumpWindow=default;hookQueue=0;facing=1;stompPressed=false;
            if(magnetic)magnetic.ResetStage();
            var spawn=magnetic && magnetic.AtCheckpoint?magnetic.retryPoint:startPoint;
            body.gravityScale=2;body.position=spawn.position;doll.position=spawn.position;
            body.velocity = Vector2.zero;
            SetFold(false);
            fabric.gameObject.SetActive(false);
            prize.gameObject.SetActive(true);
            Physics2D.SyncTransforms();
        }
        void Update()
        {
            if(changingScene)return;
            if(!magnetic && !fork && Finished){ContinueToFork();return;}
            if(Input.GetKeyDown(KeyCode.R) && UseRefitBlock())return;
            if (Input.GetKeyDown(KeyCode.Escape) || (Finished && Input.GetKeyDown(KeyCode.Return))) { WindmillCommission.BackToBench(); return; }
            if (Input.GetKeyDown(KeyCode.Z)) ResetRun();
            if (fork && !WindmillCommission.HasAssembly && Input.GetKeyDown(KeyCode.Tab) && Vector2.Distance(body.position,startPoint.position)<1)
            { HasHook=!HasHook; HasRunner=!HasHook; ShowSample(); }
            stompPressed |= Input.GetKeyDown(KeyCode.F);
            jump |= Input.GetKeyDown(KeyCode.Space);
            grip |= Input.GetKeyDown(KeyCode.E);
            released |= Input.GetKeyUp(KeyCode.E);
            dash |= Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        }
        void FixedUpdate()
        {
            Step(Input.GetAxisRaw("Horizontal"),jump,grip || (HasHook && !magnetic && Input.GetKey(KeyCode.E)),released,dash,Input.GetKey(KeyCode.F),stompPressed);
            stompPressed=false;
            jump = grip = released = dash = false;
        }
        public void Step(float axis,bool hop,bool catchRing,bool letGo,bool burst,bool cloth,bool stomp=false)
        {
            if (Finished) return;
            var position=body.position;
            if (position.y < -3.2f) { ResetRun(); return; }
            if (Mathf.Abs(axis)>.1f) facing=Mathf.Sign(axis);
            bool grounded=Physics2D.BoxCast(position+Vector2.down*.56f,new Vector2(.45f,.025f),0,Vector2.down,.09f,Ground);
            if (grounded && dashTime<=0) charged=true;
            if(magnetic && magnetic.Step(axis,catchRing,letGo,stomp,grounded)){jumpWindow=default;return;}
            hookQueue=catchRing?.14f:Mathf.Max(0,hookQueue-Time.fixedDeltaTime);
            if(letGo)hookQueue=0;
            if (HasHook && !magnetic && hookQueue>0 && !Attached)
            {
                float best=4.35f;
                foreach (var ring in rings)
                {
                    Vector2 offset=(Vector2)ring.position-position;
                    if (offset.y<.15f || (ring==previousRing && Time.fixedTime-lastRelease<.35f)) continue;
                    float dist=offset.magnitude;
                    if (dist<=best) { best=dist; Attached=ring; }
                }
                if (Attached)
                {
                    Vector2 from=position-(Vector2)Attached.position;
                    length=from.magnitude;
                    phase=Mathf.Asin(Mathf.Clamp(Mathf.Atan2(from.x,-from.y)/1.38f,-.98f,.98f));
                    hookQueue=0;jumpWindow=default;body.gravityScale=0; dashTime=0; SetFold(false);
                }
            }
            if (Attached)
            {
                phase+=1.68f*Time.fixedDeltaTime;
                SwingAngle=1.38f*Mathf.Sin(phase);
                var radial=new Vector2(Mathf.Sin(SwingAngle),-Mathf.Cos(SwingAngle));
                tangent=new Vector2(-radial.y,radial.x)*(length*1.38f*1.68f*Mathf.Cos(phase));
                body.velocity=((Vector2)Attached.position + length*radial-position)/Time.fixedDeltaTime;
                if (!letGo) return;
                previousRing=Attached; Attached=null; lastRelease=Time.fixedTime;
                body.velocity=tangent; carry=.42f;
            }
            var speed=body.velocity;
            if (fork && HasPleats && cloth && grounded && dashTime<=0) SetFold(true);
            else if (Folded && !cloth && !Physics2D.OverlapBox(position+Vector2.up*.27f,new Vector2(.53f,.61f),0,Ground)) SetFold(false);
            if (HasRunner && !Folded && charged && burst) { dashFacing=facing;dashTime=.35f; charged=false; }
            body.gravityScale=2;
            if (dashTime>0)
            {
                dashTime-=Time.fixedDeltaTime; body.gravityScale=0; speed=new Vector2(dashFacing*13.2f,0);
            }
            else
            {
                float target=axis*(Folded ? 1.95f : 3.8f);
                if (carry>0){ carry-=Time.fixedDeltaTime; speed.x=Mathf.MoveTowards(speed.x,target,5*Time.fixedDeltaTime); }
                else speed.x=target;
                if (!Folded && jumpWindow.Consume(hop,grounded,speed.y)) speed.y=7.2f;
                bool sail=HasWing && cloth && !Folded;
                fabric.gameObject.SetActive(sail);
                if(sail && speed.y < -2.4f) speed.y=-2.4f;
                if (sail && wind && Mathf.Abs(position.x-wind.position.x)<wind.localScale.x*.5f && Mathf.Abs(position.y-wind.position.y)<wind.localScale.y*.5f)
                { carry=0; speed.x=target; speed.y=4.4f; }
            }
            body.velocity=speed;
            if (Vector2.Distance(position,prize.position)<.85f)
            { Finished=true; body.velocity=Vector2.zero; body.simulated=false; prize.gameObject.SetActive(false); }
        }
        void SetFold(bool small)
        {
            Folded=small;
            float height=small?.57f:1.2f;
            hull.size=new Vector2(.55f,height); hull.offset=new Vector2(0,(height-1.2f)*.5f);
            visual.localScale=new Vector3(1,small?.46f:1,1);
            visual.localPosition=new Vector3(0,small?-.31f:0,0);
        }
        void LateUpdate()
        {
            if (!view) return;
            thread.enabled=Attached;
            if (Attached) {thread.SetPosition(0,doll.position);thread.SetPosition(1,Attached.position);}
            if(magnetic)magnetic.DrawGuide();
            view.orthographicSize=Mathf.Max(4.9f,6f/view.aspect);
            view.transform.position=new Vector3(Mathf.Clamp(body.position.x+1.6f,startPoint.position.x+1,prize.position.x-1),magnetic?1.2f:2.8f,-12);
        }
        void OnGUI()
        {
            string title=magnetic?"08 · 找回发条钥匙   认准锯齿裂纹板":fork?"12 · 取回纸星   上层双环 / 下层暗道":"03 · 取回风铃   松钩入风，张帆登台";
            string controls=Finished?"已取回委托物   Enter 回工作台 · Z 重试":
                magnetic?(magnetic.Riding?"沿轨移动 · 松 E 落下 / F 垂直下砸":"跳近磁轨按住 E 吸附 · 空中按 F 下砸"):(HasHook?"按住 E 抓环，松开 E 抛出   ":"") + (HasRunner?"Shift 冲刺   ":"")+(HasPleats?"按住 F 折身   ":"")+(HasWing?"空中按住 F 张翼":"");
            GUILayout.BeginArea(new Rect(12,12,Mathf.Min(Screen.width-24,600),152),GUI.skin.box);
            GUILayout.Label(title);GUILayout.Label(controls);
            if(NearBench && !Finished) GUILayout.Label("中转工作台   R 返回工作台 2 换装，或向右继续");
            if (!Finished) GUILayout.Label("← → 移动 · 空格跳 · Z 重试 · Esc 回工作台");
            if (fork && !WindmillCommission.HasAssembly && Vector2.Distance(body.position,startPoint.position)<1)
                GUILayout.Label("Tab 在起点试换：钩手 / 滑梭腿与褶布身");
            GUILayout.EndArea();
        }
    }
}
