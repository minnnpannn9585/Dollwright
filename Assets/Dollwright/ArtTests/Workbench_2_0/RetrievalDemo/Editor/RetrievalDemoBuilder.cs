using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Dollwright.RetrievalDemo.Editor
{
    public static class RetrievalDemoBuilder
    {
        public const string Root="Assets/Dollwright/ArtTests/Workbench_2_0/RetrievalDemo";
        public const string ScenePath=Root+"/Scenes/Workbench_2_0_RetrievalDemo.unity";
        public const string ArtScene="Assets/Dollwright/ArtTests/Workbench_2_0/Workbench_2_0.unity";
        public const string Reports="Logs/RetrievalDemo";
        public const string Output=@"C:/Users/86135/Documents/Codex/2026-09-29/blender-stylized-low-poly-3d-reference/outputs/Warehouse_20261104";
        static readonly Vector3 Origin=new Vector3(30,0,0);
        static List<Collider> walls;
        static Material wall,edge,ground,gold,teal,ivory,ink;

        [MenuItem("Dollwright/Prototypes/Retrieval Demo/Save Workbench Layout")]
        public static void SaveWorkbenchLayout(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");var c=UnityEngine.Object.FindObjectOfType<RetrievalDemoController>();if(!c||c.gameObject.scene.path!=ScenePath)throw new InvalidOperationException("Open the retrieval test scene.");
         if(!c.layoutSaved){var model=GameObject.Find("Workbench_2_0_Art_Model");var children=model.GetComponentsInChildren<Transform>(true);var clue=children.First(t=>t.name=="Clue_Board");var shelf=children.First(t=>t.name=="Doll_Display_Cabinet");
          Bounds old=VisualBounds(clue),cabinet=VisualBounds(shelf);Undo.RecordObject(shelf,"Place display cabinet");shelf.position+=new Vector3(old.center.x-cabinet.center.x,old.min.y-cabinet.min.y,old.max.z-cabinet.max.z);PrefabUtility.RecordPrefabInstancePropertyModifications(shelf);
          Undo.RecordObject(clue,"Place projection board");clue.localScale*=1.12f/old.size.y;clue.rotation=WorkbenchProjection.ProjectionFrame*clue.rotation;clue.position+=WorkbenchProjection.PaperCenter+WorkbenchProjection.ProjectionFrame*Vector3.forward*.10f-VisualBounds(clue).center;PrefabUtility.RecordPrefabInstancePropertyModifications(clue);
          foreach(var r in clue.GetComponentsInChildren<Renderer>(true))if(r.name.StartsWith("Clue_Paper")||r.name.StartsWith("Clue_String")||r.name.StartsWith("Clue_Clip")){Undo.RecordObject(r,"Clear projection board");r.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
          c.layoutSaved=true;EditorUtility.SetDirty(c);
         }
         var beam=GameObject.Find("Lamp_Beam_To_Crafting_Platform");if(beam){Undo.RecordObject(beam,"Remove idle beam");beam.SetActive(false);}
         var lamp=GameObject.Find("Lamp_Warm_Pool");if(lamp){Undo.RecordObject(lamp.GetComponent<Light>(),"Disable idle spotlight");lamp.GetComponent<Light>().enabled=false;}
         EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);AssetDatabase.SaveAssets();
        }
        static Bounds VisualBounds(Transform t){var rs=t.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);return b;}

        [MenuItem("Dollwright/Prototypes/Retrieval Demo/Open Workbench Test")]
        public static void Open()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(ScenePath);EditorApplication.ExecuteMenuItem("Window/General/Game");
        }
        [MenuItem("Dollwright/Prototypes/Retrieval Demo/Build Separate Test Scene")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before building.");
            Directory.CreateDirectory(Root+"/Scenes");Directory.CreateDirectory(Root+"/Materials");Directory.CreateDirectory(Reports);Directory.CreateDirectory(Output);AssetDatabase.Refresh();
            if(SceneManager.GetActiveScene().isDirty)
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Root+"/Scenes/Recovered_BeforeBuild_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
            if(File.Exists(ScenePath))File.Copy(ScenePath,Output+"/Before_Rebuild_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
            // Save the art scene under a new path before adding any game object.
            var scene=EditorSceneManager.OpenScene(ArtScene);
            EditorSceneManager.SaveScene(scene,ScenePath);
            var model=GameObject.Find("Workbench_2_0_Art_Model");
            if(!model)throw new InvalidOperationException("Workbench 2.0 art model missing.");
            var target=model.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name=="Crafting_Platform_Top");
            var inputTarget=new GameObject("Clickable_Crafting_Platform");inputTarget.transform.position=target.bounds.center;
            var click=inputTarget.AddComponent<BoxCollider>();click.size=target.bounds.size+new Vector3(0,.03f,0);
            var anchor=new GameObject("Doll_Assembly_Anchor");anchor.transform.position=new Vector3(-.10f,1.62f,-.22f);
            var controller=new GameObject("Retrieval_Demo_Controller").AddComponent<RetrievalDemoController>();
            controller.commission=MakeCommission();controller.view=Camera.main;controller.tableTarget=click;controller.assemblyAnchor=anchor.transform;controller.courseOrigin=Origin;
            ivory=Material("Doll_Ivory",new Color(.85f,.83f,.72f));teal=Material("Doll_Teal",new Color(.26f,.54f,.51f));gold=Material("Goal_Amber",new Color(.97f,.68f,.22f));ink=Material("Ink",new Color(.08f,.11f,.12f));
            wall=Material("Passage_Wall",new Color(.34f,.44f,.46f));edge=Material("Aperture_Trim",new Color(.70f,.76f,.68f));ground=Material("Passage_Floor",new Color(.20f,.29f,.31f));
            controller.ivory=ivory;controller.teal=teal;controller.gold=gold;controller.ink=ink;
            var root=new GameObject("Retrieval_Course_01");root.transform.position=Origin;controller.courseRoot=root.transform;walls=new List<Collider>();
            // Solid obstacles only; the rest of the space is a sparse scan grid.
            Grid(root.transform);
            controller.gates=new[]{Gate(root.transform,1,4.5f,1.80f,controller.commission.apertures[0].x,controller.commission.apertures[0].y,""),Gate(root.transform,2,10.5f,1.80f,controller.commission.apertures[1].x,controller.commission.apertures[1].y,"")};
            BuildStraightNook(root.transform,controller);
            controller.walls=walls.ToArray();
            var key=new GameObject("Demo_Soft_Light").AddComponent<Light>();key.type=LightType.Spot;key.transform.position=Origin+new Vector3(9,8,-4);key.transform.LookAt(Origin+new Vector3(9,1,0));key.intensity=7;key.range=24;key.spotAngle=145;key.color=new Color(.79f,.88f,1);key.shadows=LightShadows.Soft;
            key.transform.SetParent(root.transform,true);
            root.SetActive(false);
            controller.view.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            File.WriteAllText(Reports+"/build.json","{\"scene\":\""+ScenePath+"\",\"gates\":2,\"wallColliders\":"+walls.Count+",\"originalArtSceneUnchanged\":true}");
            if(!Application.isBatchMode)EditorApplication.ExecuteMenuItem("Window/General/Game");
            Debug.Log("RETRIEVAL_DEMO_BUILT "+ScenePath);
        }
        public static void UpgradeLayout(){if(EditorApplication.isPlaying)throw new Exception("Stop Play first");var c=UnityEngine.Object.FindObjectOfType<RetrievalDemoController>();if(!c)throw new Exception("Open retrieval scene first");var root=c.courseRoot;
         var a=c.gates[0].transform.localPosition;a.x=4.5f;c.gates[0].transform.localPosition=a;a=c.gates[1].transform.localPosition;a.x=10.5f;c.gates[1].transform.localPosition=a;
         float shift=12.4f-c.dock.localPosition.x;foreach(Transform t in root)if(t.name.StartsWith("Pocket_")||t==c.dock||t==c.bell)t.localPosition+=Vector3.right*shift;
         var old=new List<GameObject>();foreach(Transform t in root)if(t.name=="Scan_Grid_Line")old.Add(t.gameObject);foreach(var g in old)UnityEngine.Object.DestroyImmediate(g);Grid(root);EditorSceneManager.MarkSceneDirty(c.gameObject.scene);EditorSceneManager.SaveScene(c.gameObject.scene);AssetDatabase.SaveAssets();
        }
        static CommissionSpec MakeCommission(){string path=Root+"/Commission_01.asset";var spec=AssetDatabase.LoadAssetAtPath<CommissionSpec>(path);if(!spec){spec=ScriptableObject.CreateInstance<CommissionSpec>();AssetDatabase.CreateAsset(spec,path);}Directory.CreateDirectory(Root+"/Pictures");spec.apertureImages=new Texture2D[2];
         for(int k=0;k<2;k++){int n=384;var tex=new Texture2D(n,n,TextureFormat.RGBA32,false);var px=new Color32[n*n];var size=spec.apertures[k];for(int y=0;y<n;y++)for(int x=0;x<n;x++){float across=(x/(float)(n-1)-.5f)*spec.pictureSpan,depth=(y/(float)(n-1)-.5f)*spec.pictureSpan;bool opening=Mathf.Abs(across)<=size.y*.5f&&Mathf.Abs(depth)<=size.x*.5f;px[y*n+x]=opening?new Color32(232,225,201,255):new Color32(106,122,122,255);}tex.SetPixels32(px);tex.Apply();string image=Root+"/Pictures/Aperture_0"+(k+1)+".png";File.WriteAllBytes(image,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);AssetDatabase.ImportAsset(image);var imp=(TextureImporter)AssetImporter.GetAtPath(image);imp.isReadable=true;imp.npotScale=TextureImporterNPOTScale.None;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.SaveAndReimport();spec.apertureImages[k]=AssetDatabase.LoadAssetAtPath<Texture2D>(image);}EditorUtility.SetDirty(spec);return spec;}
        static void Grid(Transform parent){var mat=Material("Scan_Grid",new Color(.12f,.36f,.40f));mat.shader=Shader.Find("Universal Render Pipeline/Unlit");mat.SetColor("_BaseColor",new Color(.12f,.36f,.40f));EditorUtility.SetDirty(mat);for(int x=0;x<=16;x++){Line(parent,mat,new Vector3(x,0,-1.4f),new Vector3(x,0,1.4f));Line(parent,mat,new Vector3(x,0,1.4f),new Vector3(x,3.8f,1.4f));}for(int i=0;i<=7;i++){float y=i*.5f;Line(parent,mat,new Vector3(0,y,1.4f),new Vector3(16,y,1.4f));}for(int i=0;i<6;i++)Line(parent,mat,new Vector3(0,0,-1.25f+i*.5f),new Vector3(16,0,-1.25f+i*.5f));}
        static void Line(Transform parent,Material mat,Vector3 a,Vector3 b){var g=new GameObject("Scan_Grid_Line");g.transform.SetParent(parent,false);var l=g.AddComponent<LineRenderer>();l.useWorldSpace=false;l.positionCount=2;l.SetPositions(new[]{a,b});l.widthMultiplier=.008f;l.sharedMaterial=mat;l.shadowCastingMode=ShadowCastingMode.Off;l.receiveShadows=false;}
        static void BuildStraightNook(Transform p,RetrievalDemoController c){Block(p,"Pocket_Floor",new Vector3(14.1f,1.2f,0),new Vector3(2,.16f,1),wall);Block(p,"Pocket_Ceiling",new Vector3(14.1f,2.3f,0),new Vector3(2,.16f,1),wall);Block(p,"Pocket_Back",new Vector3(14.1f,1.75f,.35f),new Vector3(2,1,.16f),wall);Block(p,"Pocket_End",new Vector3(15.15f,1.75f,0),new Vector3(.12f,1.1f,.85f),wall);
         var d=new GameObject("Arm_Dock");d.transform.SetParent(p,false);d.transform.localPosition=new Vector3(12.4f,1.55f,0);c.dock=d.transform;
         var b=GameObject.CreatePrimitive(PrimitiveType.Sphere);b.name="Bell_Target";b.transform.SetParent(p,false);b.transform.localPosition=new Vector3(14.5f,1.8f,0);b.transform.localScale=Vector3.one*.20f;b.GetComponent<Collider>().enabled=false;b.GetComponent<Renderer>().sharedMaterial=gold;c.bell=b.transform;
        }
        static Material Material(string name,Color color)
        {
            string path=Root+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",.12f);m.SetFloat("_Metallic",0);m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*.24f);EditorUtility.SetDirty(m);return m;
        }
        static GameObject Block(Transform parent,string name,Vector3 pos,Vector3 size,Material material,bool visible=true,bool solid=true)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.layer=30;g.transform.SetParent(parent,false);g.transform.localPosition=pos;g.transform.localScale=size;
            var r=g.GetComponent<Renderer>();r.sharedMaterial=material;r.enabled=visible;
            var c=g.GetComponent<Collider>();c.enabled=solid;if(solid)walls.Add(c);return g;
        }
        static RetrievalGate Gate(Transform parent,int index,float x,float y,float width,float height,string hint)
        {
            var g=new GameObject("Gate_0"+index);g.transform.SetParent(parent,false);g.transform.localPosition=new Vector3(x,0,0);
            var info=g.AddComponent<RetrievalGate>();info.aperture=new Vector2(width,height);info.centerHeight=y;info.hint=hint;
            float bottom=y-height*.5f,top=y+height*.5f;
            Block(g.transform,"Lower_Solid",new Vector3(0,bottom*.5f,0),new Vector3(.24f,bottom,2.8f),wall);
            Block(g.transform,"Upper_Solid",new Vector3(0,(top+3.8f)*.5f,0),new Vector3(.24f,3.8f-top,2.8f),wall);
            float side=(2.8f-width)*.5f;
            Block(g.transform,"Front_Solid",new Vector3(0,y,-(width+side)*.5f),new Vector3(.24f,height,side),wall);
            Block(g.transform,"Back_Solid",new Vector3(0,y,(width+side)*.5f),new Vector3(.24f,height,side),wall);
            Block(g.transform,"Lower_Lip",new Vector3(-.135f,bottom-.025f,0),new Vector3(.05f,.05f,width+.1f),edge,true,false);
            Block(g.transform,"Upper_Lip",new Vector3(-.135f,top+.025f,0),new Vector3(.05f,.05f,width+.1f),edge,true,false);
            Block(g.transform,"Front_Lip",new Vector3(-.135f,y,-width*.5f-.025f),new Vector3(.05f,height,.05f),edge,true,false);
            Block(g.transform,"Back_Lip",new Vector3(-.135f,y,width*.5f+.025f),new Vector3(.05f,height,.05f),edge,true,false);
            info.marker=Block(g.transform,"Checkpoint_Marker",new Vector3(-.155f,3.45f,0),new Vector3(.035f,.14f,.14f),gold,true,false).GetComponent<Renderer>();
            return info;
        }
        static void BuildNook(Transform parent,RetrievalDemoController controller)
        {
            var nook=new GameObject("L_Shaped_Collection_Nook");nook.transform.SetParent(parent,false);
            Block(nook.transform,"Nook_Lower",new Vector3(16.775f,.5f,0),new Vector3(1.35f,1,1.4f),wall);
            Block(nook.transform,"Nook_Inner_Corner",new Vector3(16.325f,2.925f,0),new Vector3(.45f,1.75f,1.4f),wall);
            Block(nook.transform,"Nook_End_Wall",new Vector3(17.35f,2.4f,0),new Vector3(.20f,2.8f,1.4f),wall);
            Block(nook.transform,"Nook_Upper",new Vector3(16.95f,3.225f,0),new Vector3(.60f,1.15f,1.4f),wall);
            // Only a thin arm fits the 0.26 m depth opening, even when the body is rolled flat.
            Block(nook.transform,"Nook_Front_Collision",new Vector3(16.775f,1.825f,-.415f),new Vector3(1.35f,1.65f,.57f),wall,false);
            Block(nook.transform,"Nook_Back",new Vector3(16.775f,1.825f,.415f),new Vector3(1.35f,1.65f,.57f),ground);
            // Front face omitted deliberately: a readable section through the L-shaped slit.
            Block(nook.transform,"Opening_Edge",new Vector3(16.09f,1.525f,-.72f),new Vector3(.03f,1.05f,.03f),gold,true,false);
            var dock=new GameObject("Reach_Dock");dock.transform.SetParent(parent,false);dock.transform.localPosition=new Vector3(15.578f,1.47f,0);controller.dock=dock.transform;
            Block(parent,"Reach_Dock_Marker",new Vector3(15.65f,.025f,0),new Vector3(.65f,.03f,.70f),teal,true,false);
            var bell=GameObject.CreatePrimitive(PrimitiveType.Sphere);bell.name="Commission_Bell";bell.transform.SetParent(parent,false);bell.transform.localPosition=new Vector3(16.89f,2.37f,0);bell.transform.localScale=Vector3.one*.18f;bell.GetComponent<Renderer>().sharedMaterial=gold;bell.GetComponent<Collider>().enabled=false;bell.layer=30;controller.bell=bell.transform;
            var rim=Block(bell.transform,"Bell_Rim",new Vector3(0,-.32f,0),new Vector3(1.10f,.18f,1.10f),gold,true,false);
        }
    }
}


