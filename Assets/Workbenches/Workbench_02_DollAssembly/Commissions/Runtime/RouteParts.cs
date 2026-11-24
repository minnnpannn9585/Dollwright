using System.Collections.Generic;
using UnityEngine;

namespace Dollwright.RetrievalDemo
{
    public static class RouteParts
    {
        static readonly Dictionary<int,Mesh> meshes=new Dictionary<int,Mesh>();
        public static void Shape(GameObject target,int index)
        {
            var filter=target.GetComponent<MeshFilter>();
            if (meshes.TryGetValue(index,out var cached) && cached) {filter.sharedMesh=cached;return;}
            var pieces=new List<CombineInstance>();
            void Bar(float x,float y,float w,float h,float depth,float angle=0)
            {
                pieces.Add(new CombineInstance{mesh=filter.sharedMesh,transform=Matrix4x4.TRS(new Vector3(x,y,0),Quaternion.Euler(0,0,angle),new Vector3(w,h,depth))});
            }
            if(index==FreeAssembly.Hook)
            {
                Bar(-.29f,.22f,.28f,.65f,.85f);
                for(int k=0;k<6;k++){float a=(180+k*31)*Mathf.Deg2Rad;Bar(.32f*Mathf.Cos(a),-.10f+.32f*Mathf.Sin(a),.18f,.26f,.8f,a*Mathf.Rad2Deg);}
            }
            else if(index==FreeAssembly.Wing){Bar(-.24f,.02f,.58f,.35f,.34f,-26);Bar(.24f,.02f,.58f,.35f,.34f,26);Bar(0,-.17f,.16f,.44f,.8f);}
            else if(index==FreeAssembly.Runners){Bar(-.28f,0,.23f,.94f,.9f,-12);Bar(.28f,0,.23f,.94f,.9f,-12);Bar(0,.18f,.74f,.16f,.48f);}
            else if(index==FreeAssembly.Pleats){for(int k=0;k<5;k++)Bar((k-2)*.18f,0,.19f,.81f,.48f,k%2==0?13:-13);}
            else if(index==FreeAssembly.Magnet){Bar(-.32f,.11f,.29f,.7f,.75f);Bar(.32f,.11f,.29f,.7f,.75f);Bar(0,-.30f,.93f,.22f,.75f);}
            else if(index==FreeAssembly.Boots){for(int side=-1;side<=1;side+=2){Bar(side*.26f,-.27f,.46f,.29f,1f);Bar(side*.26f,.10f,.33f,.58f,.65f);}}
            else {Bar(0,.23f,.35f,.58f,.44f);Bar(0,-.19f,.88f,.58f,.85f);}
            var mesh=new Mesh{name="Workshop module "+index};
            mesh.CombineMeshes(pieces.ToArray());
            var bounds=mesh.bounds;var vertices=mesh.vertices;
            for(int k=0;k<vertices.Length;k++){var v=vertices[k]-bounds.center;vertices[k]=new Vector3(v.x/bounds.size.x,v.y/bounds.size.y,v.z/bounds.size.z);}
            mesh.vertices=vertices;mesh.RecalculateBounds();meshes[index]=mesh;filter.sharedMesh=mesh;
        }
        public static Transform[] Extend(Transform[] original, Transform parent)
        {
            var all=new Transform[FreeAssembly.PartCount];
            original.CopyTo(all,0);
            for(int i=original.Length;i<all.Length;i++)
            {
                var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);
                obj.name=new[]{"Spool hook","Glider","Sled feet","Pleated body","Wood weight","Magnet head","Ballast boots","Right arm"}[i-FreeAssembly.Hook];
                obj.GetComponent<Collider>().enabled=false;
                obj.GetComponent<Renderer>().sharedMaterial=original[2].GetComponent<Renderer>().sharedMaterial;
                obj.transform.SetParent(parent,false);if(i!=FreeAssembly.RightArm)Shape(obj,i);
                obj.SetActive(false);all[i]=obj.transform;
            }
            return all;
        }
    }
}
