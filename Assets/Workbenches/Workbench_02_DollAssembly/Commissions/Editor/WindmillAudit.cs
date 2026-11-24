using System;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using Dollwright.RetrievalDemo;

public static class WindmillAudit
{
    public static void Run()
    {
        var previous = Physics2D.simulationMode;
        try { RunRoute(); }
        finally { Physics2D.simulationMode = previous; }
    }

    static void RunRoute()
    {
        EditorSceneManager.OpenScene("Assets/Workbenches/Workbench_02_DollAssembly/Commissions/Windmill_Graybox.unity");
        var trial = UnityEngine.Object.FindObjectOfType<WindmillCommission>();
        var type = trial.GetType();
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        void Call(string name, params object[] args) => type.GetMethod(name, flags).Invoke(trial,args);
        void Set(string name, object value) => type.GetField(name,flags).SetValue(trial,value);
        void Check(bool ok, string name) { if (!ok) throw new Exception(name+" at "+trial.doll.position); }
        Physics2D.simulationMode = SimulationMode2D.Script;
        Call("Start");
        void Walk(int frames, float axis, bool fan = false)
        {
            Set("direction",axis);
            for(int frame=0;frame<frames;frame++){ Physics2D.SyncTransforms(); Call("Step",fan); Physics2D.Simulate(.02f); }
        }
        Vector3 mount=trial.hand.localPosition;
        Vector3 socketRest=trial.socket.position;
        void Use() { Set("action",true);Walk(1,0); }
        Walk(6,0);
        Use(); Walk(26,0);
        Check(trial.hand.parent!=trial.doll,"free drop from spawn");
        Check(Mathf.Abs(trial.hand.GetComponent<Renderer>().bounds.min.y)<.04f && trial.socket.position==socketRest,"ordinary floor must not load socket");
        Use();
        Check(trial.hand.parent==trial.doll && trial.hand.localPosition==mount,"pick up at original mount");
        Set("jump",true);Walk(10,0);
        float releaseHeight=trial.hand.position.y;
        Use(); Walk(8,0);
        Check(trial.hand.parent!=trial.doll && trial.hand.position.y<releaseHeight-.05f,"airborne release and fall");
        Walk(30,1); Walk(10,0); Use();
        Check(trial.hand.parent!=trial.doll,"remote pickup rejected");
        Call("RestartTrial");
        Walk(100,1);
        Set("action",true);Walk(2,0);
        Check(trial.hand.parent!=trial.doll,"detach");
        Check(trial.socket.position.y<socketRest.y,"placing arm depresses socket");
        Use(); Check(trial.hand.parent==trial.doll && trial.socket.position==socketRest,"picking up releases socket");
        Use(); Check(trial.socket.position.y<socketRest.y,"can place arm again");
        Set("jump",true); Walk(59,1); Walk(10,0);
        Check(trial.doll.position.x>2 && trial.doll.position.x<5,"cross booth");
        Walk(60,0,true);
        Check(trial.doll.position.y>4.3f,"lift");
        Walk(56,1,true); Walk(30,0);
        Set("action",true);Walk(1,0);
        Check(!trial.doll.GetComponent<Rigidbody2D>().simulated && trial.hand.parent==trial.doll,"repair");
        Call("RestartTrial");Walk(2,0);Check(trial.doll.position.x<-5 && trial.hand.parent==trial.doll,"rewind");
        Debug.Log("WINDMILL VERIFIED: ground and air drop, pickup, socket release and reseat, walk, jump, lift, repair, rewind");
    }
}
