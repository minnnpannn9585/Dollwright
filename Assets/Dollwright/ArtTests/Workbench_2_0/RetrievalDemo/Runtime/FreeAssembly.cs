using UnityEngine;
using UnityEngine.EventSystems;
namespace Dollwright.RetrievalDemo
{
 public sealed class FreeAssembly : MonoBehaviour
 {
  public static readonly Vector3 TorsoSize=new Vector3(.64f,.72f,.30f);
  public readonly Vector3[] sizes={new Vector3(.40f,.40f,.38f),new Vector3(.16f,.30f,.18f),new Vector3(.23f,.42f,.24f),new Vector3(.28f,.22f,.22f),new Vector3(.28f,.22f,.22f),new Vector3(.48f,.48f,.20f)};
  public readonly string[] names={"头部","短手臂","伸缩手臂","腿 A","腿 B","滚轮"};
  public Vector3[] positions=new Vector3[6];public Quaternion[] rotations=new Quaternion[6];
  public BoxCollider torso; public BoxCollider[] parts=new BoxCollider[6];
  public bool[] connected=new bool[6];public string[] reasons=new string[6];
  public int selected=-1;public bool dragging;public RetrievalDemoController owner;public RetrievalDoll doll;
  public bool wholeMode;public bool WholeActive=>wholeMode||Input.GetKey(KeyCode.LeftControl)||Input.GetKey(KeyCode.RightControl);
  public void ToggleWhole(){CancelGesture();wholeMode=!wholeMode;}
  MaterialPropertyBlock block;
  bool rotating,ringDrag;Vector2 lastMouse,dragStart;float ringRadius;const float SnapRange=.032f;
  public int Count {get{int n=0;foreach(bool b in connected)if(b)n++;return n;}}
  public bool[] equipped=new bool[6];
  public bool Complete {get{if(dragging||rotating||!connected[0]||!(connected[1]||connected[2])||!(connected[5]||(connected[3]&&connected[4])))return false;for(int i=0;i<6;i++)if(equipped[i]&&!connected[i])return false;return true;}}
  public int ActiveArm=>equipped[2]?2:1;
  public void Toggle(int i){if(equipped[i])ReturnPart(i);else{ResetPose(i);equipped[i]=true;selected=i;parts[i].gameObject.SetActive(true);Validate();}}
  void ResetPose(int i){positions[i]=Loose(i);rotations[i]=Quaternion.identity;if(parts[i]){parts[i].transform.localPosition=positions[i];parts[i].transform.localRotation=rotations[i];}}
  public void ReturnPart(int i){CancelGesture();equipped[i]=false;ResetPose(i);parts[i].gameObject.SetActive(false);if(selected==i)selected=-1;Validate();}
  public void ResetSelected(){if(selected==6){CancelGesture();RotateCore(WorkbenchProjection.ProjectionFrame);return;}if(selected<0||(selected<6&&!equipped[selected]))return;CancelGesture();ResetPose(selected);Validate();}
  public void CancelGesture(){dragging=false;rotating=false;ringDrag=false;}
  Vector3 Loose(int i)=>new Vector3(i<3?-1.12f:1.12f,.65f-(i%3)*.65f,0);
  public void Initialize(RetrievalDemoController controller,RetrievalDoll model){owner=controller;doll=model;ResetParts();}
  public void ResetParts(){for(int i=0;i<6;i++){equipped[i]=false;positions[i]=Loose(i);rotations[i]=Quaternion.identity;}selected=-1;CancelGesture();if(doll)doll.Rebuild(owner.Recipe);}
  public void Build()
  {
   torso=Cube("Fixed_Torso",Vector3.zero,TorsoSize,Quaternion.identity,doll.ivory).GetComponent<BoxCollider>();
   for(int i=0;i<6;i++)
   {
    var go=Cube(names[i],positions[i],sizes[i],rotations[i],doll.teal);parts[i]=go.GetComponent<BoxCollider>();
    if(i==5){var mesh=WheelMesh();go.GetComponent<MeshFilter>().sharedMesh=mesh;var mc=go.AddComponent<MeshCollider>();mc.sharedMesh=mesh;mc.convex=true;parts[i].enabled=owner.State==RetrievalDemoController.Mode.Assembly;doll.pieces.Remove(parts[i]);doll.pieces.Add(mc);}
    if(!equipped[i])go.SetActive(false);
    if(i==0)for(int side=-1;side<=1;side+=2){var eye=GameObject.CreatePrimitive(PrimitiveType.Sphere);eye.name="Eye";eye.layer=29;eye.transform.SetParent(go.transform,false);eye.transform.localPosition=new Vector3(side*.19f,.08f,-.51f);eye.transform.localScale=new Vector3(.12f,.13f,.04f);eye.GetComponent<Renderer>().sharedMaterial=doll.ink;eye.GetComponent<Collider>().enabled=false;}
   }
   doll.armVisual=new GameObject("Deployed_Arm").transform;doll.armVisual.SetParent(transform,false);Validate();
  }
  Mesh WheelMesh(){var v=new Vector3[32];for(int j=0;j<16;j++){float a=j*Mathf.PI/8;v[j]=new Vector3(Mathf.Cos(a)*.5f,Mathf.Sin(a)*.5f,-.5f);v[j+16]=new Vector3(v[j].x,v[j].y,.5f);}var t=new System.Collections.Generic.List<int>();for(int j=0;j<16;j++){int n=(j+1)%16;t.AddRange(new[]{j,n,j+16,n,n+16,j+16});if(j>0&&j<15)t.AddRange(new[]{0,j+1,j,16,16+j,17+j});}var m=new Mesh();m.vertices=v;m.triangles=t.ToArray();m.RecalculateNormals();return m;}
  GameObject Cube(string name,Vector3 pos,Vector3 size,Quaternion rot,Material material)
  {var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.layer=29;g.transform.SetParent(transform,false);g.transform.localPosition=pos;g.transform.localRotation=rot;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;doll.pieces.Add(g.GetComponent<Collider>());return g;}
  public void Place(int i,Vector3 position,Quaternion rotation)
  {equipped[i]=true;parts[i].gameObject.SetActive(true);positions[i]=position;rotations[i]=rotation;parts[i].transform.localPosition=position;parts[i].transform.localRotation=rotation;Validate();}
  public void Validate()
  {
   if(!torso)return;Physics.SyncTransforms();float scale=transform.lossyScale.x;
   for(int i=0;i<6;i++)
   {
    if(!equipped[i]){connected[i]=false;reasons[i]="未选用";continue;}
    var p=parts[i];
    bool touching=SurfaceGap(p)<=.018f*scale;
    bool buried=Physics.ComputePenetration(p,p.transform.position,p.transform.rotation,torso,torso.transform.position,torso.transform.rotation,out _,out float d)&&d>.012f*scale;
    bool overlap=false;
    for(int j=0;j<6;j++)if(j!=i&&equipped[j]&&Physics.ComputePenetration(p,p.transform.position,p.transform.rotation,parts[j],parts[j].transform.position,parts[j].transform.rotation,out _,out float pd)&&pd>.012f*scale)overlap=true;
    connected[i]=touching&&!buried&&!overlap;reasons[i]=buried?"不能埋入躯干":overlap?"部件重叠":!touching?"尚未连接躯干":"已连接";
   }
   RefreshFeedback();
  }
  public void BeginDrag(int i){CancelGesture();selected=i;if(i==6)return;if(!equipped[i])ResetPose(i);equipped[i]=true;parts[i].gameObject.SetActive(true);dragging=true;dragStart=Input.mousePosition;Validate();}
  public void ContinueDrag(Vector2 p){if(!dragging||Vector2.Distance(p,dragStart)<4)return;Drag(owner.view.ScreenPointToRay(p));}
  public void EndDrag(Vector2 p){if(!dragging)return;dragging=false;if(owner.warehouse&&owner.warehouse.Contains(p)){ReturnPart(selected);return;}TrySnap(selected);Validate();}
  public bool TrySnap(int i){if(i<0||i>=6||!equipped[i])return false;if(SnapCandidate(i,out Vector3 p)){Place(i,p,rotations[i]);return true;}return false;}
  float SurfaceGap(Collider c){Vector3 a=torso.ClosestPoint(c.bounds.center),b=c.ClosestPoint(a);for(int n=0;n<96;n++){Vector3 old=b;a=torso.ClosestPoint(b);b=c.ClosestPoint(a);if((old-b).sqrMagnitude<1e-13f)break;}return Vector3.Distance(a,b);}
  bool ClearAt(int i,Vector3 p,Quaternion q,bool requireContact){var c=parts[i];Vector3 old=c.transform.localPosition;Quaternion oldQ=c.transform.localRotation;c.transform.localPosition=p;c.transform.localRotation=q;Physics.SyncTransforms();float scale=transform.lossyScale.x;bool clear=true;
   if(Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,torso,torso.transform.position,torso.transform.rotation,out _,out float depth)&&depth>.003f*scale)clear=false;
   for(int j=0;j<6&&clear;j++)if(j!=i&&equipped[j]&&Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,parts[j],parts[j].transform.position,parts[j].transform.rotation,out _,out depth)&&depth>.003f*scale)clear=false;
   if(requireContact&&SurfaceGap(c)>.008f*scale)clear=false;
   c.transform.localPosition=old;c.transform.localRotation=oldQ;Physics.SyncTransforms();return clear;
  }
  bool SnapCandidate(int i,out Vector3 result){result=positions[i];float best=SnapRange+.0001f;Vector3 h=TorsoSize*.5f;
   for(int axis=0;axis<3;axis++)for(int side=-1;side<=1;side+=2){Vector3 n=Vector3.zero;n[axis]=side;Vector3 p=positions[i];p[axis]=side*(h[axis]+Support(i,n)+.002f);float distance=Vector3.Distance(p,positions[i]);if(distance<=best&&ClearAt(i,p,rotations[i],true)){result=p;best=distance;}}
   return best<=SnapRange;
  }
  public void RotateSelected(float horizontal,float vertical,float roll){if(WholeActive){transform.rotation=Quaternion.AngleAxis(roll,owner.view.transform.forward)*Quaternion.AngleAxis(-vertical,owner.view.transform.right)*Quaternion.AngleAxis(horizontal,owner.view.transform.up)*transform.rotation;return;}int i=selected;if(i==6){RotateCore(Quaternion.AngleAxis(roll,owner.view.transform.forward)*Quaternion.AngleAxis(-vertical,owner.view.transform.right)*Quaternion.AngleAxis(horizontal,owner.view.transform.up)*transform.rotation);return;}if(i<0||!equipped[i])return;Quaternion old=rotations[i];Vector3 before=positions[i];bool attached=connected[i];
   Vector3 up=transform.InverseTransformDirection(owner.view.transform.up),right=transform.InverseTransformDirection(owner.view.transform.right),forward=transform.InverseTransformDirection(owner.view.transform.forward);
   Quaternion q=Quaternion.AngleAxis(roll,forward)*Quaternion.AngleAxis(-vertical,right)*Quaternion.AngleAxis(horizontal,up)*old;Vector3 p=before;
   if(attached){int face=0;float best=float.MaxValue;for(int axis=0;axis<3;axis++){Vector3 n=Vector3.zero;n[axis]=Mathf.Sign(before[axis]);float gap=Mathf.Abs(Mathf.Abs(before[axis])-TorsoSize[axis]*.5f-Support(i,n));if(gap<best){best=gap;face=axis;}}rotations[i]=q;Vector3 normal=Vector3.zero;normal[face]=Mathf.Sign(before[face]);p[face]=normal[face]*(TorsoSize[face]*.5f+Support(i,normal)+.002f);rotations[i]=old;}
   if(!ClearAt(i,p,q,attached))return;Place(i,p,q);if(!attached)TrySnap(i);
  }
  public void RotateCore(Quaternion rotation){var old=transform.rotation;Quaternion correction=Quaternion.Inverse(rotation)*old;for(int i=0;i<6;i++){positions[i]=correction*positions[i];rotations[i]=correction*rotations[i];parts[i].transform.localPosition=positions[i];parts[i].transform.localRotation=rotations[i];}transform.rotation=rotation;Validate();for(int i=0;i<6;i++)if(equipped[i]&&!connected[i])TrySnap(i);}
  int Pick(Vector2 screen){Ray ray=owner.view.ScreenPointToRay(screen);float distance=float.MaxValue;int hit=-1;if(torso.Raycast(ray,out RaycastHit core,20)){distance=core.distance;hit=6;}for(int i=0;i<6;i++)if(equipped[i]&&parts[i].gameObject.activeInHierarchy&&parts[i].Raycast(ray,out RaycastHit h,20)&&h.distance<distance){distance=h.distance;hit=i;}return hit;}
  Vector2 Center()=>owner.view.WorldToScreenPoint(WholeActive||selected==6?transform.position:parts[selected].transform.position);
  float Radius(){if(WholeActive){float all=40;Vector2 centerAll=Center();foreach(var c in doll.pieces)if(c&&c.gameObject.activeInHierarchy){Bounds b=c.bounds;for(int i=0;i<8;i++){Vector3 v=b.center+Vector3.Scale(b.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));all=Mathf.Max(all,Vector2.Distance(centerAll,owner.view.WorldToScreenPoint(v))+16);}}return all;}Vector2 center=Center();float r=0;var t=selected==6?torso.transform:parts[selected].transform;for(int i=0;i<8;i++){Vector3 corner=new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.5f:.5f,(i&4)==0?-.5f:.5f);r=Mathf.Max(r,Vector2.Distance(center,owner.view.WorldToScreenPoint(t.TransformPoint(corner))));}return Mathf.Max(36,r+16);}
  public void Tick(){if(owner.State!=RetrievalDemoController.Mode.Assembly)return;Vector2 mouse=Input.mousePosition;bool ui=EventSystem.current&&EventSystem.current.IsPointerOverGameObject();
   if(Input.GetMouseButtonDown(0)&&!ui){int hit=Pick(mouse);if(hit>=0&&!WholeActive)BeginDrag(hit);else{selected=-1;CancelGesture();}}
   if(dragging&&Input.GetMouseButton(0))ContinueDrag(mouse);if(Input.GetMouseButtonUp(0))EndDrag(mouse);
   if(Input.GetMouseButtonDown(1)&&!ui&&!dragging){ringDrag=(WholeActive||selected>=0)&&Mathf.Abs(Vector2.Distance(mouse,Center())-Radius())<12;if(!ringDrag){int hit=Pick(mouse);if(hit>=0)selected=hit;else if(!WholeActive)return;}rotating=true;ringRadius=Radius();lastMouse=mouse;}
   if(rotating&&Input.GetMouseButton(1)){if(!ui){if(ringDrag){Vector2 a=lastMouse-Center(),b=mouse-Center();if(a.magnitude>10&&b.magnitude>10)RotateSelected(0,0,-Vector2.SignedAngle(a,b));}else{Vector2 d=mouse-lastMouse;RotateSelected(d.x*.45f,d.y*.45f,0);}}lastMouse=mouse;}
   if(Input.GetMouseButtonUp(1)){rotating=false;ringDrag=false;TrySnap(selected);}
   if(!ui&&!rotating&&(WholeActive||selected>=0)){float scroll=Input.mouseScrollDelta.y;if(scroll!=0){bool shift=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);RotateSelected(0,shift?0:scroll*15,shift?scroll*15:0);}}
   float yaw=(Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),pitch=(Input.GetKey(KeyCode.W)?1:0)-(Input.GetKey(KeyCode.S)?1:0);if(!dragging&&!rotating){transform.Rotate(owner.view.transform.up,yaw*70*Time.deltaTime,Space.World);transform.Rotate(owner.view.transform.right,pitch*70*Time.deltaTime,Space.World);}RefreshFeedback();
  }
  void OnGUI(){if(!owner||owner.State!=RetrievalDemoController.Mode.Assembly||(!WholeActive&&(selected<0||(selected<6&&!equipped[selected]))))return;Vector2 center=Center();float r=ringDrag?ringRadius:Radius();Color old=GUI.color;GUI.color=new Color(.8f,.95f,.9f,.8f);for(int k=0;k<64;k++){float a=k*Mathf.PI/32,b=(k+1)*Mathf.PI/32;ScreenLine(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r,center+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*r,1.5f);}
   if(dragging&&!WholeActive&&SnapCandidate(selected,out Vector3 p)&&Vector3.Distance(p,positions[selected])>.004f){GUI.color=new Color(.4f,1,.9f,.65f);for(int i=0;i<8;i++)for(int axis=0;axis<3;axis++){int j=i^(1<<axis);if(j<i)continue;ScreenLine(Corner(i,p),Corner(j,p),2);}}GUI.color=old;
  }
  Vector2 Corner(int bits,Vector3 p){Vector3 v=new Vector3((bits&1)==0?-.5f:.5f,(bits&2)==0?-.5f:.5f,(bits&4)==0?-.5f:.5f);return owner.view.WorldToScreenPoint(transform.TransformPoint(p+rotations[selected]*Vector3.Scale(v,sizes[selected])));}
  static void ScreenLine(Vector2 a,Vector2 b,float width){a.y=Screen.height-a.y;b.y=Screen.height-b.y;Matrix4x4 m=GUI.matrix;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y-width*.5f,Vector2.Distance(a,b),width),Texture2D.whiteTexture);GUI.matrix=m;}
  void RefreshFeedback()
  {
   float pulse=.55f+.25f*Mathf.Sin(Time.unscaledTime*3);
   Paint(torso.GetComponent<Renderer>(),new Color(.62f,.64f,.60f),new Color(.05f,.15f,.13f)*pulse);
   for(int i=0;i<6;i++)
   {Color c=connected[i]?new Color(.30f,.77f,.65f):selected==i?new Color(.91f,.59f,.25f):new Color(.57f,.58f,.58f);foreach(var r in parts[i].GetComponentsInChildren<Renderer>())Paint(r,c,connected[i]?c*pulse*.65f:Color.black);}
  }
  void Paint(Renderer r,Color color,Color emission){if(!r)return;if(block==null)block=new MaterialPropertyBlock();block.Clear();block.SetColor("_BaseColor",color);block.SetColor("_EmissionColor",emission);r.SetPropertyBlock(block);}
  public void Drag(Ray ray)
  {
   if(selected<0)return;
   if(torso.Raycast(ray,out RaycastHit hit,30))
   {Vector3 n=transform.InverseTransformDirection(hit.normal).normalized;Vector3 p=transform.InverseTransformPoint(hit.point);Place(selected,p+n*(Support(selected,n)+.002f),rotations[selected]);return;}
   var plane=new Plane(owner.view.transform.forward,transform.position);
   if(plane.Raycast(ray,out float t)){Vector3 p=transform.InverseTransformPoint(ray.GetPoint(t));Place(selected,p,rotations[selected]);}
  }
  float Support(int i,Vector3 n){Vector3 v=Quaternion.Inverse(rotations[i])*n;Vector3 h=sizes[i]*.5f;return Mathf.Abs(v.x)*h.x+Mathf.Abs(v.y)*h.y+Mathf.Abs(v.z)*h.z;}
 }
}
