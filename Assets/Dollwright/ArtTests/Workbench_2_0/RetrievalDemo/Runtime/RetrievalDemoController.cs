using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace Dollwright.RetrievalDemo {
 public sealed class RetrievalDemoController:MonoBehaviour {
  public enum Mode{Workbench,Assembly,Passage,Reach,Complete,Failed}
  public Camera view;public Collider tableTarget;public Transform courseRoot,assemblyAnchor,bell,dock;public RetrievalGate[] gates;public Collider[] walls;public Material ivory,teal,gold,ink;public CommissionSpec commission;
  public Vector3 courseOrigin=new Vector3(30,0,0);
  public Mode State{get;private set;} public DollAssembly Recipe{get;private set;}=DollAssembly.Default;
  public RetrievalDoll Doll{get;private set;} public FreeAssembly Free{get;private set;}
  public int Passed{get;private set;}public bool Retrieved{get;private set;}public bool Ready{get;private set;}
  public float ShoulderAngle{get;private set;}public float ElbowAngle=>0;public float Extension{get;private set;}=.3f;
  public Vector3 Tip=>Doll.ArmPoints(ShoulderAngle,0,Extension)[2];public float LocalX=>Doll.transform.position.x-courseOrigin.x;
  public string LastObstacle{get;private set;}public int Picture{get;private set;}
  public bool Automated=true;public bool layoutSaved;
  Vector3 homePos;Quaternion homeRot,assemblyRotation=WorkbenchProjection.ProjectionFrame;float homeFov;Color homeBg;int homeMask;CameraClearFlags homeClear;
  readonly List<GameObject> pages=new List<GameObject>();Font font;Text status,pictureLabel,failText,rotationMode;Button depart;
  RectTransform leftColumn,canvasBounds;
  readonly Text[] requirementChecks=new Text[3];readonly GameObject[] requirementStrikes=new GameObject[3];
  Color dark=new Color(.08f,.075f,.06f,.8f),paper=new Color(.84f,.81f,.73f);
  public WorkbenchProjection projection;public PartWarehouse warehouse;
  void Start(){Application.runInBackground=true;homePos=view.transform.position;homeRot=view.transform.rotation;homeFov=view.fieldOfView;homeBg=view.backgroundColor;homeMask=view.cullingMask&~(1<<28);view.cullingMask=homeMask;homeClear=view.clearFlags;font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},22);
   State=Mode.Assembly;var g=new GameObject("Commission_Doll");g.layer=29;Doll=g.AddComponent<RetrievalDoll>();Doll.ivory=ivory;Doll.teal=teal;Doll.brass=gold;Doll.ink=ink;
   Free=g.AddComponent<FreeAssembly>();Free.Initialize(this,Doll);MakeUI();projection=gameObject.AddComponent<WorkbenchProjection>();projection.Initialize(this);ReturnHome();Ready=true;
  }
  void Update(){if(!Ready)return;
   if(State==Mode.Workbench){if(Input.GetKeyDown(KeyCode.Return))OpenAssembly();if(Input.GetMouseButtonDown(0)&&!(EventSystem.current&&EventSystem.current.IsPointerOverGameObject())&&tableTarget.Raycast(view.ScreenPointToRay(Input.mousePosition),out _,30))OpenAssembly();}
   else if(State==Mode.Assembly){Free.Tick();if(Input.GetKeyDown(KeyCode.Return))StartPassage();if(Input.GetKeyDown(KeyCode.Tab))NextPicture();if(Input.GetKeyDown(KeyCode.Delete)&&Free.selected>=0&&Free.selected<6)Free.Toggle(Free.selected);if(Input.GetKeyDown(KeyCode.Escape)){Free.CancelGesture();ReturnHome();}
    RefreshRequirements();
    status.text=Free.selected>=0&&Free.selected<6&&!Free.connected[Free.selected]?Free.reasons[Free.selected]:"";depart.interactable=Free.Complete;rotationMode.text=Free.WholeActive?"整体旋转 ⇄":"单件旋转 ⇄";

   }
   else if(State==Mode.Passage){if(Automated){float dt=Mathf.Min(Time.deltaTime,.04f);Move(Vector3.right*commission.forwardSpeed*dt,Axis(KeyCode.Q,KeyCode.E)*60*dt,Axis(KeyCode.R,KeyCode.F)*60*dt);if(State==Mode.Passage&&Passed==gates.Length&&LocalX>=dock.position.x-courseOrigin.x)BeginReach();}if(Input.GetKeyDown(KeyCode.Escape))OpenAssembly();}
   else if(State==Mode.Reach){AdjustArm(ShoulderAngle+Axis(KeyCode.Q,KeyCode.E)*45*Time.deltaTime,0,Extension+Axis(KeyCode.A,KeyCode.D)*.65f*Time.deltaTime);if(Input.GetKeyDown(KeyCode.Space))TryGrab();if(Input.GetKeyDown(KeyCode.Escape))OpenAssembly();}
  }
  float Axis(KeyCode a,KeyCode b)=>(Input.GetKey(b)?1:0)-(Input.GetKey(a)?1:0);
  void LateUpdate(){if(!Ready)return;
   if(State==Mode.Assembly){
    var layout=(RectTransform)leftColumn.parent;
    float margin=26f+(layout.rect.width-canvasBounds.rect.width)*.5f;
    leftColumn.anchoredPosition=new Vector2(margin,0);
   }
   if(State==Mode.Passage||State==Mode.Failed){var focus=courseOrigin+new Vector3(Mathf.Clamp(LocalX+.75f,2.3f,dock.position.x-courseOrigin.x+.8f),1.8f,0);view.transform.position=focus+new Vector3(-3.7f,2.5f,-6);view.transform.LookAt(focus);view.fieldOfView=44;}}
  public void OpenAssembly(){Free.CancelGesture();assemblyAnchor.position=WorkbenchProjection.AssemblyCenter;if(State==Mode.Failed)Picture=Mathf.Min(Passed,gates.Length-1);State=Mode.Assembly;courseRoot.gameObject.SetActive(false);Doll.gameObject.SetActive(true);Doll.transform.SetParent(assemblyAnchor,false);Doll.transform.localPosition=Vector3.zero;Doll.transform.localScale=Vector3.one*.32f;Doll.transform.rotation=assemblyRotation;Doll.Rebuild(Recipe);view.backgroundColor=homeBg;SetAssemblyView(false);Show(1);projection.SetVisible(true);UpdatePicture();RefreshRequirements();}
  public void SetAssemblyView(bool top){view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.055f,.065f,.055f);view.cullingMask=homeMask;view.transform.position=new Vector3(-.55f,1.8f,-3.3f);view.transform.LookAt(new Vector3(.25f,1.55f,.15f));view.fieldOfView=35;}
  public void ReturnHome(){Free.CancelGesture();State=Mode.Workbench;courseRoot.gameObject.SetActive(false);Doll.gameObject.SetActive(false);if(projection)projection.SetVisible(false);view.transform.SetPositionAndRotation(homePos,homeRot);view.fieldOfView=homeFov;view.backgroundColor=homeBg;view.cullingMask=homeMask;view.clearFlags=homeClear;Show(0);}
  public void NextPicture(){Picture=(Picture+1)%commission.apertureImages.Length;UpdatePicture();}
  void UpdatePicture(){pictureLabel.text="卡口图样 "+(Picture+1)+" / "+commission.apertureImages.Length;projection.Refresh();}
  public void StartPassage(){Free.Validate();if(!Free.Complete){status.text="需要头部、手臂，以及两条腿或滚轮；选用件都须连接有效。";return;}assemblyRotation=Doll.transform.rotation;State=Mode.Passage;Passed=0;Retrieved=false;LastObstacle="";bell.gameObject.SetActive(true);courseRoot.gameObject.SetActive(true);projection.SetVisible(false);Doll.transform.SetParent(null);Doll.transform.localScale=Vector3.one;
   // Use the same light-aligned frame for the paper silhouette and actual gate.
   Doll.transform.SetPositionAndRotation(courseOrigin+new Vector3(1,1.8f,0),Quaternion.AngleAxis(-90,Vector3.up)*Quaternion.Inverse(WorkbenchProjection.ProjectionFrame)*assemblyRotation);Doll.Rebuild(Recipe);view.cullingMask=homeMask;view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.025f,.045f,.06f);Show(2);status.text="自动前进 · Q/E 翻转 · R/F 转身 · Esc 返回工作台";Physics.SyncTransforms();}
  public bool Move(Vector3 delta,float tilt=0,float turn=0){if(State!=Mode.Passage)return false;int n=Mathf.Max(1,Mathf.CeilToInt(Mathf.Max(delta.magnitude/.02f,Mathf.Max(Mathf.Abs(tilt),Mathf.Abs(turn))/1f)));
   for(int j=0;j<n;j++){Vector3 p=Doll.transform.position;Quaternion r=Doll.transform.rotation;Doll.transform.SetPositionAndRotation(p+delta/n,Quaternion.AngleAxis(turn/n,Vector3.up)*Quaternion.AngleAxis(tilt/n,Vector3.right)*r);Physics.SyncTransforms();
    if(Doll.Overlaps(walls,out string hit)){LastObstacle=hit;HighlightContact();Doll.transform.SetPositionAndRotation(p,r);Physics.SyncTransforms();State=Mode.Failed;Show(5);failText.text="卡口 "+Mathf.Min(Passed+1,gates.Length)+" · 部件受阻\n组装已保留，返回后调整。";return false;}}
   if(Passed<gates.Length){float tail=float.MaxValue;foreach(var c in Doll.pieces)if(c&&c.enabled&&c.gameObject.activeInHierarchy)tail=Mathf.Min(tail,c.bounds.min.x);if(tail>gates[Passed].transform.position.x+.16f){Passed++;status.text=Passed==gates.Length?"窄口已通过 · 正在接近目标":"准备下一个卡口 · Q/E 翻转 · R/F 转身";}}return true;}
  void HighlightContact(){foreach(var c in Doll.pieces)if(c&&c.enabled&&c.gameObject.activeInHierarchy)foreach(var w in walls)if(w&&w.enabled&&Physics.ComputePenetration(c,c.transform.position,c.transform.rotation,w,w.transform.position,w.transform.rotation,out _,out float d)&&d>.001f){var b=new MaterialPropertyBlock();b.SetColor("_BaseColor",new Color(1,.25f,.08f));b.SetColor("_EmissionColor",new Color(.6f,.08f,0));foreach(var r in c.GetComponentsInChildren<Renderer>())r.SetPropertyBlock(b);}}
  public bool BeginReach(){if(Passed<gates.Length)return false;State=Mode.Reach;Recipe=new DollAssembly{arm=0};Doll.recipe=Recipe;Doll.transform.SetPositionAndRotation(dock.position,Quaternion.identity);ShoulderAngle=0;Extension=.3f;Doll.SetArm(0,0,.3f,false);Show(3);view.transform.position=courseOrigin+new Vector3(dock.position.x-courseOrigin.x+1,3.0f,-4.8f);view.transform.LookAt(courseOrigin+new Vector3(dock.position.x-courseOrigin.x+1.2f,1.7f,0));view.fieldOfView=40;status.text="Q/E 调整手臂方向 · A/D 收回/伸长 · 空格抓取";return true;}
  public bool ArmIsClear(float s,float e,float length){var p=Doll.ArmPoints(s,0,length);return !Physics.CheckCapsule(p[0],p[1],RetrievalDoll.ArmRadius,1<<30,QueryTriggerInteraction.Ignore);}
  public bool AdjustArm(float s,float e,float length){if(State!=Mode.Reach)return false;s=Mathf.Clamp(s,-85,85);length=Mathf.Clamp(length,.3f,Free.equipped[2]?1.9f:.45f);int n=Mathf.Max(1,Mathf.CeilToInt(Mathf.Max(Mathf.Abs(s-ShoulderAngle),Mathf.Abs(length-Extension)*100)));float initialS=ShoulderAngle,initialL=Extension;for(int i=1;i<=n;i++){float a=Mathf.Lerp(initialS,s,i/(float)n),l=Mathf.Lerp(initialL,length,i/(float)n);if(!ArmIsClear(a,0,l))break;ShoulderAngle=a;Extension=l;}Doll.SetArm(ShoulderAngle,0,Extension,false);return true;}
  public bool TryGrab(){if(State!=Mode.Reach||Vector3.Distance(Tip,bell.position)>.16f||!ArmIsClear(ShoulderAngle,0,Extension))return false;Retrieved=true;bell.gameObject.SetActive(false);State=Mode.Complete;Show(4);status.text="委托完成";return true;}
  void MakeUI(){var go=new GameObject("Scan_Commission_UI",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var ca=go.GetComponent<Canvas>();ca.renderMode=RenderMode.ScreenSpaceOverlay;ca.sortingOrder=30;var sc=go.GetComponent<CanvasScaler>();sc.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;sc.referenceResolution=new Vector2(1440,900);sc.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
   var frame=new GameObject("Layout",typeof(RectTransform));frame.transform.SetParent(go.transform,false);var fr=(RectTransform)frame.transform;fr.anchorMin=fr.anchorMax=fr.pivot=new Vector2(.5f,.5f);fr.sizeDelta=new Vector2(1440,900);
   if(!EventSystem.current)new GameObject("Input",typeof(EventSystem),typeof(StandaloneInputModule));
   for(int i=0;i<6;i++){var p=new GameObject(((Mode)i).ToString(),typeof(RectTransform));p.transform.SetParent(frame.transform,false);var rt=(RectTransform)p.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;pages.Add(p);}
   Label(pages[0].transform,"深缝里的铃铛",30,25,400,40,20);Button(pages[0].transform,"开始组装 →",1230,795,170,40,OpenAssembly);
   var a=pages[1].transform;
   leftColumn=new GameObject("Workbench_Left_Column",typeof(RectTransform)).GetComponent<RectTransform>();
   leftColumn.SetParent(a,false);
   leftColumn.anchorMin=leftColumn.anchorMax=leftColumn.pivot=new Vector2(0,1);
   canvasBounds=ca.GetComponent<RectTransform>();
   Label(leftColumn,commission.brief,0,35,360,55,15);
   warehouse=gameObject.AddComponent<PartWarehouse>();
   warehouse.Initialize(this,leftColumn,font);
   Button(leftColumn,"部件仓库",0,189,115,32,warehouse.Toggle);
   MakeRequirements(a);pictureLabel=Label(a,"",950,120,230,30,16);
   rotationMode=Button(a,"单件旋转 ⇄",430,790,150,36,()=>Free.ToggleWhole()).GetComponentInChildren<Text>();
   Button(a,"整体复位",600,790,120,36,()=>Doll.transform.rotation=WorkbenchProjection.ProjectionFrame);Button(a,"部件复位",740,790,120,36,()=>Free.ResetSelected());
   Button(a,"下一张 →",1010,740,135,36,NextPicture);Button(a,"返回",1300,30,100,36,ReturnHome);depart=Button(a,"进入关卡 →",1230,795,170,40,StartPassage);depart.name="进入关卡";depart.gameObject.SetActive(false);
   Label(pages[2].transform,"探测重建 · 自动前进",30,25,600,45,23);
   Button(pages[2].transform,"召回工作台",1150,30,250,45,OpenAssembly);
   Label(pages[3].transform,"取回铃铛",30,25,600,45,23);Button(pages[3].transform,"抓取 · 空格",1110,795,290,52,()=>TryGrab());Button(pages[3].transform,"返回修改",1110,30,290,45,OpenAssembly);
   Box(pages[4].transform,"Success",450,280,540,290,dark);Label(pages[4].transform,"铃铛取回来了",490,320,460,60,30);Button(pages[4].transform,"回工作台",490,450,460,55,OpenAssembly);
   Box(pages[5].transform,"Failure",30,180,360,280,dark);failText=Label(pages[5].transform,"",55,205,310,125,22);Button(pages[5].transform,"回工作台调整",55,370,310,55,OpenAssembly);
   // Keep overlay text legible as the window brightens behind the assembly camera.
   foreach(var page in new[]{pages[0],pages[1]}){
    foreach(var label in page.GetComponentsInChildren<Text>(true)){
     var shadow=label.gameObject.AddComponent<Shadow>();
     shadow.effectColor=new Color(.09f,.065f,.04f,.85f);
     shadow.effectDistance=new Vector2(1.15f,-1.4f);
    }
   }
   status=Label(frame.transform,"",435,835,700,30,14);
  }
  void MakeRequirements(Transform parent){string[] titles={"一个头部","至少一条手臂","一种前进方式"};for(int i=0;i<3;i++){
   float y=175+i*35;requirementChecks[i]=Label(parent,"",1200,y,25,30,16);requirementChecks[i].name="Requirement_Check_"+i;
   var label=Label(parent,titles[i],1230,y,180,30,16);label.name="AssemblyRequirement_"+i;
   requirementStrikes[i]=Box(label.transform,"Completed_Strike",0,15,label.preferredWidth,1,paper);requirementStrikes[i].SetActive(false);
  }}
  void RefreshRequirements(){bool head=Free.connected[0],arm=Free.connected[1]||Free.connected[2],move=Free.connected[5]||(Free.connected[3]&&Free.connected[4]);
   SetRequirement(0,head);SetRequirement(1,arm);SetRequirement(2,move);depart.gameObject.SetActive(head&&arm&&move);depart.interactable=Free.Complete;
  }
  void SetRequirement(int i,bool done){requirementChecks[i].text=done?"✓":"";requirementStrikes[i].SetActive(done);}
  GameObject Box(Transform p,string name,float x,float y,float w,float h,Color c){var g=new GameObject(name,typeof(RectTransform),typeof(Image));g.transform.SetParent(p,false);var rt=(RectTransform)g.transform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);g.GetComponent<Image>().color=c;g.GetComponent<Image>().raycastTarget=false;return g;}
  Text Label(Transform p,string s,float x,float y,float w,float h,int size){var g=Box(p,"Text",x,y,w,h,Color.clear);DestroyImmediate(g.GetComponent<Image>());var t=g.AddComponent<Text>();t.font=font;t.text=s;t.color=paper;t.fontSize=size;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;return t;}
  Button Button(Transform p,string s,float x,float y,float w,float h,Action action){var g=Box(p,s,x,y,w,h,Color.clear);g.GetComponent<Image>().raycastTarget=true;var b=g.AddComponent<Button>();b.onClick.AddListener(()=>action());var label=Label(g.transform,s,0,0,w,h,16);b.targetGraphic=label;var colors=b.colors;colors.highlightedColor=new Color(1,.93f,.76f);colors.pressedColor=new Color(.72f,.65f,.51f);colors.disabledColor=new Color(.5f,.5f,.5f,.5f);b.colors=colors;return b;}
  void Show(int i){for(int j=0;j<pages.Count;j++)pages[j].SetActive(i==j);if(status)status.text="";}
  void OnDestroy(){if(Doll)Destroy(Doll.gameObject);}
 }
}
