using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Dollwright.RetrievalDemo
{
 public sealed partial class FreeAssembly
 {
  public enum Face { Left, Right, Bottom, Top, Front, Back }
  static readonly string[] faceNames={"左侧","右侧","底部","顶部","前胸","后背"};
  static readonly string[] axes={"左右 X","上下 Y","前后 Z"};
  bool pressed, pieceOrigin, previewFits;
  float pressTime;
  Vector2 pressPoint,lastPoint;
  int hoverFace=-1;
  Vector3 previewPoint;
  public string Notice {get;private set;}
  public bool HasSelection=>selected>=0 && selected<PartCount && equipped[selected];
  public bool HoldingPart=>pressed && pieceOrigin && HasSelection && (dragging || Time.unscaledTime-pressTime>=.22f);
  public static string NameOf(Face f)=>faceNames[(int)f];
  static Vector3 Normal(Face f){Vector3 v=Vector3.zero;v[(int)f/2]=((int)f%2==0?-1:1);return v;}
  public static Face DefaultFace(int i)
  {
   if(i==0 || i==Magnet)return Face.Top;
   if(i==3 || i==4 || i==5 || i==Runners || i==Boots)return Face.Bottom;
   if(i==Wing)return Face.Back;
   if(i==Pleats)return Face.Front;
   return i==1?Face.Left:Face.Right;
  }
  static int AxisOf(Face f,int row){int depth=(int)f/2;return row==0?(depth==0?1:0):(depth==2?1:2);}
  static Quaternion MountRotation(int i,Face face)
  {
   Vector3 from=Normal(DefaultFace(i)),to=Normal(face);
   if(Vector3.Dot(from,to)<-.99f)return Quaternion.AngleAxis(180,Mathf.Abs(from.y)>.5f?Vector3.right:Vector3.up);
   return Quaternion.FromToRotation(from,to);
  }
  Vector3 Extent(int i,Quaternion q)
  {
   Vector3 half=sizes[i]*.5f;
   Vector3 x=q*Vector3.right*half.x,y=q*Vector3.up*half.y,z=q*Vector3.forward*half.z;
   return new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y),Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z));
  }
  public Face FaceOf(int i)
  {
   var ext=Extent(i,rotations[i]); float best=float.MaxValue; Face result=DefaultFace(i);
   for(int k=0;k<6;k++){int a=k/2;float gap=Mathf.Abs(positions[i][a]*Normal((Face)k)[a]-TorsoSize[a]*.5f-ext[a]-.002f);if(gap<best){best=gap;result=(Face)k;}}
   return result;
  }
  public string AxisName(int row)=>axes[AxisOf(FaceOf(selected),row)];
  public float AxisValue(int row)=>positions[selected][AxisOf(FaceOf(selected),row)];
  bool Fits(int i,Vector3 p,Vector3 extent)
  {
   for(int j=0;j<PartCount;j++)if(i!=j && equipped[j]){
    Vector3 diff=positions[j]-p, min=extent+Extent(j,rotations[j]);
    if(Mathf.Abs(diff.x)<min.x+.001f && Mathf.Abs(diff.y)<min.y+.001f && Mathf.Abs(diff.z)<min.z+.001f)return false;
   }
   return true;
  }
  bool FindMount(int i,Face face,bool natural,out Vector3 result)
  {
   int depth=(int)face/2,u=AxisOf(face,0),v=AxisOf(face,1);
   Vector3 extent=Extent(i,MountRotation(i,face)),half=TorsoSize*.5f,normal=Normal(face);
   Vector3 desired=normal*(half[depth]+extent[depth]+.002f);
   if(natural && depth==0)desired.y=.13f;
   if(natural && (i==3 || i==4))desired.x=i==3?-.17f:.17f;
   result=desired;if(Fits(i,result,extent))return true;
   float umax=half[u]-.015f,vmax=half[v]-.015f;
   var us=new List<float>{desired[u],-umax,umax};var vs=new List<float>{desired[v],-vmax,vmax};
   for(int j=0;j<PartCount;j++)if(j!=i && equipped[j]){
    Vector3 gap=extent+Extent(j,rotations[j])+Vector3.one*.003f;
    us.Add(positions[j][u]-gap[u]);us.Add(positions[j][u]+gap[u]);
    vs.Add(positions[j][v]-gap[v]);vs.Add(positions[j][v]+gap[v]);
   }
   float nearest=float.PositiveInfinity;
   foreach(float x in us)foreach(float y in vs){
    if(Mathf.Abs(x)>umax || Mathf.Abs(y)>vmax)continue;
    Vector3 candidate=desired;candidate[u]=x;candidate[v]=y;float dist=(candidate-desired).sqrMagnitude;
    if(dist<nearest && Fits(i,candidate,extent)){nearest=dist;result=candidate;}
   }
   return !float.IsInfinity(nearest);
  }
  public bool Equip(int i)
  {
   CancelGesture();Notice="";
   if(equipped[i]){selected=i;return true;}
   Face f=DefaultFace(i);
   if(!FindMount(i,f,true,out var p)){Notice=NameOf(f)+"空间不足";return false;}
   selected=i;Place(i,p,MountRotation(i,f));return true;
  }
  public bool Transfer(Face f)
  {
   if(!HasSelection)return false;
   if(FaceOf(selected)==f)return true;
   if(!FindMount(selected,f,false,out var p)){Notice=NameOf(f)+"空间不足，已保留原位置";return false;}
   Place(selected,p,MountRotation(selected,f));Notice="";return true;
  }
  public bool SetAxisValue(int row,float value)
  {
   if(!HasSelection || float.IsNaN(value) || float.IsInfinity(value))return false;
   int i=selected,axis=AxisOf(FaceOf(i),row);Vector3 p=positions[i];float limit=TorsoSize[axis]*.5f-.015f;
   p[axis]=Mathf.Clamp(value,-limit,limit);
   if(!Fits(i,p,Extent(i,rotations[i]))){Notice="这里已有零件";return false;}
   Place(i,p,rotations[i]);Notice=Mathf.Abs(value-p[axis])>.0001f?"已到安装面边缘":"";return true;
  }
  public void PointerDown(Vector2 mouse)
  {
   CancelGesture();Notice="";Physics.SyncTransforms();int hit=Pick(mouse);pieceOrigin=hit>=0 && hit<PartCount;if(pieceOrigin)selected=hit;
   pressPoint=lastPoint=mouse;pressTime=Time.unscaledTime;pressed=true;
  }
  public void PointerMove(Vector2 mouse,bool overUI)
  {
   if(!pressed)return;
   if(!dragging && !rotating && Vector2.Distance(mouse,pressPoint)<5)return;
   if(pieceOrigin && HasSelection){
    dragging=true;var ray=owner.view.ScreenPointToRay(mouse);
    if(overUI || !torso.Raycast(ray,out var hit,30)){hoverFace=-1;return;}
    var local=transform.InverseTransformDirection(hit.normal);int axis=Mathf.Abs(local.x)>.7f?0:Mathf.Abs(local.y)>.7f?1:2;
    int next=axis*2+(local[axis]>0?1:0);
    if(hoverFace!=next){hoverFace=next;Face f=(Face)next;if(FaceOf(selected)==f){previewPoint=positions[selected];previewFits=true;}else previewFits=FindMount(selected,f,false,out previewPoint);}
   }else{
    rotating=true;var delta=mouse-lastPoint;
    transform.rotation=Quaternion.AngleAxis(-delta.y*.45f,owner.view.transform.right)*Quaternion.AngleAxis(delta.x*.45f,owner.view.transform.up)*transform.rotation;
   }
   lastPoint=mouse;
  }
  public void PointerUp(Vector2 mouse,bool overUI)
  {
   if(!pressed)return;
   PointerMove(mouse,overUI);
   if(dragging && owner.warehouse.Contains(mouse))ReturnPart(selected);
   else if(dragging && !overUI && hoverFace>=0)Transfer((Face)hoverFace);
   else if(!pieceOrigin && !rotating)selected=-1;
   CancelGesture();
  }
  public void Tick()
  {
   Vector2 mouse=Input.mousePosition;bool ui=EventSystem.current && EventSystem.current.IsPointerOverGameObject();
   if(Input.GetMouseButtonDown(0) && !ui)PointerDown(mouse);
   if(Input.GetMouseButton(0))PointerMove(mouse,ui);
   if(Input.GetMouseButtonUp(0))PointerUp(mouse,ui);
   RefreshFeedback();
  }
  void OnApplicationFocus(bool focus){if(!focus)CancelGesture();}
  void Wire(Vector3 center,Vector3 half,Quaternion orientation,Color color)
  {
   GUI.color=color;
   Vector2 Project(int bits){
    Vector3 offset=Vector3.Scale(half,new Vector3((bits&1)==0?-1:1,(bits&2)==0?-1:1,(bits&4)==0?-1:1));
    return owner.view.WorldToScreenPoint(transform.TransformPoint(center+orientation*offset));
   }
   for(int i=0;i<8;i++)for(int a=0;a<3;a++){int j=i^(1<<a);if(i<j)ScreenLine(Project(i),Project(j),1.5f);}
  }
  void OnGUI()
  {
   if(Event.current.type!=EventType.Repaint || !owner || owner.State!=RetrievalDemoController.Mode.Assembly || !HasSelection)return;
   Color old=GUI.color;
   Wire(positions[selected],sizes[selected]*.5f,rotations[selected],new Color(.98f,.85f,.58f,.93f));
   if(dragging && hoverFace>=0){
    Face face=(Face)hoverFace;int depth=hoverFace/2;var h=TorsoSize*.5f;Vector3 c=Normal(face)*h[depth];h[depth]=0;
    Color ink=previewFits?new Color(.43f,1,.85f):new Color(1,.46f,.35f);
    Wire(c,h,Quaternion.identity,ink);
    if(previewFits)Wire(previewPoint,sizes[selected]*.5f,MountRotation(selected,face),ink);
   }
   GUI.color=old;
  }
 }
}
