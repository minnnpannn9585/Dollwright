using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dollwright.RetrievalDemo
{
    public sealed class WindmillCommission : MonoBehaviour
    {
        public const string SceneName = "Windmill_Graybox";
        const string Bench = "Workbench_02_DollAssembly";
        sealed class Loadout
        {
            public readonly bool[] used = new bool[FreeAssembly.PartCount];
            public readonly Vector3[] mounts = new Vector3[FreeAssembly.PartCount];
            public readonly Quaternion[] turns = new Quaternion[FreeAssembly.PartCount];
            public readonly Vector3[] sizes = new Vector3[FreeAssembly.PartCount];
            public Quaternion rotation;
        }
        static Loadout travelling, workInProgress;
        static bool returning, remembered;

        public static void RememberSample(Transform[] parts)
        {
            if (HasAssembly) return;
            travelling = new Loadout { rotation = WorkbenchProjection.ProjectionFrame };
            for (int i=0;i<parts.Length;i++)
            {
                travelling.used[i]=parts[i].gameObject.activeSelf;
                travelling.mounts[i]=parts[i].localPosition;
                travelling.sizes[i]=parts[i].localScale;
                travelling.turns[i]=parts[i].localRotation;
            }
        }

        public static void Launch(FreeAssembly assembly) => Launch(assembly, SceneName);

        public static bool HasAssembly => travelling != null;
        public static bool Equipped(int index) => HasAssembly && travelling.used[index];
        public static void ApplyParts(Transform[] parts)
        {
            if (!HasAssembly) return;
            for (int index = 0; index < parts.Length; index++)
            {
                parts[index].gameObject.SetActive(travelling.used[index]);
                parts[index].localPosition = travelling.mounts[index];
                parts[index].localRotation = travelling.turns[index];
                parts[index].localScale = travelling.sizes[index];
            }
        }
        public static void BackToBench()
        {
            returning = true;
            remembered = false;
            SceneManager.LoadScene(Bench);
        }

        public static void Launch(FreeAssembly assembly, string scene)
        {
            assembly.Validate();
            if (!assembly.Complete) return;
            LaunchRoute(assembly,scene,true);
        }

        public static void LaunchRoute(FreeAssembly assembly,string scene,bool useAssembly)
        {
            var saved = new Loadout { rotation = assembly.transform.rotation };
            assembly.positions.CopyTo(saved.mounts,0);
            assembly.rotations.CopyTo(saved.turns,0);
            assembly.equipped.CopyTo(saved.used,0);
            assembly.sizes.CopyTo(saved.sizes,0);
            workInProgress = saved;
            travelling = useAssembly ? saved : null;
            PaperRoute.Refitting = false;
            SceneManager.LoadScene(scene);
        }

        public static void Restore(RetrievalDemoController bench)
        {
            if (!returning) return;
            returning = false;
            bench.OpenAssembly();
            var original = workInProgress;
            if (PaperRoute.Refitting && travelling != null && (original == null || System.Array.IndexOf(original.used,true)<0))
                original = travelling;
            var restore = original ?? travelling;
            if (restore != null)
            {
                bench.Free.transform.rotation = restore.rotation;
                for (int i=0;i<restore.used.Length;i++)
                    if (restore.used[i]) bench.Free.Place(i,restore.mounts[i],restore.turns[i]);
            }
            if (remembered) bench.StartCoroutine(FanMemory(bench.Doll.transform));
            travelling = workInProgress = null;
            remembered = false;
        }

        static System.Collections.IEnumerator FanMemory(Transform doll)
        {
            var ribbon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ribbon.name = "Remembered_Fan";
            Destroy(ribbon.GetComponent<Collider>());
            ribbon.transform.SetParent(doll, false);
            ribbon.transform.localPosition = new Vector3(-.32f,.07f,0);
            for (float elapsed = 0; elapsed < 1.25f; elapsed += Time.deltaTime)
            {
                ribbon.transform.localScale = new Vector3(.03f + .6f * Mathf.Sin(elapsed / 1.25f * Mathf.PI),.25f,.08f);
                yield return null;
            }
            Destroy(ribbon);
        }

        public Transform spawn, practicePlate, socket, wind, rotor, goal, doll, hand, sail;
        public Transform[] vanes, defaultParts;
        Rigidbody2D body;
        Vector3 handMount, socketRest, practiceRest;
        Quaternion handTurn;
        Vector3 originalHandSize;
        enum HandState { Attached, Falling, Resting, OnSocket, OnPractice }
        HandState handState;
        float fallSpeed;
        Renderer handMesh;
        bool complete, jump, action;
        float direction;
        Camera view;
        JumpWindow jumpWindow;
        PhysicsMaterial2D runnerSurface;
        void OnDestroy() { if (runnerSurface) Destroy(runnerSurface); }
        void Start()
        {
            view = Camera.main;
            body = doll.gameObject.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 2;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var hull = doll.gameObject.AddComponent<BoxCollider2D>();
            runnerSurface = new PhysicsMaterial2D("Cloth runner") { friction = 0, bounciness = 0 };
            hull.sharedMaterial = runnerSurface;
            hull.size = new Vector2(.60f,1.24f);
            hull.offset = new Vector2(0,-.08f);
            if (HasAssembly)
            {
                var used = travelling.used;
                defaultParts=RouteParts.Extend(defaultParts,doll);
                int weight = used[FreeAssembly.Weight] ? FreeAssembly.Weight : used[2] ? 2 : used[1] ? 1 : used[FreeAssembly.RightArm] ? FreeAssembly.RightArm : FreeAssembly.Hook;
                for (int index = 0; index < used.Length; index++)
                {
                    var part = index == weight ? hand : defaultParts[index];
                    if (index == weight) defaultParts[index].gameObject.SetActive(false);
                    part.gameObject.SetActive(used[index]);
                    if (!used[index]) continue;
                    part.localPosition = travelling.mounts[index];
                    part.localRotation = travelling.turns[index];
                    part.localScale = travelling.sizes[index];
                }
            }
            handMesh = hand.GetComponent<Renderer>();
            handMount = hand.localPosition;
            handTurn = hand.localRotation;
            originalHandSize = hand.localScale;
            practiceRest = practicePlate.position;
            socketRest = socket.position;
            RestartTrial();
        }

        void RestartTrial()
        {
            complete = jump = action = false;
            jumpWindow=default;
            rotor.localRotation=Quaternion.identity;
            practicePlate.position=practiceRest;
            socket.position=socketRest;
            AttachHand();
            body.simulated = true;
            doll.position = spawn.position;
            body.position = spawn.position;
            body.velocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || (complete && Input.GetKeyDown(KeyCode.Return)))
            {
                returning = true;
                remembered = complete;
                SceneManager.LoadScene(Bench);
                return;
            }
            if (Input.GetKeyDown(KeyCode.Z)) RestartTrial();
            direction = Input.GetAxisRaw("Horizontal");
            jump |= Input.GetKeyDown(KeyCode.Space);
            action |= Input.GetKeyDown(KeyCode.E);
            if (complete) rotor.Rotate(0,0,-140 * Time.deltaTime);
            view.orthographicSize = Mathf.Max(3.9f,5.2f / view.aspect);
            view.transform.position = new Vector3(Mathf.Clamp(doll.position.x + 1,-4f,5.5f),2.8f,-12);
        }

        bool CanPlaceAtSocket =>
            Mathf.Abs(body.position.x-socketRest.x)<.88f && Mathf.Abs(body.position.y-socketRest.y)<.72f;
        bool NearHand => Vector2.Distance(body.position,hand.position)<1.22f;
        bool SocketLoaded => handState==HandState.OnSocket;

        void AttachHand()
        {
            handState=HandState.Attached;
            fallSpeed=0;
            hand.SetParent(doll,false);
            hand.localPosition=handMount;
            hand.localRotation=handTurn;
            hand.localScale=originalHandSize;
        }

        void RestOnPlate(bool main)
        {
            var plate=main?socket:practicePlate;
            var rest=main?socketRest:practiceRest;
            float depth=main?.055f:.085f;
            hand.position=new Vector3(rest.x,rest.y+plate.lossyScale.y*.5f-depth+handMesh.bounds.extents.y,rest.z-.25f);
            handState=main?HandState.OnSocket:HandState.OnPractice;
            fallSpeed=0;
        }

        void DropHand()
        {
            hand.SetParent(transform,true);
            handState=HandState.Falling;
            fallSpeed=0;
            if (CanPlaceAtSocket) RestOnPlate(true);
        }

        void FallHand()
        {
            float dt=Time.fixedDeltaTime;
            fallSpeed+=19.6f*dt;
            float bottom=handMesh.bounds.min.y;
            float x=hand.position.x;
            float next=bottom-fallSpeed*dt;
            // Only surfaces reached from above can catch the falling arm.
            var hit=Physics2D.Raycast(new Vector2(x,bottom+.035f),Vector2.down,bottom-next+.05f,1<<8);
            float floor=hit.collider?hit.collider.bounds.max.y:float.NegativeInfinity;
            float practiceTop=practiceRest.y+practicePlate.lossyScale.y*.5f;
            float socketTop=socketRest.y+socket.lossyScale.y*.5f;
            if (Mathf.Abs(x-socketRest.x)<=socket.lossyScale.x*.5f && bottom+.04f>=socketTop && next<=socketTop && socketTop>=floor)
                RestOnPlate(true);
            else if (Mathf.Abs(x-practiceRest.x)<=practicePlate.lossyScale.x*.5f && bottom+.04f>=practiceTop && next<=practiceTop && practiceTop>=floor)
                RestOnPlate(false);
            else if (hit.collider)
            {
                hand.position+=Vector3.up*(floor-bottom);
                handState=HandState.Resting;
            }
            else
            {
                hand.position+=Vector3.down*(fallSpeed*dt);
                if(hand.position.y < -3f) AttachHand();
            }
        }
        bool AtGoal => Vector2.Distance(doll.position,goal.position) < 1.4f;

        void FixedUpdate() => Step(Input.GetKey(KeyCode.F));

        void Step(bool fanOpen)
        {
            if (complete) { jump = action = false; return; }
            var position = body.position;
            bool grounded = Physics2D.BoxCast(position + Vector2.down * .66f,new Vector2(.51f,.04f),0,Vector2.down,.08f,1 << 8);
            if (action)
            {
                if (AtGoal && SocketLoaded)
                {
                    complete=true;
                    AttachHand();
                    body.simulated=false;
                    jump=action=false;
                    return;
                }
                if (handState==HandState.Attached) DropHand();
                else if (NearHand) AttachHand();
            }
            if (handState==HandState.Falling) FallHand();
            bool practice = handState==HandState.OnPractice || (grounded && Mathf.Abs(position.x-practiceRest.x)<.56f && Mathf.Abs(position.y-practiceRest.y)<1f);
            bool rising = SocketLoaded || practice;
            practicePlate.position = practiceRest + Vector3.down * (practice ? .085f : 0);
            socket.position = socketRest + Vector3.down * (SocketLoaded ? .055f : 0);
            var half = wind.lossyScale * .5f;
            bool inWind = Mathf.Abs(position.x-wind.position.x) < half.x && Mathf.Abs(position.y-wind.position.y) < half.y;
            sail.localScale = new Vector3(fanOpen ? 1.05f : .18f,.22f,.16f);
            foreach (var vane in vanes) vane.localRotation = Quaternion.Euler(0,0,rising ? 90 : 0);
            var speed = body.velocity;
            speed.x = direction * 3.3f + (inWind && fanOpen && !rising ? 2 : 0);
            if (jumpWindow.Consume(jump,grounded,speed.y)) speed.y = 8.7f;
            if (inWind && rising && fanOpen) speed.y = 3.7f;
            body.velocity = speed;
            if (position.y < -3f) RestartTrial();
            jump = action = false;
        }

        void OnGUI()
        {
            GUI.Box(new Rect(14,14,Mathf.Min(Screen.width-28,570),58), complete
                ? "风车又转起来了！   Enter 回工作台" : "纸剧场委托 · 让风车再转一次");
            if (!complete) GUI.Label(new Rect(24,43,550,25), "← → 移动    空格 跳    E 放下 / 拾回 / 修理    按住 F 张帆    Z 重试    Esc 回工作台");
            string hint = complete ? "" : AtGoal && SocketLoaded ? "E · 修理风车" :
                handState==HandState.Attached ? (CanPlaceAtSocket ? "E · 把手臂放到窗内踏板上" : "E · 放下手臂") :
                NearHand ? (SocketLoaded ? "手臂已压住踏板 · E 拾回" : "E · 拾回手臂") :
                SocketLoaded ? "风已转向 ↑ · 进入风区后按住 F 张帆" :
                (hand.position.x<doll.position.x ? "← 手臂在左侧，靠近后按 E 拾回" : "手臂在右侧 →，靠近后按 E 拾回");
            if (hint.Length > 0) GUI.Box(new Rect(14,Screen.height-47,365,32),hint);
        }
    }
}
