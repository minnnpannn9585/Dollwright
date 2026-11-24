using System.Collections;
using Dollwright.RetrievalDemo;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Dollwright.Graybox.Tests
{
    public sealed class PaperRouteTests
    {
        [UnityTest] public IEnumerator SwingToWind() => Run(false,false);
        [UnityTest] public IEnumerator UpperDoubleRing() => Run(true,false);
        [UnityTest] public IEnumerator LowerDashAndFold() => Run(true,true);

        [UnityTest]
        public IEnumerator AllFourEntriesLaunchFromHomeAndReturnTheOriginalDoll()
        {
            yield return SceneManager.LoadSceneAsync("Workbench_02_DollAssembly");
            yield return null;
            var scenes=new[]{WindmillCommission.SceneName,PaperRoute.SwingScene,MagneticStage.SceneName,PaperRoute.ForkScene};
            var headPos=new Vector3(0,.562f,0);
            var headTurn=Quaternion.Euler(0,12,0);
            for(int index=0;index<scenes.Length;index++)
            {
                var bench=Object.FindObjectOfType<RetrievalDemoController>();
                if(index==1)bench.Free.Place(0,headPos,headTurn);
                bench.ReturnHome();
                var entries=GameObject.Find("横版委托").GetComponentsInChildren<Button>();
                Assert.AreEqual(4,entries.Length);
                StringAssert.Contains("预装试玩",entries[index].GetComponentInChildren<Text>().text);
                Canvas.ForceUpdateCanvases();
                var rect=(RectTransform)entries[index].transform;
                var pointer=new PointerEventData(EventSystem.current){
                    position=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)),
                    button=PointerEventData.InputButton.Left
                };
                var hits=new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer,hits);
                Assert.IsNotEmpty(hits);
                Assert.AreSame(entries[index],hits[0].gameObject.GetComponentInParent<Button>(),"Menu entry must receive the pointer");
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
                yield return null; yield return null;
                Assert.AreEqual(scenes[index],SceneManager.GetActiveScene().name);
                var route=Object.FindObjectOfType<PaperRoute>();
                if(index==0)Assert.IsNotNull(Object.FindObjectOfType<WindmillCommission>());
                if(index==1)Assert.IsTrue(route.HasHook&&route.HasWing);
                if(index==2)Assert.IsNotNull(route.magnetic);
                if(index==3)Assert.IsTrue(route.fork&&route.HasHook);
                WindmillCommission.BackToBench();
                yield return null; yield return null;
                bench=Object.FindObjectOfType<RetrievalDemoController>();
                Assert.AreEqual(index==0?0:1,System.Array.FindAll(bench.Free.equipped,on=>on).Length);
                if(index>0){
                    Assert.AreEqual(headPos,bench.Free.positions[0]);
                    Assert.Less(Quaternion.Angle(headTurn,bench.Free.rotations[0]),.1f);
                }
            }
        }

        IEnumerator Run(bool fork,bool low)
        {
            yield return SceneManager.LoadSceneAsync("Workbench_02_DollAssembly");
            yield return null;
            var bench=Object.FindObjectOfType<RetrievalDemoController>();
            bench.OpenAssembly();
            bench.Free.Place(0,new Vector3(0,.562f,0),Quaternion.identity);
            bench.warehouse.SelectCategory(FreeAssembly.Face.Back);
            Assert.IsTrue(bench.warehouse.slots[FreeAssembly.Wing].gameObject.activeInHierarchy);
            if(low)
            {
                bench.Free.Place(1,new Vector3(.402f,.15f,0),Quaternion.identity);
                bench.Free.Place(FreeAssembly.Runners,new Vector3(0,-.472f,0),Quaternion.identity);
                bench.Free.Place(FreeAssembly.Pleats,new Vector3(0,0,.212f),Quaternion.identity);
            }
            else
            {
                bench.Free.Place(FreeAssembly.Hook,new Vector3(.432f,.06f,0),Quaternion.identity);
                if(!fork) bench.Free.Place(FreeAssembly.Wing,new Vector3(0,0,.222f),Quaternion.identity);
                bench.Free.Place(5,new Vector3(0,-.602f,0),Quaternion.identity);
            }
            yield return null;
            var button=GameObject.Find(fork?"12 分岔 · 钩手或滑梭＋褶布":"03 → 12 · 吊环手＋滑翔翼").GetComponent<Button>();
            Assert.IsTrue(button.interactable);
            button.onClick.Invoke();
            yield return new WaitForSeconds(.15f);
            var route=Object.FindObjectOfType<PaperRoute>();
            Assert.IsNotNull(route);
            route.enabled=false;
            Assert.AreEqual(!low,route.HasHook);
            Assert.AreEqual(low,route.HasRunner);
            int caught=0;
            bool dashed=false,folded=false,lifted=false;
            var tick=new WaitForFixedUpdate();
            for(int frame=0; frame<650 && !route.Finished; frame++)
            {
                var at=route.body.position;
                float axis=1; bool grab=false, drop=false, boost=false, fabric=false;
                if(low)
                {
                    if(at.x>-2.6f && at.y>1) axis=0;
                    if(!dashed && at.x>-1.99f && at.y<.8f){boost=true; dashed=true;}
                    fabric=at.x>6.2f && at.x<10.85f;
                    folded|=route.Folded;
                }
                else
                {
                    grab=!route.Attached && ((caught==0 && at.x>(fork?-3.15f:-2.97f)) || (fork && caught==1 && at.x>2.0f));
                    drop=route.Attached && route.SwingAngle>(fork?(caught==2?.98f:.80f):.68f);
                    if(fork && at.x>=route.prize.position.x) axis=0;
                    if(!fork && caught>0 && !route.Attached)
                    {
                        fabric=true;
                        if(at.x>3.4f && !lifted) axis=0;
                        lifted|=at.y>5.75f;
                    }
                }
                route.Step(axis,false,grab,drop,boost,fabric);
                if(grab && route.Attached) caught++;
                yield return tick;
            }
            Assert.IsTrue(route.Finished,"at "+route.body.position+" rings "+caught+" dash "+dashed+" folded "+folded+" lifted "+lifted);
            if(low) Assert.IsTrue(dashed && folded);
            else { Assert.AreEqual(fork?2:1,caught); if(!fork) Assert.IsTrue(lifted); }
            if(!fork)
            {
                Assert.IsTrue(route.ContinueToFork());
                yield return new WaitForSeconds(.2f);
                route=Object.FindObjectOfType<PaperRoute>();Assert.IsTrue(route.fork && route.HasHook && route.HasWing);
                Assert.IsTrue(route.UseRefitBlock());
                yield return new WaitForSeconds(.2f);
                bench=Object.FindObjectOfType<RetrievalDemoController>();
                Assert.IsTrue(bench.Free.Complete && bench.Free.connected[FreeAssembly.Hook] && bench.Free.connected[FreeAssembly.Wing]);
                bench.Free.ReturnPart(FreeAssembly.Hook);bench.Free.ReturnPart(FreeAssembly.Wing);bench.Free.ReturnPart(5);
                bench.Free.Place(1,new Vector3(-.402f,.1f,0),Quaternion.identity);
                bench.Free.Place(FreeAssembly.Runners,new Vector3(0,-.472f,0),Quaternion.identity);
                bench.Free.Place(FreeAssembly.Pleats,new Vector3(0,0,.212f),Quaternion.identity);
                yield return null;
                var resume=GameObject.Find("12 分岔 · 钩手或滑梭＋褶布").GetComponent<Button>();
                Assert.IsTrue(resume.interactable);
                Assert.AreEqual("继续 12 · 从中转台出发",resume.GetComponentInChildren<Text>().text);
                resume.onClick.Invoke();yield return new WaitForSeconds(.2f);
                route=Object.FindObjectOfType<PaperRoute>();Assert.IsTrue(route.fork && route.HasRunner && route.HasPleats && !route.HasHook);
                Assert.Less(Vector2.Distance(route.body.position,route.refitBlock.position),1.65f);
            }
            route.ResetRun();
            Assert.IsFalse(route.Finished);
            Assert.Less(Vector2.Distance(route.body.position,route.startPoint.position),.01f);
            if(low)
            {
                for(int k=0;k<6;k++){route.Step(0,false,false,false,false,false);yield return tick;}
                route.Step(1,false,false,false,true,false);yield return tick;
                route.Step(-1,false,false,false,false,false);
                Assert.Greater(route.body.velocity.x,10,"A reverse input must not turn an ongoing dash");
                route.ResetRun();
                for(int k=0;k<80 && route.body.position.x < -2.68f;k++){route.Step(1,false,false,false,false,false);yield return tick;}
                Assert.Greater(route.body.position.x,-2.70f);
                route.Step(1,true,false,false,false,false);
                Assert.Greater(route.body.velocity.y,6,"A jump just off the platform edge should still register");
                yield return tick;
                float previous=route.body.velocity.y;
                route.Step(1,true,false,false,false,false);
                Assert.LessOrEqual(route.body.velocity.y,previous+.01f,"Coyote time must not grant a second jump");
            }
            WindmillCommission.BackToBench();
            yield return new WaitForSeconds(.2f);
            Assert.IsTrue(Object.FindObjectOfType<RetrievalDemoController>().Free.Complete);
        }
    }
}
