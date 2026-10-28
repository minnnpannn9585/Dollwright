using System.Collections.Generic;
using UnityEngine;

namespace Dollwright.Graybox
{
    // Workpiece-only state: never spends ingredients or writes the player's save.
    public sealed class BodyWorkshopSession
    {
        public const int Rows=13;
        public readonly DollBody Body;
        public readonly float[] Left=new float[Rows], Right=new float[Rows];
        public readonly List<Vector2> Seam=new List<Vector2>(), Stitches=new List<Vector2>();
        public Vector2 Needle { get; private set; }
        public float Heading { get; private set; }
        public float Spin { get; private set; }
        public bool OffLine { get; private set; }
        public int Segment { get; private set; }
        public float SegmentT { get; private set; }
        public int Cuts { get; private set; }
        public bool Finished => Body==DollBody.Cotton ? Segment>=Seam.Count-1 : MaxError<=.085f;
        public float MaxError
        {
            get { float e=0;for(int i=0;i<Rows;i++){e=Mathf.Max(e,Mathf.Abs(Left[i]-Target(i)));e=Mathf.Max(e,Mathf.Abs(Right[i]-Target(i)));}return e; }
        }
        public float Progress => Finished ? 1 : Body==DollBody.Cotton ? Mathf.Clamp01((Segment+SegmentT)/(Seam.Count-1)) : Mathf.Clamp01(1-MaxError/(Body==DollBody.Wood?.81f:.33f));
        public static float Height(int i)=>-1.1f+2.2f*i/(Rows-1);
        public static float Target(int i)
        {
            float t=i/(float)(Rows-1);
            return .43f+.43f*Mathf.Sin(Mathf.PI*t)-.12f*t;
        }
        public BodyWorkshopSession(DollBody body)
        {
            Body=body;
            for(int i=0;i<=32;i++)
            {
                float a=i*Mathf.PI*2/32;
                Seam.Add(new Vector2(Mathf.Sin(a)*(.77f-.14f*Mathf.Cos(a)),Mathf.Cos(a)*.9f));
            }
            Reset();
        }
        public void Reset()
        {
            for(int i=0;i<Rows;i++)Left[i]=Right[i]=Body==DollBody.Wood?1.12f:.64f;
            Segment=0;SegmentT=0;Cuts=0;Spin=0;OffLine=false;
            Needle=Seam[0];var d=Seam[1]-Seam[0];Heading=Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg;
            Stitches.Clear();Stitches.Add(Needle);
        }
        public void Turn(float degrees){Heading=Mathf.Repeat(Heading+degrees+180,360)-180;}
        public void Tick(float seconds,bool powered)
        {
            seconds=Mathf.Clamp(seconds,0,.05f);
            if(Body==DollBody.Ceramic){if(powered)Spin=Mathf.Repeat(Spin+seconds*110,360);return;}
            if(Body!=DollBody.Cotton||!powered||Finished)return;
            Vector2 direction=new Vector2(Mathf.Cos(Heading*Mathf.Deg2Rad),Mathf.Sin(Heading*Mathf.Deg2Rad));
            Vector2 proposed=Needle+direction*(seconds*.25f);
            var a=Seam[Segment];var b=Seam[Segment+1];var edge=b-a;
            if(Vector2.Dot(direction,edge.normalized)<.15f){OffLine=true;return;}
            float t=Vector2.Dot(proposed-a,edge)/edge.sqrMagnitude;
            Vector2 closest=Vector2.Lerp(a,b,Mathf.Clamp01(t));
            if(Vector2.Distance(proposed,closest)>.10f || t<SegmentT-.04f)
            {
                OffLine=true;Needle=Vector2.Lerp(a,b,SegmentT);return;
            }
            OffLine=false;Needle=proposed;SegmentT=Mathf.Max(SegmentT,Mathf.Clamp01(t));
            if(Vector2.Distance(Stitches[Stitches.Count-1],Needle)>.045f)Stitches.Add(Needle);
            if(t>=1)
            {
                Segment++;SegmentT=0;
                if(Finished){Needle=Seam[Seam.Count-1];Stitches.Add(Needle);}
            }
        }
        public bool Stroke(Vector2 from,Vector2 to,bool spinning)
        {
            if(Body==DollBody.Cotton || (Body==DollBody.Ceramic&&!spinning))return false;
            float distance=Vector2.Distance(from,to);
            if(distance<.0001f || distance>1f)return false; // ignore pointer jumps/re-entry
            int steps=Mathf.CeilToInt(distance/.025f);bool changed=false;
            for(int step=1;step<=steps;step++)
            {
                Vector2 p=Vector2.Lerp(from,to,step/(float)steps);
                if(p.y< -1.22f||p.y>1.22f||Mathf.Abs(p.x)<.1f)continue;
                float row=Mathf.Clamp((p.y+1.1f)/2.2f*(Rows-1),0,Rows-1);
                int nearest=Mathf.RoundToInt(row);var side=p.x<0?Left:Right;
                if(Mathf.Abs(Mathf.Abs(p.x)-side[nearest])>.30f)continue;
                for(int i=0;i<Rows;i++)
                {
                    float weight=Mathf.Max(0,1-Mathf.Abs(i-row)/1.8f);
                    if(weight<=0)continue;
                    if(Body==DollBody.Wood)
                    {
                        float before=side[i];side[i]=Mathf.Max(.16f,side[i]-distance/steps*.32f*weight);
                        changed|=side[i]!=before;
                    }
                    else
                    {
                        float delta=(to.x-from.x)/steps*Mathf.Sign(p.x)*.72f*weight;
                        float value=Mathf.Clamp(Left[i]+delta,.18f,1.18f);
                        changed|=value!=Left[i];Left[i]=Right[i]=value;
                    }
                }
            }
            if(changed)Cuts++;
            return changed;
        }
    }
}
