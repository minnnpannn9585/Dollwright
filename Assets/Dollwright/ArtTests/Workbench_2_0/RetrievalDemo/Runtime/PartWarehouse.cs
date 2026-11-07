using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
namespace Dollwright.RetrievalDemo {
 public sealed class PartWarehouse : MonoBehaviour {
  public RetrievalDemoController owner;public RectTransform panel;
  readonly GameObject[] stages=new GameObject[6];readonly Camera[] cameras=new Camera[6];readonly RenderTexture[] textures=new RenderTexture[6];readonly Text[] counts=new Text[6];readonly RawImage[] images=new RawImage[6];
  GameObject previewRoot;Material[] previewMaterials; public WarehouseSlot[] slots=new WarehouseSlot[6];
  public bool Contains(Vector2 position)=>panel.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint(panel,position,null);
  public void Initialize(RetrievalDemoController c,Transform parent,Font font){owner=c;
   panel=Rect(parent,"Warehouse_Page",0,224,220,404);var bg=panel.gameObject.AddComponent<Image>();bg.color=Color.clear;bg.raycastTarget=true;
   previewRoot=new GameObject("Warehouse_3D_Previews");
   previewMaterials=new Material[3];Material[] original={c.ivory,c.teal,c.ink};for(int i=0;i<3;i++){previewMaterials[i]=new Material(original[i]);if(i!=2)previewMaterials[i].SetColor("_BaseColor",new Color(.68f,.63f,.52f));previewMaterials[i].SetColor("_EmissionColor",i==2?Color.black:new Color(.14f,.12f,.09f));}
   var light=new GameObject("Preview_Light").AddComponent<Light>();light.transform.SetParent(previewRoot.transform);light.type=LightType.Directional;light.intensity=2.2f;light.color=new Color(1,.95f,.84f);light.cullingMask=1<<28;light.transform.rotation=Quaternion.Euler(35,-35,0);light.shadows=LightShadows.None;
   for(int i=0;i<6;i++){int col=i%2,row=i/2;var cell=Rect(panel,"Slot_"+i,col*112,6+row*132,104,122);var cellBg=cell.gameObject.AddComponent<Image>();cellBg.color=new Color(.15f,.14f,.11f,.22f);var raw=Rect(cell,"Live_3D_View",3,3,98,89).gameObject.AddComponent<RawImage>();raw.raycastTarget=false;images[i]=raw;
    TextAt(cell,c.Free.names[i],4,94,83,23,font,13);counts[i]=TextAt(cell,"1",87,94,17,23,font,13);
    var slot=cell.gameObject.AddComponent<WarehouseSlot>();slot.warehouse=this;slot.index=i;slots[i]=slot;
    var stage=new GameObject("Preview_"+i);stage.transform.SetParent(previewRoot.transform);stage.transform.position=new Vector3(200+i*5,100,0);stages[i]=stage;
    var mesh=Instantiate(c.Free.parts[i].gameObject,stage.transform);mesh.name="Actual_Part_Mesh";mesh.SetActive(true);mesh.transform.localPosition=Vector3.zero;mesh.transform.localRotation=Quaternion.identity;mesh.transform.localScale=c.Free.sizes[i];
    foreach(var t in mesh.GetComponentsInChildren<Transform>(true))t.gameObject.layer=28;
    foreach(var collider in mesh.GetComponentsInChildren<Collider>())collider.enabled=false;
    foreach(var r in mesh.GetComponentsInChildren<Renderer>()){r.SetPropertyBlock(null);r.sharedMaterial=r.gameObject.name=="Eye"?previewMaterials[2]:previewMaterials[1];r.shadowCastingMode=ShadowCastingMode.Off;}
    var cam=new GameObject("Preview_Camera").AddComponent<Camera>();cam.transform.SetParent(stage.transform);cam.transform.localPosition=new Vector3(.75f,.55f,-1.7f);cam.transform.LookAt(stage.transform.position);cam.orthographic=true;cam.orthographicSize=.39f;cam.nearClipPlane=.05f;cam.farClipPlane=4;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(0,0,0,0);cam.cullingMask=1<<28;cam.allowHDR=false;
    var rt=new RenderTexture(192,160,16);rt.Create();cam.targetTexture=rt;raw.texture=rt;textures[i]=rt;cameras[i]=cam;
   }
  }
  public void Toggle(){panel.gameObject.SetActive(!panel.gameObject.activeSelf);}
  void LateUpdate(){if(!owner)return;bool visible=owner.State==RetrievalDemoController.Mode.Assembly&&panel.gameObject.activeInHierarchy;
   for(int i=0;i<6;i++){cameras[i].enabled=visible;counts[i].text=owner.Free.equipped[i]?"✓":"";images[i].color=owner.Free.equipped[i]?new Color(.45f,.5f,.5f,1):Color.white;}
  }
  public void Begin(int index,Vector2 p){if(owner.State!=RetrievalDemoController.Mode.Assembly||owner.Free.equipped[index])return;owner.Free.BeginDrag(index);owner.Free.ContinueDrag(p);}
  static RectTransform Rect(Transform p,string name,float x,float y,float w,float h){var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(p,false);var rt=(RectTransform)g.transform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(x,-y);rt.sizeDelta=new Vector2(w,h);return rt;}
  static Text TextAt(Transform p,string s,float x,float y,float w,float h,Font f,int size){var t=Rect(p,"Label",x,y,w,h).gameObject.AddComponent<Text>();t.font=f;t.text=s;t.fontSize=size;t.color=new Color(.84f,.81f,.73f);t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;return t;}
  void OnDestroy(){if(previewRoot)Destroy(previewRoot);foreach(var t in textures)if(t){t.Release();Destroy(t);}if(previewMaterials!=null)foreach(var m in previewMaterials)if(m)Destroy(m);}
 }
 public sealed class WarehouseSlot:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler,IEndDragHandler {
  public PartWarehouse warehouse;public int index;
  public void OnPointerDown(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)warehouse.Begin(index,e.position);}
  public void OnDrag(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)warehouse.owner.Free.ContinueDrag(e.position);}
  public void OnPointerUp(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)warehouse.owner.Free.EndDrag(e.position);}
  public void OnEndDrag(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)warehouse.owner.Free.EndDrag(e.position);}
 }
}
