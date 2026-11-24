using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
namespace Dollwright.RetrievalDemo {
 public sealed class PartWarehouse : MonoBehaviour {
  public RetrievalDemoController owner;public RectTransform panel;
  readonly Camera[] cameras=new Camera[FreeAssembly.PartCount];readonly RenderTexture[] textures=new RenderTexture[FreeAssembly.PartCount];readonly Text[] counts=new Text[FreeAssembly.PartCount];readonly RawImage[] images=new RawImage[FreeAssembly.PartCount];
  GameObject previewRoot;Material[] previewMaterials; public WarehouseSlot[] slots=new WarehouseSlot[FreeAssembly.PartCount];
  static readonly FreeAssembly.Face[] categories={FreeAssembly.Face.Top,FreeAssembly.Face.Left,FreeAssembly.Face.Right,FreeAssembly.Face.Bottom,FreeAssembly.Face.Front,FreeAssembly.Face.Back};
  readonly Image[] tabs=new Image[categories.Length];
  Button header;Text headerLabel;Image background;RawImage dragIcon;
  static readonly Color panelColor=new Color(.12f,.11f,.10f,.32f);
  public void SelectCategory(FreeAssembly.Face face)
  {
   for(int n=0;n<tabs.Length;n++)
    tabs[n].color=categories[n]==face?new Color(.38f,.44f,.36f,.95f):new Color(.18f,.16f,.14f,.8f);
   int cell=0;
   for(int i=0;i<slots.Length;i++){
    bool show=FreeAssembly.DefaultFace(i)==face;
    slots[i].gameObject.SetActive(show);
    if(!show)continue;
    ((RectTransform)slots[i].transform).anchoredPosition=new Vector2((cell%2)*112,-66-(cell/2)*125);
    cell++;
   }
  }
  public bool Contains(Vector2 position){
   return header.gameObject.activeInHierarchy && (RectTransformUtility.RectangleContainsScreenPoint((RectTransform)header.transform,position)
    || (panel.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(panel,position)));
  }
  public void ShowReturnedPart(int index){
   panel.gameObject.SetActive(true);
   SelectCategory(FreeAssembly.DefaultFace(index));
  }
  public void Initialize(RetrievalDemoController c,Transform parent,Font font,Button headerButton){owner=c;
   header=headerButton;headerLabel=(Text)header.targetGraphic;
   panel=Rect(parent,"Warehouse_Page",0,221,220,440);background=panel.gameObject.AddComponent<Image>();background.color=panelColor;
   for(int n=0;n<categories.Length;n++){
    var face=categories[n];string name=FreeAssembly.NameOf(face);
    var tab=Rect(panel,"分类_"+name,n%3*74,2+n/3*31,70,28);
    var fill=tabs[n]=tab.gameObject.AddComponent<Image>();
    var button=tab.gameObject.AddComponent<Button>();button.targetGraphic=fill;button.onClick.AddListener(()=>SelectCategory(face));
    TextAt(tab,name,0,0,70,28,font,14).alignment=TextAnchor.MiddleCenter;
   }
   dragIcon=Rect(parent.GetComponentInParent<Canvas>().transform,"Held_Part_Preview",0,0,58,48).gameObject.AddComponent<RawImage>();
   dragIcon.raycastTarget=false;dragIcon.gameObject.SetActive(false);
   previewRoot=new GameObject("Warehouse_3D_Previews");
   previewMaterials=new Material[3];Material[] original={c.ivory,c.teal,c.ink};for(int i=0;i<3;i++){previewMaterials[i]=new Material(original[i]);if(i!=2)previewMaterials[i].SetColor("_BaseColor",new Color(.68f,.63f,.52f));previewMaterials[i].SetColor("_EmissionColor",i==2?Color.black:new Color(.14f,.12f,.09f));}
   var light=new GameObject("Preview_Light").AddComponent<Light>();light.transform.SetParent(previewRoot.transform);light.type=LightType.Directional;light.intensity=2.2f;light.color=new Color(1,.95f,.84f);light.cullingMask=1<<28;light.transform.rotation=Quaternion.Euler(35,-35,0);light.shadows=LightShadows.None;
   for(int i=0;i<slots.Length;i++){var cell=Rect(panel,"Slot_"+i,0,0,104,122);var cellBg=cell.gameObject.AddComponent<Image>();cellBg.color=new Color(.15f,.14f,.11f,.22f);var raw=Rect(cell,"Live_3D_View",3,3,98,89).gameObject.AddComponent<RawImage>();raw.raycastTarget=false;images[i]=raw;
    TextAt(cell,c.Free.names[i],4,94,83,23,font,13);counts[i]=TextAt(cell,"1",87,94,17,23,font,13);
    var slot=cell.gameObject.AddComponent<WarehouseSlot>();slot.warehouse=this;slot.index=i;slots[i]=slot;
    var stage=new GameObject("Preview_"+i);stage.transform.SetParent(previewRoot.transform);stage.transform.position=new Vector3(200+i*5,100,0);
    var mesh=Instantiate(c.Free.parts[i].gameObject,stage.transform);mesh.name="Actual_Part_Mesh";mesh.SetActive(true);mesh.transform.localPosition=Vector3.zero;mesh.transform.localRotation=Quaternion.identity;mesh.transform.localScale=c.Free.sizes[i];
    foreach(var t in mesh.GetComponentsInChildren<Transform>(true))t.gameObject.layer=28;
    foreach(var collider in mesh.GetComponentsInChildren<Collider>())collider.enabled=false;
    foreach(var r in mesh.GetComponentsInChildren<Renderer>()){r.SetPropertyBlock(null);r.sharedMaterial=r.gameObject.name=="Eye"?previewMaterials[2]:previewMaterials[1];r.shadowCastingMode=ShadowCastingMode.Off;}
    var cam=new GameObject("Preview_Camera").AddComponent<Camera>();cam.transform.SetParent(stage.transform);cam.transform.localPosition=new Vector3(.75f,.55f,-1.7f);cam.transform.LookAt(stage.transform.position);cam.orthographic=true;cam.orthographicSize=.39f;cam.nearClipPlane=.05f;cam.farClipPlane=4;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(0,0,0,0);cam.cullingMask=1<<28;cam.allowHDR=false;
    var rt=new RenderTexture(192,160,16);rt.Create();cam.targetTexture=rt;raw.texture=rt;textures[i]=rt;cameras[i]=cam;
   }
   SelectCategory(FreeAssembly.Face.Top);
  }
  public void Toggle(){panel.gameObject.SetActive(!panel.gameObject.activeSelf);}
  void LateUpdate(){if(!owner)return;
   bool holding=owner.State==RetrievalDemoController.Mode.Assembly && owner.Free.HoldingPart;
   bool overWarehouse=holding && Contains(Input.mousePosition);
   headerLabel.text=overWarehouse?"松开归还":holding?"拖到仓库归还":"部件仓库";
   background.color=overWarehouse?new Color(.29f,.43f,.35f,.7f):panelColor;
   dragIcon.gameObject.SetActive(holding);
   if(holding){
    dragIcon.texture=textures[owner.Free.selected];
    dragIcon.rectTransform.position=Input.mousePosition+new Vector3(12,-12,0);
   }
   for(int i=0;i<slots.Length;i++){
    cameras[i].enabled=slots[i].gameObject.activeInHierarchy || (holding && i==owner.Free.selected);
    counts[i].text=owner.Free.equipped[i]?"✓":"";images[i].color=owner.Free.equipped[i]?new Color(.45f,.5f,.5f,1):Color.white;
   }
  }
  static RectTransform Rect(Transform p,string name,float x,float y,float w,float h){var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(p,false);var rt=(RectTransform)g.transform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);return rt;}
  static Text TextAt(Transform p,string s,float x,float y,float w,float h,Font f,int size){var t=Rect(p,"Label",x,y,w,h).gameObject.AddComponent<Text>();t.font=f;t.text=s;t.fontSize=size;t.color=new Color(.84f,.81f,.73f);t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;return t;}
  void OnDestroy(){if(previewRoot)Destroy(previewRoot);foreach(var t in textures)if(t){t.Release();Destroy(t);}if(previewMaterials!=null)foreach(var m in previewMaterials)if(m)Destroy(m);}
 }
 public sealed class WarehouseSlot:MonoBehaviour,IPointerClickHandler {
  public PartWarehouse warehouse;public int index;
  public void OnPointerClick(PointerEventData e){if(e.button==PointerEventData.InputButton.Left && warehouse.owner.State==RetrievalDemoController.Mode.Assembly)warehouse.owner.Free.Equip(index);}
 }
}
