using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dollwright.Graybox
{
    // Geometry-only prototype view; coordinates stay independent of UI resolution.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class BodyWorkshopGraphic : MaskableGraphic, IPointerDownHandler, IPointerUpHandler, IDragHandler, IPointerMoveHandler
    {
        public BodyWorkshopSession session;
        public Vector2 Pointer {get;private set;}=new Vector2(1.2f,0);
        public bool Pressed {get;private set;}
        static readonly Color Ink=new Color(.23f,.28f,.31f),Guide=new Color(.98f,.94f,.77f),Green=new Color(.29f,.61f,.51f),Warning=new Color(.83f,.42f,.25f);
        VertexHelper mesh;
        float Scale=>Mathf.Min(rectTransform.rect.width/8,rectTransform.rect.height/4.9f);
        public Vector2 ScreenPoint(Vector2 p)=>RectTransformUtility.WorldToScreenPoint(null,transform.TransformPoint(p*Scale));
        Vector2 Local(PointerEventData e)
        {RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,e.position,e.pressEventCamera,out var p);return p/Scale;}
        public void OnPointerDown(PointerEventData e){if(e.button!=PointerEventData.InputButton.Left)return;Pointer=Local(e);Pressed=true;}
        public void OnPointerUp(PointerEventData e){Pressed=false;}
        public void OnPointerMove(PointerEventData e){if(!Pressed)Pointer=Local(e);}
        public void OnDrag(PointerEventData e)
        {var p=Local(e);if(Pressed&&session!=null)session.Stroke(Pointer,p,Input.GetKey(KeyCode.Space));Pointer=p;SetVerticesDirty();}
        protected override void OnDisable(){Pressed=false;base.OnDisable();}
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();mesh=vh;if(session==null)return;
            if(session.Body==DollBody.Cotton)Sewing();else Shaping();
        }
        void Sewing()
        {
            // Machine base beneath the moving fabric.
            Box(-.25f,.10f,1.95f,.18f,new Color(.64f,.70f,.71f));
            Vector2 center=Fabric(Vector2.zero);
            for(int i=0;i<session.Seam.Count-1;i++)Tri(center,Fabric(session.Seam[i]*1.2f),Fabric(session.Seam[i+1]*1.2f),new Color(.83f,.81f,.75f));
            for(int i=0;i<session.Seam.Count-1;i++)
            {
                var a=Fabric(session.Seam[i]);var b=Fabric(session.Seam[i+1]);
                Line(a,Vector2.Lerp(a,b,.8f),.055f,i<session.Segment?Green:Guide);
            }
            for(int i=1;i<session.Stitches.Count;i+=2)Line(Fabric(session.Stitches[i-1]),Fabric(session.Stitches[i]),.025f,Ink);
            // Chunky arm, upright, hand wheel, and fixed needle.
            Box(.94f,.13f,.49f,1.25f,new Color(.46f,.57f,.59f));
            Box(-.17f,1.04f,1.6f,.35f,new Color(.58f,.68f,.68f));
            Circle(new Vector2(1.43f,.97f),.25f,.25f,Ink);
            Circle(new Vector2(1.43f,.97f),.13f,.13f,new Color(.78f,.78f,.72f));
            float bounce=Input.GetKey(KeyCode.Space)?Mathf.Sin(Time.unscaledTime*45)*.05f:0;
            Line(new Vector2(0,1.07f),new Vector2(0,.42f+bounce),.035f,Ink);
            Line(new Vector2(-.12f,.42f),new Vector2(.12f,.42f),.035f,Ink);
            Circle(new Vector2(0,.38f),.065f,.065f,session.OffLine?Warning:Green);
            // Upcoming stitch direction remains visible on the fabric.
            if(!session.Finished)
            {
                Vector2 next=Fabric(session.Seam[Mathf.Min(session.Segment+2,session.Seam.Count-1)]);
                Circle(next,.07f,.07f,Guide);
            }
        }
        Vector2 Fabric(Vector2 p)
        {
            float a=(-90-session.Heading)*Mathf.Deg2Rad;var d=p-session.Needle;
            return new Vector2(d.x*Mathf.Cos(a)-d.y*Mathf.Sin(a),d.x*Mathf.Sin(a)+d.y*Mathf.Cos(a))+new Vector2(0,.38f);
        }
        void Shaping()
        {
            bool wood=session.Body==DollBody.Wood;
            var baseColor=wood?new Color(.73f,.55f,.36f):new Color(.69f,.72f,.75f);
            Box(-1.45f,-1.55f,2.9f,.26f,wood?new Color(.45f,.38f,.32f):new Color(.43f,.54f,.57f));
            if(!wood)
            {
                Circle(new Vector2(0,-1.25f),1.4f,.22f,new Color(.55f,.65f,.67f));
                for(int i=0;i<8;i++)
                {
                    float a=(session.Spin+i*45)*Mathf.Deg2Rad;
                    Line(new Vector2(0,-1.25f),new Vector2(Mathf.Cos(a)*1.35f,-1.25f+Mathf.Sin(a)*.20f),.018f,Ink);
                }
            }
            for(int i=0;i<BodyWorkshopSession.Rows-1;i++)
            {
                float y=BodyWorkshopSession.Height(i),ny=BodyWorkshopSession.Height(i+1);
                for(int f=0;f<6;f++)
                {
                    float u=f/6f,v=(f+1)/6f;
                    float shade=.84f+.20f*Mathf.Sin((u+.1f)*Mathf.PI);
                    if(!wood)shade+=Mathf.Sin((f*31+session.Spin)*Mathf.Deg2Rad)*.025f;
                    Color tint=new Color(baseColor.r*shade,baseColor.g*shade,baseColor.b*shade,1);
                    Quad(new Vector2(Mathf.Lerp(-session.Left[i],session.Right[i],u),y),new Vector2(Mathf.Lerp(-session.Left[i],session.Right[i],v),y),
                        new Vector2(Mathf.Lerp(-session.Left[i+1],session.Right[i+1],v),ny),new Vector2(Mathf.Lerp(-session.Left[i+1],session.Right[i+1],u),ny),tint);
                }
            }
            if(wood)
            {
                for(int i=0;i<4;i++)Line(new Vector2(-.22f+i*.15f,-1.05f),new Vector2(-.18f+i*.13f,1.05f),.012f,new Color(.62f,.46f,.31f));
                Box(-.25f,-1.62f,.5f,.40f,Ink); // clamp
            }
            // Target stays over the workpiece; a small untouched reference sits on the right.
            for(int i=0;i<BodyWorkshopSession.Rows-1;i++)foreach(float side in new[]{-1f,1f})
            {
                float width=side<0?session.Left[i]:session.Right[i];
                Color c=Mathf.Abs(width-BodyWorkshopSession.Target(i))<=.085f?Green:wood&&width<BodyWorkshopSession.Target(i)-.085f?Warning:Guide;
                Line(new Vector2(side*BodyWorkshopSession.Target(i),BodyWorkshopSession.Height(i)),
                    new Vector2(side*BodyWorkshopSession.Target(i+1),BodyWorkshopSession.Height(i+1)),.028f,c);
                Line(new Vector2(2.6f+side*BodyWorkshopSession.Target(i)*.55f,BodyWorkshopSession.Height(i)*.55f),
                    new Vector2(2.6f+side*BodyWorkshopSession.Target(i+1)*.55f,BodyWorkshopSession.Height(i+1)*.55f),.025f,Ink);
            }
            foreach(int end in new[]{0,BodyWorkshopSession.Rows-1})
            {
                float w=BodyWorkshopSession.Target(end),h=BodyWorkshopSession.Height(end);
                Line(new Vector2(2.6f-w*.55f,h*.55f),new Vector2(2.6f+w*.55f,h*.55f),.025f,Ink);
            }
            if(wood)
            {
                Vector2 p=Pointer;
                Quad(p,p+new Vector2(.26f,.15f),p+new Vector2(.48f,.50f),p+new Vector2(.30f,.62f),new Color(.77f,.81f,.83f));
                Line(p+new Vector2(.38f,.55f),p+new Vector2(.68f,.94f),.19f,Ink);
                if(Pressed)for(int i=0;i<3;i++)
                {var q=new Vector2(-1.2f+i*.32f,-1.83f);Tri(q,q+new Vector2(.13f,.14f),q+new Vector2(.24f,.02f),baseColor);}
            }
            else
            {
                int row=Mathf.Clamp(Mathf.RoundToInt((Pointer.y+1.1f)/2.2f*(BodyWorkshopSession.Rows-1)),0,BodyWorkshopSession.Rows-1);
                foreach(float side in new[]{-1f,1f})Circle(new Vector2(side*(session.Left[row]+.15f),BodyWorkshopSession.Height(row)),.22f,.34f,Pressed?new Color(.82f,.69f,.57f):new Color(.78f,.77f,.71f));
            }
        }
        void Tri(Vector2 a,Vector2 b,Vector2 c,Color tint)
        {int n=mesh.currentVertCount;mesh.AddVert(a*Scale,tint,Vector2.zero);mesh.AddVert(b*Scale,tint,Vector2.zero);mesh.AddVert(c*Scale,tint,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);}
        void Quad(Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color tint){Tri(a,b,c,tint);Tri(a,c,d,tint);}
        void Box(float x,float y,float w,float h,Color tint){Quad(new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h),tint);}
        void Line(Vector2 a,Vector2 b,float thickness,Color tint)
        {var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*thickness*.5f;Quad(a-n,b-n,b+n,a+n,tint);}
        void Circle(Vector2 center,float x,float y,Color tint)
        {for(int i=0;i<16;i++){float a=i*Mathf.PI/8,b=(i+1)*Mathf.PI/8;Tri(center,center+new Vector2(Mathf.Cos(a)*x,Mathf.Sin(a)*y),center+new Vector2(Mathf.Cos(b)*x,Mathf.Sin(b)*y),tint);}}
    }
}
