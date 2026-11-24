using System.Collections;
using Dollwright.RetrievalDemo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Dollwright.Graybox.Tests
{
 public sealed class AssemblyMountTests
 {
  [UnityTest] public IEnumerator ClickTrimDragOrbitAndRestore()
  {
   yield return SceneManager.LoadSceneAsync("Workbench_02_DollAssembly");yield return null;
   var bench=Object.FindObjectOfType<RetrievalDemoController>();bench.OpenAssembly();var free=bench.Free;
   void Click(int index){
    bench.warehouse.SelectCategory(FreeAssembly.DefaultFace(index));
    var evt=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
    ExecuteEvents.Execute(bench.warehouse.slots[index].gameObject,evt,ExecuteEvents.pointerClickHandler);
    Assert.AreEqual(index,free.selected);Assert.IsTrue(free.connected[index]);
   }
   Click(FreeAssembly.RightArm);Vector3 first=free.positions[FreeAssembly.RightArm];Assert.AreEqual(FreeAssembly.Face.Right,free.FaceOf(FreeAssembly.RightArm));
   Click(1);Assert.AreEqual(FreeAssembly.Face.Left,free.FaceOf(1));
   Assert.IsTrue(free.Transfer(FreeAssembly.Face.Right));Assert.AreEqual(first,free.positions[FreeAssembly.RightArm]);Assert.IsTrue(free.connected[1]);
   Assert.AreNotEqual(first,free.positions[1]);var prior=free.positions[1];Assert.IsFalse(free.SetAxisValue(0,first.y));Assert.AreEqual(prior,free.positions[1]);
   Assert.IsTrue(free.Transfer(FreeAssembly.Face.Front));yield return null;
   var field=GameObject.Find("Coordinate_0").GetComponent<InputField>();field.onEndEdit.Invoke("-0.100");
   Assert.That(free.positions[1].x,Is.EqualTo(-.1f).Within(.001f));Assert.AreEqual("上下 Y",free.AxisName(1));
   Vector3 tuned=free.positions[1];Click(1);Assert.AreEqual(tuned,free.positions[1]);
   Assert.IsTrue(free.Transfer(FreeAssembly.Face.Front));Assert.AreEqual(tuned,free.positions[1]);
   free.transform.rotation=Quaternion.LookRotation((free.transform.position-bench.view.transform.position).normalized,Vector3.up);
   Physics.SyncTransforms();
   Vector2 screen(Vector3 local)=>bench.view.WorldToScreenPoint(free.transform.TransformPoint(local));
   var from=screen(free.positions[1]);
   free.PointerDown(from);Assert.AreEqual(1,free.selected);
   var center=screen(Vector3.zero);
   free.PointerMove(center,false);free.PointerUp(center,false);Assert.AreEqual(tuned,free.positions[1]);
   Vector2 blank=center+new Vector2(-160,80);var old=free.transform.rotation;
   free.PointerDown(blank);free.PointerMove(blank+new Vector2(-150,0),false);free.PointerUp(blank+new Vector2(-150,0),false);
   Assert.Greater(Quaternion.Angle(old,free.transform.rotation),40);Assert.AreEqual(tuned,free.positions[1]);
   Assert.AreEqual(1,free.selected,"Turning the doll should keep the trim panel selected");
   var to=screen(Vector3.left*.32f);from=screen(free.positions[1]);
   free.PointerDown(from);Assert.AreEqual(1,free.selected);free.PointerMove(to,false);free.PointerUp(to,false);
   Assert.AreEqual(FreeAssembly.Face.Left,free.FaceOf(1));
   foreach(FreeAssembly.Face side in System.Enum.GetValues(typeof(FreeAssembly.Face))){Assert.IsTrue(free.Transfer(side));Assert.AreEqual(side,free.FaceOf(1));}Assert.IsTrue(free.Transfer(FreeAssembly.Face.Left));
   Click(0);Click(5);Click(FreeAssembly.Wing);Assert.AreEqual(FreeAssembly.Face.Back,free.FaceOf(FreeAssembly.Wing));
   Vector3 wing=free.positions[FreeAssembly.Wing];Click(FreeAssembly.Hook);Assert.IsTrue(free.Complete);
   WindmillCommission.Launch(free,PaperRoute.SwingScene);yield return new WaitForSeconds(.2f);
   var route=Object.FindObjectOfType<PaperRoute>();Assert.IsTrue(route.parts[FreeAssembly.RightArm].gameObject.activeSelf);
   WindmillCommission.BackToBench();yield return new WaitForSeconds(.2f);
   free=Object.FindObjectOfType<RetrievalDemoController>().Free;Assert.IsTrue(free.Complete);
   Assert.AreEqual(wing,free.positions[FreeAssembly.Wing]);Assert.IsTrue(free.equipped[FreeAssembly.RightArm]);
  }

  [UnityTest] public IEnumerator WarehouseFiltersAndReturnsDraggedParts()
  {
   yield return SceneManager.LoadSceneAsync("Workbench_02_DollAssembly");yield return null;
   var bench=Object.FindObjectOfType<RetrievalDemoController>();bench.OpenAssembly();yield return null;
   var free=bench.Free;var store=bench.warehouse;
   Vector2 Center(RectTransform rect)=>RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
   void ClickUI(Transform target){
    Canvas.ForceUpdateCanvases();
    var pointer=new PointerEventData(EventSystem.current){position=Center((RectTransform)target),button=PointerEventData.InputButton.Left};
    var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
    Assert.IsNotEmpty(hits);
    Assert.AreEqual(target.gameObject,ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject),
     "Pointer "+pointer.position+" hit "+string.Join(", ",hits.ConvertAll(hit=>hit.gameObject.name+" @ "+hit.depth)));
    ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
   }
   void Category(FreeAssembly.Face face,params int[] expected){
    ClickUI(store.panel.Find("分类_"+FreeAssembly.NameOf(face)));
    for(int i=0;i<store.slots.Length;i++)
     Assert.AreEqual(System.Array.IndexOf(expected,i)>=0,store.slots[i].gameObject.activeInHierarchy,face+" / "+free.names[i]);
   }
   Category(FreeAssembly.Face.Top,0,FreeAssembly.Magnet);
   Category(FreeAssembly.Face.Right,2,FreeAssembly.Hook,FreeAssembly.Weight,FreeAssembly.RightArm);
   Category(FreeAssembly.Face.Bottom,3,4,5,FreeAssembly.Runners,FreeAssembly.Boots);
   Category(FreeAssembly.Face.Front,FreeAssembly.Pleats);
   Category(FreeAssembly.Face.Back,FreeAssembly.Wing);
   Category(FreeAssembly.Face.Left,1);
   yield return null;
   ClickUI(store.slots[1].transform);
   var original=free.positions[1];Assert.IsTrue(free.Transfer(FreeAssembly.Face.Front));Assert.IsTrue(free.SetAxisValue(0,-.1f));
   free.transform.rotation=Quaternion.LookRotation((free.transform.position-bench.view.transform.position).normalized,Vector3.up);
   Physics.SyncTransforms();
   Vector2 Arm()=>bench.view.WorldToScreenPoint(free.parts[1].transform.position);
   free.PointerDown(Arm());free.PointerUp(Arm(),false);Assert.IsTrue(free.equipped[1]);
   Category(FreeAssembly.Face.Back,FreeAssembly.Wing);
   var drop=Center((RectTransform)store.slots[FreeAssembly.Wing].transform);
   free.PointerDown(Arm());yield return new WaitForSecondsRealtime(.26f);
   Assert.IsTrue(free.HoldingPart);
   free.PointerMove(drop,true);free.PointerUp(drop,true);
   Assert.IsFalse(free.equipped[1]);Assert.IsFalse(free.HasSelection);Assert.IsFalse(free.HasArm);
   Assert.IsTrue(store.slots[1].gameObject.activeInHierarchy,"A remounted arm returns to its default category");
   yield return null;
   ClickUI(store.slots[1].transform);Assert.AreEqual(original,free.positions[1]);Assert.AreEqual(Quaternion.identity,free.rotations[1]);
   Assert.IsTrue(free.Transfer(FreeAssembly.Face.Front));
   var header=GameObject.Find("部件仓库").transform;ClickUI(header);Assert.IsFalse(store.panel.gameObject.activeSelf);
   drop=Center((RectTransform)header);free.PointerDown(Arm());free.PointerMove(drop,true);free.PointerUp(drop,true);
   Assert.IsFalse(free.equipped[1]);Assert.IsTrue(store.slots[1].gameObject.activeInHierarchy);
  }
 }
}
