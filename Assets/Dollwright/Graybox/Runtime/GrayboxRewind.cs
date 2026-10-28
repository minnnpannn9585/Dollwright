using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Dollwright.Graybox
{
    // Every playable graybox run owns a checkpoint snapshot, including scenes without boxes.
    public sealed class GrayboxRewind : MonoBehaviour
    {
        public bool IsRewinding { get; private set; }
        public float FogCoverage { get; private set; }
        public int RestoreCount { get; private set; }
        public Vector2 SavedPlayerPosition { get; private set; }
        public float closeSeconds=.45f, holdSeconds=.12f, openSeconds=.55f;
        struct BoxState { public GrayboxPushCrate box; public Vector2 position; }
        readonly List<BoxState> snapshot=new List<BoxState>();
        GrayboxRun run;
        Texture2D fog;
        Color32[] pixels;
        float[] distances,noise;
        GUIStyle buttonStyle;
        Font font;
        void Awake(){run=GetComponent<GrayboxRun>();}
        public void Capture(Vector2 playerPosition)
        {
            if(IsRewinding) return;
            SavedPlayerPosition=playerPosition;
            snapshot.Clear();
            foreach(var box in FindObjectsOfType<GrayboxPushCrate>())
                snapshot.Add(new BoxState{box=box,position=box.CheckpointPosition});
        }
        public bool TryRewind()
        {
            if(IsRewinding || run.player==null || run.Completed || DreamTravel.IsLoading) return false;
            IsRewinding=true;
            StartCoroutine(Transition());
            return true;
        }
        IEnumerator Transition()
        {
            // Freeze physics as well as input: no falling, pushing or extra trigger activations under the curtain.
            var bodies=new List<Rigidbody2D>{run.player.Body};
            foreach(var state in snapshot)if(state.box!=null)bodies.Add(state.box.GetComponent<Rigidbody2D>());
            var simulation=new bool[bodies.Count];
            for(int i=0;i<bodies.Count;i++){simulation[i]=bodies[i].simulated;bodies[i].simulated=false;}
            yield return Animate(0,1,closeSeconds);
            FogCoverage=1;
            foreach(var state in snapshot)if(state.box!=null)state.box.RestoreCheckpoint(state.position);
            run.player.Teleport(SavedPlayerPosition);
            var phase=run.player.GetComponent<DreamPhaseAbility>();if(phase!=null)phase.Cancel();
            run.SetExitAvailable(false);
            var guide=run.GetComponent<Dream02Guide>();if(guide!=null)guide.ReachedEnd=false;
            Physics2D.SyncTransforms();
            foreach(var plate in FindObjectsOfType<DreamPressurePlate>())plate.Evaluate();
            foreach(var saw in FindObjectsOfType<DreamSawGate>())saw.SnapToPlate();
            var camera=Camera.main!=null?Camera.main.GetComponent<GrayboxCamera2D>():null;if(camera!=null)camera.SnapToTarget();
            RestoreCount++;
            yield return new WaitForSecondsRealtime(holdSeconds);
            yield return Animate(1,0,openSeconds);
            FogCoverage=0;
            for(int i=0;i<bodies.Count;i++)if(bodies[i]!=null)bodies[i].simulated=simulation[i];
            Physics2D.SyncTransforms();
            IsRewinding=false;
        }
        IEnumerator Animate(float from,float to,float seconds)
        {
            float t=0;
            while(t<seconds){t+=Time.unscaledDeltaTime;FogCoverage=Mathf.Lerp(from,to,Mathf.SmoothStep(0,1,Mathf.Clamp01(t/seconds)));yield return null;}
            FogCoverage=to;
        }
        void Update(){if(Input.GetKeyDown(KeyCode.Z))TryRewind();}
        void OnGUI()
        {
            if(buttonStyle==null)
            {
                font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},19);
                buttonStyle=new GUIStyle(GUI.skin.button){font=font,fontSize=19};
            }
            GUI.depth=-20;
            GUI.enabled=!IsRewinding&&!run.Completed&&!DreamTravel.IsLoading;
            if(GUI.Button(new Rect(Screen.width-205,Screen.height-68,185,46),"Z · 回溯检查点",buttonStyle))TryRewind();
            GUI.enabled=true;
            if(!IsRewinding || FogCoverage<=0)return;
            GUI.depth=-10000;
            DrawFog();
        }
        void DrawFog()
        {
            const int w=256,h=144;
            if(fog==null)
            {
                fog=new Texture2D(w,h,TextureFormat.RGBA32,false){wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
                pixels=new Color32[w*h];distances=new float[w*h];noise=new float[w*h];
                for(int y=0;y<h;y++)for(int x=0;x<w;x++)
                {
                    int i=y*w+x;float u=(x+.5f)/w-.5f,v=(y+.5f)/h-.5f;
                    distances[i]=Mathf.Sqrt(u*u*3.16f+v*v);
                    noise[i]=Mathf.PerlinNoise(x*.036f,y*.036f)*.11f+Mathf.PerlinNoise(x*.10f,y*.10f)*.035f;
                }
            }
            float radius=Mathf.Lerp(1.25f,-.23f,FogCoverage);
            for(int i=0;i<pixels.Length;i++)
            {
                float a=FogCoverage>=.999f?1:Mathf.SmoothStep(0,1,Mathf.Clamp01((distances[i]-radius+noise[i])/.20f));
                byte shade=(byte)(139+noise[i]*120);
                pixels[i]=new Color32(shade,(byte)(shade+9),(byte)(shade+18),(byte)(255*a));
            }
            fog.SetPixels32(pixels);fog.Apply(false);
            GUI.color=Color.white;GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),fog,ScaleMode.StretchToFill,true);
        }
        void OnDestroy(){if(fog!=null)Destroy(fog);if(font!=null)Destroy(font);}
    }
}
