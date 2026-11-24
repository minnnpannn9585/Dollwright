using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dollwright.RetrievalDemo.Editor
{
    public static class PaperRouteBuilder
    {
        const string Folder="Assets/Workbenches/Workbench_02_DollAssembly/Commissions/";
        static Transform Group(string name, Transform parent, Vector3 position)
        {
            var obj=new GameObject(name).transform;
            obj.SetParent(parent,false); obj.localPosition=position; return obj;
        }
        static Material Mat(string name)=>AssetDatabase.LoadAssetAtPath<Material>("Assets/Workbenches/Workbench_02_DollAssembly/Art/Materials/WB2_"+name+".mat");
        static Transform Box(string name, Transform parent,Vector3 position,Vector3 size,string material,bool solid=false)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;
            UnityEngine.Object.DestroyImmediate(obj.GetComponent<BoxCollider>());
            var tr=obj.transform;tr.SetParent(parent,false);tr.localPosition=position;tr.localScale=size;
            obj.GetComponent<Renderer>().sharedMaterial=Mat(material);
            if(solid){obj.layer=8;obj.AddComponent<BoxCollider2D>();}
            return tr;
        }
        static Transform Ring(Transform parent,float x,float y)
        {
            var ring=Group("Spool ring",parent,new Vector3(x,y,0));
            for(int index=0;index<8;index++)
            {
                float angle=index*Mathf.PI/4;
                var link=Box("Brass link",ring,new Vector3(Mathf.Cos(angle)*.26f,Mathf.Sin(angle)*.26f,0),new Vector3(.1f,.24f,.16f),"Brass");
                link.localRotation=Quaternion.Euler(0,0,angle*Mathf.Rad2Deg);
            }
            Box("Suspension",ring,new Vector3(0,1.7f,.22f),new Vector3(.045f,3,.05f),"Cord");
            return ring;
        }
        public static void Build()
        {
            foreach(string name in new[]{PaperRoute.SwingScene,PaperRoute.ForkScene,MagneticStage.SceneName})
            {
                bool fork=name==PaperRoute.ForkScene, magnet=name==MagneticStage.SceneName;
                var path=Folder+name+".unity";
                if(AssetDatabase.LoadAssetAtPath<SceneAsset>(path)) continue;
                var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                var route=new GameObject("Paper Theatre").AddComponent<PaperRoute>();route.fork=fork;
                var stage=Group("Editable stage",route.transform,Vector3.zero);
                void Ground(string title,float left,float right,float bottom,float top,string mat="Wood")
                    =>Box(title,stage,new Vector3((left+right)/2,(bottom+top)/2,0),new Vector3(right-left,top-bottom,1),mat,true);
                route.startPoint=Group("Start",stage,fork?new Vector3(-6,3.14f):new Vector3(-8,.64f));
                if(magnet)
                {
                    route.startPoint.position=new Vector3(-8,2.05f);
                    var track=route.magnetic=route.gameObject.AddComponent<MagneticStage>();
                    track.retryPoint=Group("Transfer checkpoint",stage,new Vector3(-4.35f,2.04f));
                    Ground("Practice and transfer deck",-9.8f,-3.4f,.96f,1.4f);
                    Box("Checkpoint stripe",stage,new Vector3(-4.35f,1.24f,-.52f),new Vector3(.82f,.15f,.03f),"TealPaint");
                    Ground("Solid drop A",1.6f,3.95f,.96f,1.4f);
                    var plate=Box("Scored breakaway board",stage,new Vector3(4.95f,1.18f,0),new Vector3(2,.44f,1),"Paper",true);
                    track.panel=plate.GetComponent<BoxCollider2D>();
                    for(int k=0;k<4;k++)
                    {
                        var crack=Box("Split",plate,new Vector3((k-1.5f)*.17f,0,-.52f),new Vector3(.04f,.9f,.04f),"DarkWood");
                        crack.localRotation=Quaternion.Euler(0,0,k%2==0?24:-24);
                    }
                    Ground("Solid drop B",5.95f,11.6f,.96f,1.4f);
                    Ground("Buried service tunnel",1.6f,11.6f,-2.15f,-1.8f);
                    Ground("Sealed west bulkhead",1.6f,1.95f,-1.8f,1.4f);
                    Ground("End bulkhead",11.25f,11.6f,-1.8f,6);
                    BoxCollider2D Rail(string label,float left,float right,float y)
                    {
                        var beam=Box(label,stage,new Vector3((left+right)*.5f,y,0),new Vector3(right-left,.25f,.52f),"TealPaint");
                        var bounds=beam.gameObject.AddComponent<BoxCollider2D>();bounds.isTrigger=true;beam.gameObject.layer=2;
                        for(float x=left+.25f;x<right;x+=.65f) Box("Magnetic tooth",beam,new Vector3((x-beam.position.x)/(right-left),-.47f,-.31f),new Vector3(.09f/(right-left),.3f,.45f),"Brass");
                        return bounds;
                    }
                    track.rails=new[]{Rail("Practice magnet",-8.7f,-5.07f,3.55f),Rail("Crossing magnet",-4.85f,7.55f,4.17f)};
                    route.rings=Array.Empty<Transform>();
                }
                else if (fork)
                {
                    Ground("Upper start and lookout",-8,-3,2.05f,2.5f);
                    Ground("Lower takeoff",-8,-1.65f,-.55f,0);
                    Ground("Landing and common exit",2.4f,13,-.55f,0);
                    Ground("Upper walk • low passage beneath",7.15f,10.7f,.83f,2.25f,"GreenPaint");
                    Box("Dash takeoff lip",stage,new Vector3(-1.85f,-.10f,-.51f),new Vector3(.38f,.16f,.04f),"Brass");
                    route.rings=new[]{Ring(stage,-.8f,5.9f),Ring(stage,4.35f,6.25f)};
                }
                else
                {
                    Ground("Rehearsal deck and takeoff",-11,-2.65f,-.55f,0);
                    Ground("High bell gallery",5.1f,10,4.05f,4.5f);
                    route.rings=new[]{Ring(stage,-7,3.25f),Ring(stage,.05f,3.65f)};
                    route.wind=Group("Updraft bounds",stage,new Vector3(3.55f,2.65f,.65f));
                    route.wind.localScale=new Vector3(2.9f,7.6f,1);
                    for(int col=0;col<2;col++)for(int row=0;row<4;row++)
                    {
                        var arrow=Group("Folded wind ribbon",route.wind,new Vector3(col*.45f-.22f,row*.22f-.32f,0));
                        for(int sign=-1;sign<=1;sign+=2)
                        {
                            var fold=Box("Fold",arrow,new Vector3(sign*.045f,0,0),new Vector3(.035f,.049f,.06f),"TealPaint");
                            fold.localRotation=Quaternion.Euler(0,0,sign*36);
                        }
                    }
                }
                route.prize=Group(fork?"Paper star":"Wind chime",stage,fork?new Vector3(11.6f,.79f,0):new Vector3(8,5.29f,0));
                for(int index=0;index<(fork?5:3);index++)
                {
                    float a=index*Mathf.PI*.4f;
                    var strip=Box("Fold",route.prize,fork?new Vector3(.21f*Mathf.Sin(a),.21f*Mathf.Cos(a),0):new Vector3((index-1)*.17f,0,0),
                        fork?new Vector3(.16f,.45f,.16f):new Vector3(.095f,.37f+index*.12f,.12f),"Brass");
                    if(fork) strip.localRotation=Quaternion.Euler(0,0,-a*Mathf.Rad2Deg);
                }
                if(magnet){route.prize.name="Clockwork key";route.prize.position=new Vector3(9.65f,-1.14f,0);Box("Key shaft",route.prize,new Vector3(.29f,0,0),new Vector3(.60f,.10f,.14f),"Brass");}
                route.doll=Group("Commission doll",route.transform,route.startPoint.position);
                route.body=route.doll.gameObject.AddComponent<Rigidbody2D>();route.body.freezeRotation=true;route.body.gravityScale=2;
                route.body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
                route.hull=route.doll.gameObject.AddComponent<BoxCollider2D>();route.hull.size=new Vector2(.55f,1.2f);
                route.visual=Group("Assembly",route.doll,Vector3.zero);
                Box("Torso",route.visual,Vector3.zero,FreeAssembly.TorsoSize,"Ivory");
                var positions=new[]{new Vector3(0,.562f),new Vector3(-.402f,.03f),new Vector3(.437f,.06f),new Vector3(-.17f,-.472f),new Vector3(.17f,-.472f),new Vector3(0,-.602f)};
                var sizes=new[]{new Vector3(.4f,.4f,.38f),new Vector3(.16f,.3f,.18f),new Vector3(.23f,.42f,.24f),new Vector3(.28f,.22f,.22f),new Vector3(.28f,.22f,.22f),new Vector3(.48f,.48f,.20f)};
                route.parts=positions.Select((v,i)=>Box(new[]{"Head","Short arm","Spool hook","Runner L","Runner R","Wheel"}[i],route.visual,v,sizes[i],i==0?"Cotton":"TealPaint")).ToArray();
                route.parts[1].gameObject.SetActive(false);route.parts[3].gameObject.SetActive(false);route.parts[4].gameObject.SetActive(false);
                route.fabric=Box("Unfolded sail",route.visual,new Vector3(0,.2f,.18f),new Vector3(1.55f,.44f,.10f),"Fabric");route.fabric.gameObject.SetActive(false);
                route.thread=route.doll.gameObject.AddComponent<LineRenderer>();route.thread.sharedMaterial=Mat("Brass");route.thread.widthMultiplier=.055f;route.thread.positionCount=2;route.thread.useWorldSpace=true;route.thread.enabled=false;
                var camera=Group("Main Camera",null,new Vector3(-4,2.8f,-12)).gameObject.AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=5;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.22f,.26f);
                var light=Group("Daylight",null,Vector3.zero).gameObject.AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(35,-25,0);light.intensity=1.35f;
                if(!EditorSceneManager.SaveScene(scene,path)) throw new Exception("Could not save "+path);
            }
            var forkPath=Folder+PaperRoute.ForkScene+".unity";
            var forkScene=EditorSceneManager.OpenScene(forkPath);
            var forkStage=UnityEngine.Object.FindObjectOfType<PaperRoute>();
            if(!forkStage.refitBlock)
            {
                forkStage.refitBlock=Box("Refit station - R",forkStage.startPoint.parent,forkStage.startPoint.position+new Vector3(-.92f,-.17f,.35f),new Vector3(.82f,.90f,.65f),"TealPaint");
                Box("Bench top",forkStage.refitBlock,new Vector3(0,.48f,0),new Vector3(1.18f,.13f,1.12f),"Wood");
                EditorSceneManager.MarkSceneDirty(forkScene);
                if(!EditorSceneManager.SaveScene(forkScene))throw new Exception("Unable to save refit station");
            }
            var paths=new[]{PaperRoute.SwingScene,PaperRoute.ForkScene,MagneticStage.SceneName}.Select(n=>Folder+n+".unity");
            var scenes=EditorBuildSettings.scenes.ToList();foreach(var path in paths) if(!scenes.Any(s=>s.path==path)) scenes.Add(new EditorBuildSettingsScene(path,true));
            EditorBuildSettings.scenes=scenes.ToArray(); AssetDatabase.SaveAssets();
        }
    }
}
