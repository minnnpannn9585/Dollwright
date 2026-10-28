using UnityEngine;
using UnityEngine.UI;
namespace Dollwright.Graybox
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DollOptionGraphic : MaskableGraphic
    {
        public int category, option;
        static readonly Color Ink=new Color(.22f,.25f,.28f), Paper=new Color(.95f,.94f,.90f);
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if(category==0)
            {
                Color clay=option==0?new Color(.93f,.92f,.86f):option==1?new Color(.59f,.57f,.53f):new Color(.78f,.82f,.85f);
                Circle(mesh,.5f,.66f,.30f,.25f,clay);Circle(mesh,.5f,.25f,.21f,.23f,clay);
                if(option==0)for(int i=0;i<3;i++)Quad(mesh,.69f,.53f+i*.10f,.10f,.025f,Ink);
                if(option==1)for(int i=0;i<3;i++)Line(mesh,new Vector2(.30f,.52f+i*.10f),new Vector2(.67f,.55f+i*.10f),.018f,Ink);
                if(option==2)Quad(mesh,.31f,.65f,.07f,.11f,Color.white);
            }
            else if(category==1)
            {
                Circle(mesh,.5f,.51f,.42f,.40f,Paper);
                bool buttons=option==1||option==3,sad=option>=2;
                foreach(float side in new[]{-1f,1f})
                {
                    float x=.5f+side*.16f;Circle(mesh,x,.55f,buttons ? .10f : .045f,buttons ? .10f : .055f,Ink);
                    if(buttons)
                    {
                        Line(mesh,new Vector2(x-.045f,.505f),new Vector2(x+.045f,.595f),.022f,Paper);
                        Line(mesh,new Vector2(x-.045f,.595f),new Vector2(x+.045f,.505f),.022f,Paper);
                    }
                    if(sad)Line(mesh,new Vector2(x-side*.06f,.76f),new Vector2(x+side*.06f,.69f),.025f,Ink);
                }
                float y=sad ? .32f : .29f,edge=sad ? .25f : .36f;
                Line(mesh,new Vector2(.38f,edge),new Vector2(.5f,y),.030f,Ink);
                Line(mesh,new Vector2(.5f,y),new Vector2(.62f,edge),.030f,Ink);
            }
            else if(option==0)
            {
                Circle(mesh,.5f,.5f,.30f,.30f,Ink);Circle(mesh,.5f,.5f,.25f,.25f,Paper);
                Line(mesh,new Vector2(.25f,.23f),new Vector2(.75f,.78f),.06f,Ink);
            }
            else if(option==1)
            {
                Quad(mesh,.13f,.22f,.74f,.09f,Ink);Quad(mesh,.29f,.29f,.43f,.51f,Ink);
                Quad(mesh,.30f,.37f,.42f,.08f,new Color(.65f,.67f,.68f));
            }
            else
            {
                Circle(mesh,.47f,.47f,.32f,.32f,Ink);Circle(mesh,.47f,.47f,.28f,.28f,Paper);
                Circle(mesh,.83f,.60f,.10f,.10f,Ink);Circle(mesh,.83f,.60f,.07f,.07f,Paper);
                Quad(mesh,.32f,.80f,.30f,.07f,Ink);
                for(int i=0;i<3;i++)Quad(mesh,.32f+i*.12f,.85f,.06f,.10f,Ink);
            }
        }
        Vector3 Point(float x,float y){var r=rectTransform.rect;return new Vector3(r.xMin+x*r.width,r.yMin+y*r.height,0);}
        void Quad(VertexHelper mesh,float x,float y,float w,float h,Color tint)
        {
            int n=mesh.currentVertCount;
            mesh.AddVert(Point(x,y),tint,Vector2.zero);mesh.AddVert(Point(x,y+h),tint,Vector2.zero);
            mesh.AddVert(Point(x+w,y+h),tint,Vector2.zero);mesh.AddVert(Point(x+w,y),tint,Vector2.zero);
            mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
        }
        void Circle(VertexHelper mesh,float x,float y,float rx,float ry,Color tint)
        {
            int n=mesh.currentVertCount;mesh.AddVert(Point(x,y),tint,Vector2.zero);
            for(int i=0;i<20;i++){float a=i*Mathf.PI*.1f;mesh.AddVert(Point(x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry),tint,Vector2.zero);}
            for(int i=0;i<20;i++)mesh.AddTriangle(n,n+1+i,n+1+(i+1)%20);
        }
        void Line(VertexHelper mesh,Vector2 a,Vector2 b,float width,Color tint)
        {
            Vector2 normal=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
            int n=mesh.currentVertCount;var points=new[]{a+normal,b+normal,b-normal,a-normal};
            foreach(var p in points)mesh.AddVert(Point(p.x,p.y),tint,Vector2.zero);
            mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n,n+2,n+3);
        }
    }
}

