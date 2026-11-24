using UnityEngine;
using UnityEngine.EventSystems;
namespace Dollwright.RetrievalDemo
{
 public sealed partial class FreeAssembly : MonoBehaviour
 {
  public const int Hook=6, Wing=7, Runners=8, Pleats=9, Weight=10, Magnet=11, Boots=12, RightArm=13, PartCount=14, CoreIndex=PartCount;
  public bool HasHead => connected[0] || connected[Magnet];
  public bool HasArm => connected[1] || connected[RightArm] || connected[2] || connected[Hook] || connected[Weight];
  public bool HasDrive => connected[Boots] || connected[5] || connected[Runners] || (connected[3] && connected[4]);
  public static readonly Vector3 TorsoSize=new Vector3(.64f,.72f,.30f);
  public readonly Vector3[] sizes={new Vector3(.40f,.40f,.38f),new Vector3(.16f,.30f,.18f),new Vector3(.23f,.42f,.24f),new Vector3(.28f,.22f,.22f),new Vector3(.28f,.22f,.22f),new Vector3(.48f,.48f,.20f),new Vector3(.22f,.34f,.2f),new Vector3(.78f,.25f,.14f),new Vector3(.47f,.22f,.3f),new Vector3(.50f,.26f,.12f),new Vector3(.24f,.30f,.24f),new Vector3(.39f,.35f,.29f),new Vector3(.49f,.24f,.32f),new Vector3(.16f,.30f,.18f)};
  public readonly string[] names={"头部","左短臂","伸缩手臂","左腿","右腿","滚轮","吊环手","滑翔翼","滑梭双足","褶布身","配重木手","磁石头","配重靴","右短臂"};
  public Vector3[] positions=new Vector3[PartCount];public Quaternion[] rotations=new Quaternion[PartCount];
  public BoxCollider torso; public BoxCollider[] parts=new BoxCollider[PartCount];
  public bool[] connected=new bool[PartCount];public string[] reasons=new string[PartCount];
  public int selected=-1;public bool dragging;public RetrievalDemoController owner;public RetrievalDoll doll;
  MaterialPropertyBlock block;
  bool rotating;
  public int Count {get{int n=0;foreach(bool b in connected)if(b)n++;return n;}}
  public bool[] equipped=new bool[PartCount];
  public bool Complete {get{if(dragging||rotating||!HasHead||!HasArm||!HasDrive)return false;for(int i=0;i<PartCount;i++)if(equipped[i]&&!connected[i])return false;return true;}}
  public int ActiveArm=>equipped[2]?2:equipped[1]?1:equipped[RightArm]?RightArm:equipped[Hook]?Hook:Weight;
  public void Toggle(int i){if(equipped[i])ReturnPart(i);else Equip(i);}
  void ResetPose(int i){positions[i]=Loose(i);rotations[i]=Quaternion.identity;if(parts[i]){parts[i].transform.localPosition=positions[i];parts[i].transform.localRotation=rotations[i];}}
  public void ReturnPart(int i){
   if(!equipped[i])return;
   CancelGesture();equipped[i]=false;ResetPose(i);parts[i].gameObject.SetActive(false);
   if(selected==i)selected=-1;
   Notice="";Validate();
   if(owner.warehouse)owner.warehouse.ShowReturnedPart(i);
  }
  public void CancelGesture(){dragging=false;rotating=false;pressed=false;hoverFace=-1;}
  Vector3 Loose(int i)=>new Vector3(i<3?-1.12f:1.12f,.65f-(i%3)*.65f,0);
  public void Initialize(RetrievalDemoController controller,RetrievalDoll model){owner=controller;doll=model;ResetParts();}
  public void ResetParts(){for(int i=0;i<PartCount;i++){equipped[i]=false;positions[i]=Loose(i);rotations[i]=Quaternion.identity;}selected=-1;CancelGesture();if(doll)doll.Rebuild(owner.Recipe);}
  public void Build()
  {
   torso=Cube("Fixed_Torso",Vector3.zero,TorsoSize,Quaternion.identity,doll.ivory).GetComponent<BoxCollider>();
   for(int i=0;i<PartCount;i++)
   {
    var go=Cube(names[i],positions[i],sizes[i],rotations[i],doll.teal);parts[i]=go.GetComponent<BoxCollider>();
    if(i>=Hook && i!=RightArm) RouteParts.Shape(go,i);
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
   for(int i=0;i<PartCount;i++)
   {
    if(!equipped[i]){connected[i]=false;reasons[i]="未选用";continue;}
    var p=parts[i];
    bool touching=SurfaceGap(p)<=.018f*scale;
    bool buried=Physics.ComputePenetration(p,p.transform.position,p.transform.rotation,torso,torso.transform.position,torso.transform.rotation,out _,out float d)&&d>.012f*scale;
    bool overlap=false;
    for(int j=0;j<PartCount;j++)if(j!=i&&equipped[j]&&Physics.ComputePenetration(p,p.transform.position,p.transform.rotation,parts[j],parts[j].transform.position,parts[j].transform.rotation,out _,out float pd)&&pd>.012f*scale)overlap=true;
    connected[i]=touching&&!buried&&!overlap;reasons[i]=buried?"不能埋入躯干":overlap?"部件重叠":!touching?"尚未连接躯干":"已连接";
   }
   RefreshFeedback();
  }
  float SurfaceGap(Collider c){Vector3 a=torso.ClosestPoint(c.bounds.center),b=c.ClosestPoint(a);for(int n=0;n<96;n++){Vector3 old=b;a=torso.ClosestPoint(b);b=c.ClosestPoint(a);if((old-b).sqrMagnitude<1e-13f)break;}return Vector3.Distance(a,b);}
  int Pick(Vector2 screen){Ray ray=owner.view.ScreenPointToRay(screen);float distance=float.MaxValue;int hit=-1;if(torso.Raycast(ray,out RaycastHit core,20)){distance=core.distance;hit=CoreIndex;}for(int i=0;i<PartCount;i++)if(equipped[i]&&parts[i].gameObject.activeInHierarchy&&parts[i].Raycast(ray,out RaycastHit h,20)&&h.distance<distance){distance=h.distance;hit=i;}return hit;}
  static void ScreenLine(Vector2 a,Vector2 b,float width){a.y=Screen.height-a.y;b.y=Screen.height-b.y;Matrix4x4 m=GUI.matrix;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y-width*.5f,Vector2.Distance(a,b),width),Texture2D.whiteTexture);GUI.matrix=m;}
  void RefreshFeedback()
  {
   float pulse=.55f+.25f*Mathf.Sin(Time.unscaledTime*3);
   Paint(torso.GetComponent<Renderer>(),new Color(.62f,.64f,.60f),new Color(.05f,.15f,.13f)*pulse);
   for(int i=0;i<PartCount;i++)
   {Color c=connected[i]?new Color(.30f,.77f,.65f):selected==i?new Color(.91f,.59f,.25f):new Color(.57f,.58f,.58f);foreach(var r in parts[i].GetComponentsInChildren<Renderer>())Paint(r,c,connected[i]?c*pulse*.65f:Color.black);}
  }
  void Paint(Renderer r,Color color,Color emission){if(!r)return;if(block==null)block=new MaterialPropertyBlock();block.Clear();block.SetColor("_BaseColor",color);block.SetColor("_EmissionColor",emission);r.SetPropertyBlock(block);}
 }
}
