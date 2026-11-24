using System;
using System.Collections;
using Dollwright.RetrievalDemo;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace Dollwright.Graybox.Tests
{
    public sealed class MagneticRouteTests
    {
        [UnityTest]
        public IEnumerator RailTransferMissRetryBreakAndReturn()
        {
            yield return SceneManager.LoadSceneAsync("Workbench_02_DollAssembly");
            yield return null;
            var bench=Object.FindObjectOfType<RetrievalDemoController>();bench.OpenAssembly();
            var enter=GameObject.Find("08 磁轨落点 · 磁石头＋配重靴").GetComponent<Button>();
            bench.Free.Place(FreeAssembly.Magnet,new Vector3(0,.537f,0),Quaternion.identity);
            bench.Free.Place(1,new Vector3(-.402f,0,0),Quaternion.identity);
            yield return null;
            Assert.IsTrue(enter.interactable);
            StringAssert.Contains("预装试玩",enter.GetComponentInChildren<Text>().text);
            bench.Free.Place(FreeAssembly.Boots,new Vector3(0,-.482f,0),Quaternion.identity);
            yield return null;
            StringAssert.Contains("当前人偶",enter.GetComponentInChildren<Text>().text);
            enter.onClick.Invoke();
            yield return new WaitForSeconds(.18f);
            var route=Object.FindObjectOfType<PaperRoute>();Assert.IsNotNull(route.magnetic);
            route.enabled=false;
            var rail=route.magnetic;
            var tick=new WaitForFixedUpdate();
            IEnumerator Drive(Func<bool> done,float axis=0,bool jump=false,bool grab=false,bool release=false,bool slam=false)
            {
                for(int k=0;k<220;k++)
                {
                    route.Step(axis,k==0 && jump,k==0 && grab,k==0 && release,false,false,k==0 && slam);
                    yield return tick;
                    if(done())yield break;
                }
                Assert.Fail("Route 08 stalled at "+route.body.position+" riding="+rail.Riding+" broken="+rail.PanelOpen);
            }
            yield return Drive(()=>rail.Riding,grab:true);
            yield return Drive(()=>route.body.position.x>=-5.25f,1);
            yield return Drive(()=>rail.AtCheckpoint,1,release:true);
            route.ResetRun();
            yield return Drive(()=>route.body.position.y<2.03f);
            yield return Drive(()=>rail.Riding,jump:true,grab:true);
            yield return Drive(()=>route.body.position.x>=2.6f,1);
            yield return Drive(()=>!rail.Slamming && route.body.position.y<2.05f,slam:true);
            Assert.IsFalse(rail.PanelOpen,"Wrong landing must survive the stomp");
            yield return Drive(()=>rail.Riding,jump:true,grab:true);
            yield return Drive(()=>route.body.position.x>=4.85f,1);
            yield return Drive(()=>route.body.position.y<2.04f,release:true);
            Assert.IsFalse(rail.PanelOpen,"A normal drop must not break the panel");
            yield return Drive(()=>rail.Riding,jump:true,grab:true);
            yield return Drive(()=>rail.PanelOpen,slam:true);
            yield return Drive(()=>route.body.position.y<-.98f);
            yield return Drive(()=>route.Finished,1);
            route.ResetRun();Assert.IsFalse(rail.PanelOpen);Assert.IsFalse(rail.Slamming);
            Assert.Less(Vector2.Distance(route.body.position,rail.retryPoint.position),.05f);
            WindmillCommission.BackToBench();
            yield return new WaitForSeconds(.18f);
            Assert.IsTrue(Object.FindObjectOfType<RetrievalDemoController>().Free.connected[FreeAssembly.Magnet]);
        }
    }
}
