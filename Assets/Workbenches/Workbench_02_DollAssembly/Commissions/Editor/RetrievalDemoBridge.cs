using System;using System.IO;using System.Collections.Generic;using UnityEngine;using UnityEditor;using UnityEngine.EventSystems;
namespace Dollwright.RetrievalDemo.Editor {
 [InitializeOnLoad]public static class RetrievalDemoBridge {
  [Serializable]class Request{public string id,command,file;}
  static int autoPhase;static double autoStart;static double next;static string pending;static int frames;
  static RetrievalDemoBridge(){EditorApplication.update+=Poll;}
  static void Poll(){if(autoPhase>0){var c=UnityEngine.Object.FindObjectOfType<RetrievalDemoController>();if(c){if(autoPhase==1&&c.Passed==1&&c.LocalX>5.5f){c.Move(Vector3.zero,90);autoPhase=2;}if(c.State==RetrievalDemoController.Mode.Reach){File.AppendAllText(RetrievalDemoBuilder.Output+"/verification.txt","PASS real-time automatic advance reaches retrieval dock; rotation only, no player translation.\n");autoPhase=0;}else if(c.State==RetrievalDemoController.Mode.Failed||EditorApplication.timeSinceStartup-autoStart>90){File.AppendAllText(RetrievalDemoBuilder.Output+"/verification.txt","FAIL automatic route: "+c.LastObstacle+"\n");autoPhase=0;}}}if(pending!=null&&++frames>30){ScreenCapture.CaptureScreenshot(RetrievalDemoBuilder.Output+"/"+pending);pending=null;}if(EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.3;string path=RetrievalDemoBuilder.Reports+"/request.json";if(!File.Exists(path))return;var r=JsonUtility.FromJson<Request>(File.ReadAllText(path));if(r.id==SessionState.GetString("RetrievalDemo.Request",""))return;SessionState.SetString("RetrievalDemo.Request",r.id);
   try{var c=UnityEngine.Object.FindObjectOfType<RetrievalDemoController>();switch(r.command){case "save_workbench":RetrievalDemoBuilder.SaveWorkbenchLayout();break;case "focus_test":FocusTest(c);break;case "home":c.ReturnHome();break;case "layout":RetrievalDemoBuilder.UpgradeLayout();break;case "open":RetrievalDemoBuilder.Open();break;case "play":EditorApplication.isPlaying=true;break;case "stop":EditorApplication.isPlaying=false;break;case "assembly":c.OpenAssembly();break;case "example":Example(c,true);break;case "autotest":Example(c,true);c.StartPassage();c.Automated=true;autoPhase=1;autoStart=EditorApplication.timeSinceStartup;break;case "course":c.StartPassage();c.Automated=false;break;case "auto":c.Automated=true;break;case "top":c.SetAssemblyView(true);break;case "test":Test(c);break;case "requirements_test":RequirementsTest(c);break;case "unified_test":UnifiedTest(c);break;case "controls_test":ControlsTest(c);break;case "warehouse_test":TestWarehouse(c);break;case "capture":pending=Path.GetFileName(r.file);frames=0;EditorApplication.ExecuteMenuItem("Window/General/Game");break;}
   File.WriteAllText(RetrievalDemoBuilder.Reports+"/state.json","{\"id\":\""+r.id+"\",\"result\":\"ok\"}");}catch(Exception e){File.WriteAllText(RetrievalDemoBuilder.Reports+"/state.json",JsonUtility.ToJson(new Failure{id=r.id,error=e.ToString()},true));Debug.LogException(e);}}
  [Serializable]class Failure{public string id,error;}
  static void Check(bool b,string s){if(!b)throw new Exception(s);}
  static void Example(RetrievalDemoController c,bool wheel){c.OpenAssembly();var f=c.Free;f.ResetParts();f.Place(0,new Vector3(0,.562f,0),Quaternion.identity);f.Place(2,new Vector3(.437f,.15f,0),Quaternion.identity);if(wheel)f.Place(5,new Vector3(0,-.602f,0),Quaternion.identity);else{f.Place(3,new Vector3(-.17f,-.472f,0),Quaternion.identity);f.Place(4,new Vector3(.17f,-.472f,0),Quaternion.identity);}f.transform.rotation=WorkbenchProjection.ProjectionFrame*Quaternion.AngleAxis(90,Vector3.up);c.projection.Refresh();}
  static void Travel(RetrievalDemoController c,float x){for(int i=0;i<800&&c.LocalX<x-.005f;i++){Check(c.Move(new Vector3(Mathf.Min(.03f,x-c.LocalX),0,0)),"Collision near "+c.LocalX+": "+c.LastObstacle);}Check(Mathf.Abs(c.LocalX-x)<.02f,"Did not reach target");}
  static void Route(RetrievalDemoController c,bool wheel){Example(c,wheel);c.StartPassage();Check(Quaternion.Angle(c.Doll.transform.rotation,Quaternion.identity)<.1f,"Preview/departure rotation mismatch");Travel(c,5.5f);Check(c.Passed==1,"First gate missing");c.Move(Vector3.zero,90);Travel(c,11.6f);Check(c.Passed==2,"Second gate missing");Travel(c,12.4f);}
  static void FocusTest(RetrievalDemoController c){Check(c.layoutSaved,"Layout was not saved in scene");c.ReturnHome();var board=GameObject.Find("Workbench_2_0_Art_Model").GetComponentsInChildren<Transform>(true);var positions=new Vector3[board.Length];for(int i=0;i<board.Length;i++)positions[i]=board[i].position;
   var light=c.GetComponent<WorkbenchProjection>().transform.Find("Assembly_Projection_Light");Check(light&&!light.gameObject.activeSelf,"Home projection light remains on");Color ambient=RenderSettings.ambientSkyColor;
   var shade=GameObject.Find("Lamp_Bell_Shade").transform;var rest=shade.rotation;
   c.OpenAssembly();c.projection.SendMessage("AdvanceLamp",.29f);
   Check(Quaternion.Angle(rest,shade.rotation)>10,"Shade did not lift");
   Check((light.position-(shade.position-shade.forward*.176f)).sqrMagnitude<.000001f,"Shaft detached from shade");
   c.projection.SendMessage("AdvanceLamp",1f);Check(light.gameObject.activeSelf,"Assembly light missing");Check(RenderSettings.ambientSkyColor.maxColorComponent<ambient.maxColorComponent,"Environment not dimmed");
   Vector3 axis=light.forward,toDoll=(c.Doll.transform.position-light.position).normalized,toPaper=(WorkbenchProjection.PaperCenter-light.position).normalized;Check(Vector3.Dot(axis,toDoll)>.999f&&Vector3.Dot(axis,toPaper)>.999f,"Light/doll/paper are not aligned");
   var picture=GameObject.Find("Workbench_Commission_Picture_And_Shadow");Check(Vector3.Dot(axis,picture.transform.forward)>.999f,"Paper not perpendicular to projection");
   c.ReturnHome();c.projection.SendMessage("AdvanceLamp",1f);Check(Quaternion.Angle(rest,shade.rotation)<.01f,"Lamp did not return to rest");Check(!light.gameObject.activeSelf&&RenderSettings.ambientSkyColor==ambient,"Lighting not restored");for(int i=0;i<board.Length;i++)Check(Vector3.Distance(board[i].position,positions[i])<.0001f,"Runtime rearranged furniture");
   File.WriteAllText(RetrievalDemoBuilder.Output+"/focus-verification.txt","PASS persistent layout; no runtime furniture moves; focus beam only in assembly; source/doll/paper aligned; environment restored on exit.\n");Example(c,true);}
  static void RequirementsTest(RetrievalDemoController c){c.OpenAssembly();var f=c.Free;f.ResetParts();var page=GameObject.Find("Scan_Commission_UI").transform.Find("Layout/Assembly");var button=page.Find("进入关卡").gameObject;
   Action<int,bool> row=(i,done)=>Check(page.Find("AssemblyRequirement_"+i+"/Completed_Strike").gameObject.activeSelf==done,"Wrong task row "+i);
   Action refresh=()=>c.SendMessage("RefreshRequirements");refresh();row(0,false);row(1,false);row(2,false);Check(!button.activeSelf,"Empty assembly shows entry");
   f.Place(0,new Vector3(0,.562f,0),Quaternion.identity);refresh();row(0,true);row(1,false);Check(!button.activeSelf,"Head alone shows entry");
   f.Place(2,new Vector3(.437f,.15f,0),Quaternion.identity);f.Place(3,new Vector3(-.17f,-.472f,0),Quaternion.identity);refresh();row(1,true);row(2,false);Check(!button.activeSelf,"One leg completes locomotion");
   f.Place(4,new Vector3(.17f,-.472f,0),Quaternion.identity);refresh();row(2,true);Check(button.activeSelf&&f.Complete,"Two legs not accepted");
   f.ReturnPart(3);refresh();row(2,false);Check(!button.activeSelf,"Removing leg retains entry");f.ReturnPart(4);f.Place(5,new Vector3(0,-.602f,0),Quaternion.identity);refresh();row(2,true);Check(button.activeSelf&&f.Complete,"Wheel not accepted");
   f.Place(0,new Vector3(0,1.1f,0),Quaternion.identity);refresh();row(0,false);Check(!button.activeSelf,"Disconnected head completes task");
   Example(c,true);refresh();File.WriteAllText(RetrievalDemoBuilder.Output+"/requirements-verification.txt","PASS connected head and arm; single leg incomplete; two legs OR wheel complete; removal/disconnection hides entry; completed rows struck through.\n");}
  static void UnifiedTest(RetrievalDemoController c){Example(c,true);Check(Mathf.Abs(Vector3.Dot(c.view.transform.forward,Vector3.up))<.2f,"Wrong camera angle");ControlsTest(c);}
  static void ControlsTest(RetrievalDemoController c){c.OpenAssembly();var f=c.Free;f.ResetParts();
   Check(f.Equip(FreeAssembly.RightArm),"Default right shoulder");var first=f.positions[FreeAssembly.RightArm];
   Check(f.Equip(1) && f.Transfer(FreeAssembly.Face.Right),"Second arm on right");
   Check(f.positions[FreeAssembly.RightArm]==first && f.connected[1],"Transfer disturbed another piece");
   Check(f.Transfer(FreeAssembly.Face.Back) && f.SetAxisValue(0,.08f),"Two-axis trim failed");
  }
  static void TestWarehouse(RetrievalDemoController c){c.OpenAssembly();c.Free.ResetParts();var f=c.Free;
   var evt=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
   c.warehouse.slots[0].OnPointerClick(evt);Check(f.connected[0] && !f.dragging,"Click should equip directly");
   f.ReturnPart(0);Check(!f.equipped[0],"Return failed");
  }
  static void Test(RetrievalDemoController c){c.Automated=false;var lines=new List<string>();c.OpenAssembly();c.Free.ResetParts();c.StartPassage();Check(c.State==RetrievalDemoController.Mode.Assembly,"Empty model accepted");
   Example(c,true);Check(c.Free.Complete,"Wheel combination invalid");Check(!c.Free.equipped[3]&&!c.Free.equipped[4],"Wheel forces legs");c.Free.Toggle(5);Check(!c.Free.Complete,"No locomotion accepted");c.Free.Place(3,new Vector3(-.17f,-.472f,0),Quaternion.identity);Check(!c.Free.Complete,"One leg accepted");lines.Add("PASS mandatory head + arm + two legs OR wheel.");
   Route(c,true);Check(!c.Free.parts[1].gameObject.activeSelf&&!c.Free.parts[3].gameObject.activeSelf,"Unused parts travel with doll");lines.Add("PASS wheel assembly through two gates; unused parts excluded; preview orientation matches departure.");
   Check(c.BeginReach(),"Dock failed");c.AdjustArm(3.44f,0,1.67f);Check(c.TryGrab(),"Telescopic retrieval failed tip="+c.Tip+" goal="+c.bell.position);lines.Add("PASS telescopic arm retrieves the deep target.");
   Route(c,false);lines.Add("PASS two-leg alternative passes both gates.");
   Example(c,true);c.Free.Toggle(2);c.Free.Place(1,new Vector3(.402f,.15f,0),Quaternion.identity);Check(c.Free.Complete,"Short arm should be selectable");c.StartPassage();Travel(c,5.5f);c.Move(Vector3.zero,90);Travel(c,11.6f);Travel(c,12.4f);c.BeginReach();c.AdjustArm(0,0,2);Check(c.Extension<=.451f&&!c.TryGrab(),"Short arm can retrieve deep target");lines.Add("PASS short arm accepted for assembly but cannot reach deep target.");
   Example(c,true);var old=c.Free.positions[0];c.StartPassage();c.Move(Vector3.zero,90);c.Move(Vector3.right*5);Check(c.State==RetrievalDemoController.Mode.Failed,"Collision did not fail sortie");c.OpenAssembly();Check(c.Free.positions[0]==old&&c.Free.equipped[5],"Failure destroyed assembly");lines.Add("PASS collision failure preserves chosen parts and arrangement.");
   Example(c,true);c.Free.Place(0,Vector3.zero,Quaternion.identity);Check(!c.Free.Complete,"Buried head accepted");lines.Add("PASS invalid connections still block departure.");
   Example(c,true);c.Automated=true;File.WriteAllLines(RetrievalDemoBuilder.Output+"/verification.txt",lines);Debug.Log("SCAN_COMMISSION_TESTS_PASSED");}
 }
}

