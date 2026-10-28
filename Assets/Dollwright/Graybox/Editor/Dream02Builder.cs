using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dollwright.Graybox.Editor
{
    public static partial class Dream02Builder
    {
        public const string Entry = GrayboxBuilder.Root + "/Scenes/Dream02_MagicBox.unity";
        public const string Stage = GrayboxBuilder.Root + "/Scenes/Dream02_MagicStage.unity";
        static Sprite square,circle;
        static Material material;
        static PhysicsMaterial2D friction;
        static Transform geometry,decor,items;
        static GrayboxPlayer2D player;
        static readonly Color Gray = new Color(.48f,.53f,.56f), Blue = new Color(.32f,.47f,.77f), Yellow = new Color(.98f,.84f,.25f);
        [MenuItem("Dollwright/Graybox/Create Dream 02")]
        public static void Create() { Build(false); }
        [MenuItem("Dollwright/Graybox/Open Dream 02 - Magic Box")]
        public static void OpenEntry() { if (!EditorApplication.isPlaying && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Entry); }
        [MenuItem("Dollwright/Graybox/Open Dream 02 - Magic Stage")]
        public static void OpenStage() { if (!EditorApplication.isPlaying && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(Stage); }
        public static void Build(bool overwrite)
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
            if(!overwrite && (File.Exists(Entry)||File.Exists(Stage))) throw new InvalidOperationException("Dream 02 already exists; preserve manual scene edits.");
            if(!EditorSceneManager.SaveOpenScenes()) return;
            string backup="Logs/DollwrightGraybox/BeforeDream02-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
            foreach(var path in new[]{DreamTravel.WorkbenchScene, Entry,Stage,"ProjectSettings/EditorBuildSettings.asset"})
                if(File.Exists(path))File.Copy(path,Path.Combine(backup,Path.GetFileName(path)));
            square=AssetDatabase.LoadAssetAtPath<Sprite>(GrayboxBuilder.Root+"/Art/Square.png");
            circle=AssetDatabase.LoadAssetAtPath<Sprite>(GrayboxBuilder.Root+"/Art/Circle.png");
            material=AssetDatabase.LoadAssetAtPath<Material>(GrayboxBuilder.Root+"/Art/GrayboxSprite.mat");
            friction=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(GrayboxBuilder.Root+"/Art/PlayerFriction.physicsMaterial2D");
            BuildEntry(); BuildStage(); AddShelfDoll();
            var paths=new[]{Entry,Stage};
            EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(paths.Where(p=>!EditorBuildSettings.scenes.Any(s=>s.path==p)).Select(p=>new EditorBuildSettingsScene(p,true))).ToArray();
            AssetDatabase.SaveAssets(); EditorSceneManager.OpenScene(Entry);
        }
        static void Begin(Vector2 spawn,bool lesson,float maxX)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            geometry=new GameObject("01_CollisionGeometry").transform;
            decor=new GameObject("02_SceneryAndHints").transform;
            items=new GameObject("03_Collectibles").transform;
            var run=new GameObject("06_Dream02Run").AddComponent<GrayboxRun>();
            run.initialSpawn=spawn;run.useCustomHud=true;run.returnToWorkbench=false;
            run.gameObject.AddComponent<Dream02Guide>().entryLesson=lesson;
            var obj=new GameObject("Player_MagicianDoll");obj.transform.position=spawn;
            var rb=obj.AddComponent<Rigidbody2D>();rb.gravityScale=2.8f;rb.constraints=RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation=RigidbodyInterpolation2D.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
            var cap=obj.AddComponent<CapsuleCollider2D>();cap.size=new Vector2(.65f,1.4f);cap.sharedMaterial=friction;
            player=obj.AddComponent<GrayboxPlayer2D>();player.run=run;run.player=player;
            Block(obj.transform,"Body",spawn+new Vector2(0,-.16f),new Vector2(.50f,.8f),Color.white,false,10);
            Block(obj.transform,"Head",spawn+new Vector2(0,.46f),new Vector2(.62f,.62f),Color.white,false,11,circle);
            Block(obj.transform,"HatBrim",spawn+new Vector2(0,.76f),new Vector2(.85f,.10f),Blue,false,12);
            Block(obj.transform,"HatCrown",spawn+new Vector2(0,.97f),new Vector2(.48f,.38f),Blue,false,12);
            player.gameObject.AddComponent<DreamPhaseAbility>();
            var camera=new GameObject("MainCamera_StraightSideView");camera.tag="MainCamera";camera.transform.position=new Vector3(6,3.7f,-10);
            var view=camera.AddComponent<Camera>();view.orthographic=true;view.orthographicSize=5.2f;
            view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=new Color(.9f,.92f,.95f);
            camera.AddComponent<AudioListener>();var follow=camera.AddComponent<GrayboxCamera2D>();follow.target=player.transform;
            follow.worldMinX=-4;follow.worldMaxX=maxX;follow.centerY=3.7f;follow.followHeight=true;follow.verticalOffset=.5f;follow.lookAhead=1.5f;
            var fog=Block(null,"07_Fog_RevealAhead",new Vector2(100,4),new Vector2(200,80),new Color(.19f,.23f,.30f),false,100);
            var reveal=fog.AddComponent<GrayboxFog2D>();reveal.player=player.transform;reveal.revealAhead=5.5f;reveal.revealPushLessons=false;reveal.revealCircusLandmark=false;
        }
        static GameObject Block(Transform parent,string name,Vector2 pos,Vector2 size,Color color,bool solid=true,int order=0,Sprite sprite=null)
        {
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=pos;go.transform.localScale=new Vector3(size.x,size.y,1);
            var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite==null?square:sprite;sr.sharedMaterial=material;sr.color=color;sr.sortingOrder=order;
            if(solid)go.AddComponent<BoxCollider2D>().sharedMaterial=friction;
            return go;
        }
        static void Platform(string name,float left,float right,float top,float depth=.35f)
        {Block(geometry,name,new Vector2((left+right)/2,top-depth/2),new Vector2(right-left,depth),Gray);}
        static void Wall(string name,float x,float bottom,float top,bool phase=false)
        {
            var go=Block(geometry,name,new Vector2(x,(bottom+top)/2),new Vector2(.4f,top-bottom),phase?Yellow:Blue);
            if(phase)go.AddComponent<DreamPhaseBarrier>();
        }
        static void Label(string name,string text,float x,float y,float size=.075f)
        {
            var go=new GameObject(name);go.transform.SetParent(decor);go.transform.position=new Vector3(x,y,-.1f);
            var t=go.AddComponent<TextMesh>();t.text=text;t.fontSize=48;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=new Color(.2f,.26f,.33f);
            t.GetComponent<MeshRenderer>().sortingOrder=20;
        }
        static void Crate(string name,float x,float floor,float min,float max)
        {
            var go=Block(geometry,name,new Vector2(x,floor+.6f),new Vector2(1.2f,1.2f),new Color(.94f,.59f,.25f),true,5);
            var rb=go.AddComponent<Rigidbody2D>();rb.bodyType=RigidbodyType2D.Kinematic;rb.interpolation=RigidbodyInterpolation2D.Interpolate;
            var push=go.AddComponent<GrayboxPushCrate>();push.player=player;push.minX=min;push.maxX=max;
        }
        static void Portal(string name,Vector2 pos,Vector2 size,string path,bool end=false)
        {
            var go=new GameObject(name);go.transform.position=pos;go.transform.SetParent(geometry);
            var c=go.AddComponent<BoxCollider2D>();c.size=size;c.isTrigger=true;
            var p=go.AddComponent<Dream02Portal>();p.nextScene=path;p.prototypeEnd=end;
        }
        static void Pickup(string id,GrayboxPickup.Kind kind,float x,float y)
        {
            var go=Block(items,"Material_"+id,new Vector2(x,y),Vector2.one*.6f,kind==GrayboxPickup.Kind.Base?new Color(.37f,.66f,.56f):new Color(.78f,.50f,.62f),false,10,circle);
            var c=go.AddComponent<CircleCollider2D>();c.radius=.7f;c.isTrigger=true;
            var p=go.AddComponent<GrayboxPickup>();p.materialId=id;p.kind=kind;
            Label(id+"_Label",id,x,y+.65f);
        }
        static void Checkpoint(float x,float floor)
        {
            var go=new GameObject("StageFloor_Checkpoint");go.transform.position=new Vector2(x,floor+1);
            var c=go.AddComponent<BoxCollider2D>();c.size=new Vector2(1,2);c.isTrigger=true;
            var t=go.AddComponent<GrayboxTrigger>();t.action=GrayboxTrigger.Action.Checkpoint;t.checkpoint=new Vector2(x,floor+.76f);t.message="已进入魔术舞台。";
        }
        static void BuildEntry()
        {
            Begin(new Vector2(2,6.96f),true,18);
            Block(decor,"MagicBox_Interior",new Vector2(3,3),new Vector2(6,6),new Color(.78f,.83f,.94f),false,-10);
            Platform("MagicBox_Floor",0,7.5f,0,.5f);Wall("MagicBox_LeftSolid",0,0,6.2f);
            var roof=Block(geometry,"MagicBox_PhaseRoof",new Vector2(3,6.05f),new Vector2(6,.3f),Yellow);roof.AddComponent<DreamPhaseBarrier>();
            Wall("MagicBox_PhaseExit",6,0,6.2f,true);
            Portal("Enter_MagicStage",new Vector2(6.9f,2.5f),new Vector2(.65f,5),Stage);
            Label("RoofPrompt","E : PHASE",2,8.3f);Label("ExitPrompt","E  >  STAGE",4.3f,1.5f);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Entry);
        }
        static void BuildStage()
        {
            Begin(new Vector2(0,.76f),false,34);
            Block(decor,"StageInterior",new Vector2(15.75f,4.5f),new Vector2(16.5f,9),new Color(.79f,.84f,.94f),false,-10);
            Block(decor,"BackstageInterior",new Vector2(26.5f,2.7f),new Vector2(5,5.4f),new Color(.72f,.78f,.89f),false,-10);
            Platform("EntryFloor",-3,3.2f,0,.5f);Wall("EntryLeftBoundary",-3,-3,10);
            Wall("Entry_PhaseGate",3.5f,.65f,4.7f,true);
            Platform("EntryCratePlatform",3.7f,7.3f,1);
            Crate("Entry_PushCrate",5.7f,1,4.5f,6.7f);
            Wall("Stage_LeftSolid",7.5f,-.5f,3.3f);
            Wall("Stage_PhaseEntrance",7.5f,3.3f,8.8f,true);
            Block(geometry,"Stage_SolidCeiling",new Vector2(15.75f,9),new Vector2(16.9f,.4f),Blue);
            Platform("StageFloor",7.7f,29,0,.5f);Wall("Backstage_RightSolid",29,0,6.6f);
            Platform("Platform_A_Start",11.6f,13.8f,3.2f);
            Platform("Platform_B_Middle",15.1f,17.5f,4.9f);
            Platform("Platform_R1_Reward",11,13.5f,6.6f);
            Platform("Platform_F1_Reward",18.8f,28.6f,6.6f);
            // Low roof meets the curtain: no route to the crate's right side from inside.
            Platform("CrateRoof_ReturnRoute",19.7f,24.2f,2.15f,.35f);
            Wall("Stage_PhaseCurtain",24,0,8.8f,true);
            Crate("Puzzle_PushCrate",21.3f,0,8.5f,23.2f);
            Block(decor,"A_SubtleFloorMark",new Vector2(10.3f,.035f),new Vector2(1.3f,.06f),new Color(.68f,.58f,.38f),false,3);
            Label("A_FloorHint","A",10.3f,.24f,.045f);
            Pickup("R1",GrayboxPickup.Kind.Base,12.1f,7.35f);Pickup("F1",GrayboxPickup.Kind.Face,20.6f,7.35f);
            Checkpoint(9,0);
            Platform("Continuation_Preview",28.6f,32,6.6f);
            Wall("Prototype_RightBoundary",32.4f,-2,12);
            Portal("Prototype_End",new Vector2(30.5f,7.8f),new Vector2(1,2.4f),"",true);
            Label("StageTitle","MAGIC STAGE",15.6f,8.35f);
            Label("BackstageTitle","BACKSTAGE",26.5f,3.8f);
            Label("ContinueLabel","TO BE CONTINUED",30,9,.065f);
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Stage);
        }
        static void AddShelfDoll()
        {
            EditorSceneManager.OpenScene(DreamTravel.WorkbenchScene);
            var ui=UnityEngine.Object.FindObjectOfType<WorkbenchDreamUI>();
            var doll=AssetDatabase.LoadAssetAtPath<DollDefinition>(GrayboxBuilder.Root+"/Data/Doll02_Magician.asset");
            if(doll==null){doll=ScriptableObject.CreateInstance<DollDefinition>();AssetDatabase.CreateAsset(doll,GrayboxBuilder.Root+"/Data/Doll02_Magician.asset");}
            doll.dollId="02";doll.displayName="人偶 02 · 魔术师（测试）";doll.dreamName="梦境 02 · 魔术舞台";doll.abilities="行走 · 跳跃 · 推箱 · 穿越屏障";doll.dreamScenePath=Entry;EditorUtility.SetDirty(doll);
            var old=GameObject.Find("Doll02_Magician_Test");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
            var model=UnityEngine.Object.Instantiate(ui.dollCollider.gameObject,ui.dollCollider.transform.parent);
            model.name="Doll02_Magician_Test";model.transform.position+=Vector3.left*.81f;
            var ink=AssetDatabase.LoadAssetAtPath<Material>(GrayboxBuilder.Root+"/Art/ShelfDoll_Ink.mat");
            foreach(var spec in new[]{new Vector4(0,1.23f,.55f,.06f),new Vector4(0,1.40f,.35f,.3f)})
            {
                var hat=GameObject.CreatePrimitive(PrimitiveType.Cylinder);hat.name="Magician_Hat";hat.transform.SetParent(model.transform,false);
                hat.transform.localPosition=new Vector3(0,spec.y,0);hat.transform.localScale=new Vector3(spec.z,spec.w/2,spec.z);
                hat.GetComponent<Renderer>().sharedMaterial=ink;UnityEngine.Object.DestroyImmediate(hat.GetComponent<Collider>());
            }
            ui.magicianDoll=doll;ui.magicianCollider=model.GetComponent<Collider>();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }
    }
}
