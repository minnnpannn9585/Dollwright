using System.Collections.Generic;
using UnityEngine;

namespace Dollwright.RetrievalDemo
{
    [System.Serializable]
    public struct DollAssembly
    {
        public int body, head, arm, socket;
        public static DollAssembly Default => new DollAssembly { body=0, head=0, arm=1, socket=1 };
    }

    // Each selectable part is real geometry. The same compound colliders are used at every gate.
    public sealed class RetrievalDoll : MonoBehaviour
    {
        public DollAssembly recipe;
        public readonly List<Collider> pieces = new List<Collider>();
        public Material ivory, teal, brass, ink;
        public Transform armVisual;
        public Vector3 Shoulder => GetComponent<FreeAssembly>() ? GetComponent<FreeAssembly>().positions[GetComponent<FreeAssembly>().ActiveArm] : new Vector3(.34f,.25f,0);
        public const float LinkA=.90f, LinkB=.65f, ArmRadius=.035f;

        public void Rebuild(DollAssembly value)
        {
            recipe=value; pieces.Clear();
            for(int i=transform.childCount-1;i>=0;i--)
            {
                var old=transform.GetChild(i).gameObject;old.SetActive(false);
                if(Application.isPlaying)Destroy(old);else DestroyImmediate(old);
            }
            var free=GetComponent<FreeAssembly>();if(free){free.Build();return;}
            Vector3[] bodies={new Vector3(.60f,.66f,.24f),new Vector3(.68f,.66f,.68f),new Vector3(.94f,.46f,.24f)};
            Part("Body",PrimitiveType.Cube,new Vector3(0,-.02f,0),bodies[value.body],ivory,true);
            Part("Head",value.head==0?PrimitiveType.Sphere:PrimitiveType.Cube,new Vector3(0,.57f,0),Vector3.one*(value.head==0?.44f:.48f),ivory,true);
            for(int sign=-1;sign<=1;sign+=2)
            {
                Part("Foot",PrimitiveType.Cube,new Vector3(sign*.16f,-.49f,0),new Vector3(.19f,.26f,.25f),teal,true);
                Part("Eye",PrimitiveType.Sphere,new Vector3(sign*.073f,.60f,-.214f),new Vector3(.047f,.065f,.018f),ink,false);
            }
            Part("Left_Mitten",PrimitiveType.Cube,new Vector3(-bodies[value.body].x*.5f-.06f,-.12f,0),new Vector3(.12f,.35f,.19f),teal,true);
            armVisual=new GameObject("Arm_Module").transform;armVisual.SetParent(transform,false);
            SetArm(-90,180,.45f,true);
        }
        GameObject Part(string name,PrimitiveType type,Vector3 pos,Vector3 size,Material mat,bool solid,Transform parent=null)
        {
            var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(parent?parent:transform,false);g.transform.localPosition=pos;g.transform.localScale=size;
            g.GetComponent<Renderer>().sharedMaterial=mat;
            var c=g.GetComponent<Collider>();
            if(solid)pieces.Add(c);else c.enabled=false;
            g.layer=gameObject.layer;return g;
        }
        public void SetArm(float shoulderAngle,float elbowAngle,float length,bool solid)
        {
            if(!armVisual)return;
            var free=GetComponent<FreeAssembly>();if(free&&free.parts[free.ActiveArm]){free.parts[free.ActiveArm].gameObject.SetActive(false);pieces.Remove(free.parts[free.ActiveArm]);}
            for(int i=armVisual.childCount-1;i>=0;i--)
            {
                var old=armVisual.GetChild(i).gameObject;
                foreach(var collider in old.GetComponentsInChildren<Collider>())pieces.Remove(collider);
                old.SetActive(false);if(Application.isPlaying)Destroy(old);else DestroyImmediate(old);
            }
            Vector3 a=Shoulder,b=a+Direction(shoulderAngle)*(recipe.arm==0?length:LinkA);
            Vector3 c=recipe.arm==0?b:b+Direction(shoulderAngle+elbowAngle)*LinkB;
            Rod("Upper_Arm",a,b,recipe.arm==0?.065f:.07f,teal,solid);
            if(recipe.arm==1){Rod("Forearm",b,c,.06f,brass,solid);Part("Elbow",PrimitiveType.Sphere,b,Vector3.one*.10f,brass,solid,armVisual);}
            Part("Shoulder",PrimitiveType.Sphere,a,Vector3.one*.13f,brass,solid,armVisual);
            Part("Gripper",PrimitiveType.Sphere,c,Vector3.one*.11f,brass,solid,armVisual);
        }
        void Rod(string name,Vector3 a,Vector3 b,float width,Material material,bool solid)
        {
            var g=Part(name,PrimitiveType.Cube,(a+b)*.5f,new Vector3(width,Vector3.Distance(a,b),width),material,solid,armVisual);
            g.transform.localRotation=Quaternion.FromToRotation(Vector3.up,(b-a).normalized);
        }
        public Vector3[] ArmPoints(float shoulder,float elbow,float extension)
        {
            var a=Shoulder;var b=a+Direction(shoulder)*(recipe.arm==0?extension:LinkA);
            var c=recipe.arm==0?b:b+Direction(shoulder+elbow)*LinkB;
            return new[]{transform.TransformPoint(a),transform.TransformPoint(b),transform.TransformPoint(c)};
        }
        static Vector3 Direction(float angle){float a=angle*Mathf.Deg2Rad;return new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);}
        public bool Overlaps(Collider[] walls,out string obstacle)
        {
            foreach(var own in pieces)
            {
                if(!own||!own.enabled||!own.gameObject.activeInHierarchy)continue;
                foreach(var other in walls)
                {
                    if(!other||!other.enabled||!other.gameObject.activeInHierarchy)continue;
                    if(Physics.ComputePenetration(own,own.transform.position,own.transform.rotation,other,other.transform.position,other.transform.rotation,out _,out float distance)&&distance>.001f)
                    {obstacle=other.name;return true;}
                }
            }
            obstacle="";return false;
        }
    }
}

