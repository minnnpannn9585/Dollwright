using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Dollwright.Workbench2.Editor
{
 public static class Workbench2ArtBuilder
 {
  public const string Root="Assets/Dollwright/ArtTests/Workbench_2_0";
  public const string ScenePath=Root+"/Workbench_2_0.unity";
  public const string Reports="Logs/Workbench_2_0";
  static readonly string Preview=@"C:/Users/86135/Documents/Codex/2026-09-29/blender-stylized-low-poly-3d-reference/outputs/Workbench_2_0_ArtTest";
  static Dictionary<string,Material> mats;
  static Camera camera;
  static int waitFrames;
  static bool exitAfterCapture;
  static string pendingCapture;
  static readonly List<string> errors=new List<string>();

  [MenuItem("Dollwright/Art Tests/Workbench 2.0/Build Separate Art Scene")]
  public static void Build()
  {
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode before rebuilding the art test.");
   if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   Directory.CreateDirectory(Reports);Directory.CreateDirectory(Preview);
   foreach(var dir in new[]{"Materials","Lighting","Prefabs"})Directory.CreateDirectory(Root+"/"+dir);
   AssetDatabase.Refresh();
   var importer=AssetImporter.GetAtPath(Root+"/Models/Workbench_2_0.fbx") as ModelImporter;
   importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
   importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;
   importer.isReadable=true;importer.globalScale=1;importer.useFileScale=true;
   importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
   importer.SaveAndReimport();
   var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
   MakeMaterials();
   var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Models/Workbench_2_0.fbx"));
   PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
   root.name="Workbench_2_0_Art_Model";
   var transforms=root.GetComponentsInChildren<Transform>(true);
   var foot=transforms.First(t=>t.name=="Lamp_Stepped_Foot");
   if(foot.position.x>0)root.transform.rotation=Quaternion.Euler(0,180,0);
   var report=new List<string>();
   foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
   {
    var imported=renderer.sharedMaterials;
    var assigned=imported.Select(m=>mats[MaterialKey(m.name)]).ToArray();
    renderer.sharedMaterials=assigned;renderer.receiveShadows=true;renderer.shadowCastingMode=ShadowCastingMode.On;
    report.Add(renderer.name+" -> "+string.Join(",",assigned.Select(m=>m.name)));
   }
   foreach(var t in transforms)
   {
    if(t.name=="Wood_Station"||t.name=="Pottery_Station"||t.name=="Interaction_Anchors")t.gameObject.SetActive(false);
    if(t.name=="Crafting_Platform")t.localScale=new Vector3(1.15f,1.10f,1.0f);
   }
   // Add only restrained trim detail to the imported art copy. No game behaviours.
   var artDetails=new GameObject("Art_Detail_Trim");artDetails.transform.SetParent(root.transform,false);
   AddTrimDetails(artDetails.transform);
   PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/Workbench_2_0_Art.prefab");
   SetupCamera();
   SetupBackground();
   SetupLighting();
   SetupPostProcessing();
   EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
   File.WriteAllLines(Reports+"/material-assignments.txt",report);
   var probe=UnityEngine.Object.FindObjectOfType<ReflectionProbe>();
   if(probe)
   {
    string reflection=Root+"/Lighting/Workbench_Reflection.exr";
    if(Lightmapping.BakeReflectionProbe(probe,reflection))
    {AssetDatabase.ImportAsset(reflection);probe.mode=ReflectionProbeMode.Custom;probe.customBakedTexture=AssetDatabase.LoadAssetAtPath<Texture>(reflection);}
   }
   EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();
   if(SceneView.lastActiveSceneView){SceneView.lastActiveSceneView.LookAt(new Vector3(0,1.15f,0),camera.transform.rotation,2.6f);SceneView.lastActiveSceneView.sceneLighting=true;}
   Validate();
   Debug.Log("WORKBENCH_2_ART_READY "+ScenePath);
  }

  public static void BuildAndCapture()
  {
   Application.logMessageReceived+=TrackError;
   try{Build();QueueCapture("01_Unity_Workbench_2_0.png",Application.isBatchMode);}
   catch(Exception ex){File.WriteAllText(Reports+"/failure.txt",ex.ToString());Debug.LogException(ex);if(Application.isBatchMode)EditorApplication.Exit(1);}
  }
  public static void Open()
  {
   if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   EditorSceneManager.OpenScene(ScenePath);EditorApplication.ExecuteMenuItem("Window/General/Game");
  }
  [MenuItem("Dollwright/Art Tests/Workbench 2.0/Capture Current Camera")]
  public static void Capture(){QueueCapture("01_Unity_Workbench_2_0.png",false);}
  static void TrackError(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text);}
  static void QueueCapture(string filename,bool exit)
  {
   camera=Camera.main;pendingCapture=filename;exitAfterCapture=exit;waitFrames=0;
   EditorApplication.update-=CaptureTick;EditorApplication.update+=CaptureTick;
  }
  static void CaptureTick()
  {
   if(++waitFrames<35){EditorApplication.QueuePlayerLoopUpdate();return;}
   EditorApplication.update-=CaptureTick;
   try
   {
    RenderToFile(camera,Path.Combine(Preview,pendingCapture),1920,1000);
    Validate();
    File.WriteAllText(Reports+"/capture-complete.json",JsonUtility.ToJson(new CaptureReport{image=Path.Combine(Preview,pendingCapture),errors=errors.ToArray()},true));
    if(exitAfterCapture)EditorApplication.Exit(errors.Count==0?0:1);
   }
   catch(Exception ex){File.WriteAllText(Reports+"/failure.txt",ex.ToString());Debug.LogException(ex);if(exitAfterCapture)EditorApplication.Exit(1);}
  }
  static void RenderToFile(Camera cam,string file,int w,int h)
  {
   var rt=new RenderTexture(w,h,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);rt.antiAliasing=1;rt.Create();
   var old=RenderTexture.active;float aspect=cam.aspect;cam.aspect=(float)w/h;
   var data=cam.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;
   try
   {
    for(int i=0;i<3;i++)RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
    RenderTexture.active=rt;var image=new Texture2D(w,h,TextureFormat.RGB24,false,false);
    image.ReadPixels(new Rect(0,0,w,h),0,0);image.Apply();File.WriteAllBytes(file,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
   }
   finally{RenderTexture.active=old;cam.aspect=aspect;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
  }

  static string MaterialKey(string n){int i=n.IndexOf("WB2_",StringComparison.Ordinal);return i<0?"Wood":n.Substring(i+4).Replace(" (Instance)","");}
  static Color C(float r,float g,float b){return new Color(r,g,b,1);}
  static Texture2D Texture(string name,bool repeat=true)
  {
   string path=Root+"/Textures/"+name+".png";var imp=(TextureImporter)AssetImporter.GetAtPath(path);
   imp.textureType=TextureImporterType.Default;imp.sRGBTexture=true;imp.mipmapEnabled=repeat;
   imp.wrapMode=repeat?TextureWrapMode.Repeat:TextureWrapMode.Clamp;imp.anisoLevel=8;imp.filterMode=FilterMode.Trilinear;
   imp.maxTextureSize=2048;imp.textureCompression=TextureImporterCompression.CompressedHQ;imp.SaveAndReimport();
   return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
  }
  static Material Mat(string key,Color color,Texture tex=null,float smooth=.22f,float metal=0,bool painted=false,Color? paint=null,float coverage=0)
  {
   string path=Root+"/Materials/WB2_"+key+".mat";
   var m=AssetDatabase.LoadAssetAtPath<Material>(path);
   var shader=Shader.Find(painted?"Dollwright/Workbench2/PaintedSurface":"Universal Render Pipeline/Lit");
   if(!m){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}else m.shader=shader;
   m.SetColor("_BaseColor",color);m.SetTexture("_BaseMap",tex);m.SetFloat("_Smoothness",smooth);m.SetFloat("_Metallic",metal);
   if(painted){m.SetColor("_PaintColor",paint??color);m.SetFloat("_PaintCoverage",coverage);}
   EditorUtility.SetDirty(m);mats[key]=m;return m;
  }
  static void MakeMaterials()
  {
   mats=new Dictionary<string,Material>();var wood=Texture("Painted_Walnut");var panel=Texture("Aged_Red_Panel",false);var paper=Texture("Paper_Patterns",false);
   Mat("Wood",C(.61f,.47f,.34f),wood,.28f,0,true);
   Mat("DarkWood",C(.35f,.29f,.23f),wood,.23f,0,true);
   Mat("Recess",C(.30f,.32f,.24f),wood,.10f,0,true,C(.055f,.082f,.055f),.85f);
   Mat("GreenPaint",C(.56f,.46f,.30f),wood,.24f,0,true,C(.10f,.19f,.145f),.82f);
   Mat("TealPaint",C(.66f,.53f,.35f),wood,.30f,0,true,C(.09f,.27f,.25f),.93f);
   Mat("RedPaint",C(.62f,.47f,.30f),wood,.26f,0,true,C(.32f,.055f,.032f),.85f);
   Mat("RedPanel",C(.71f,.70f,.65f),panel,.26f);
   Mat("Brass",C(.69f,.47f,.21f),null,.68f,.84f);
   Mat("Steel",C(.53f,.59f,.56f),null,.55f,.75f);
   Mat("Ink",C(.022f,.037f,.028f),null,.39f,.25f);
   Mat("Ivory",C(.73f,.69f,.53f),wood,.31f,0,true,C(.66f,.59f,.41f),.98f);
   Mat("Ceramic",C(.59f,.58f,.43f),null,.40f);
   Mat("Paper",C(.70f,.65f,.51f),paper,.08f);
   Mat("Fabric",C(.038f,.17f,.18f),null,.12f);
   Mat("Cotton",C(.58f,.49f,.32f),null,.12f);
   Mat("Cord",C(.20f,.14f,.07f),null,.12f);
   Mat("Bristles",C(.055f,.027f,.012f),null,.18f);
   var bulb=Mat("Bulb",C(1,.76f,.36f),null,.25f);bulb.EnableKeyword("_EMISSION");bulb.SetColor("_EmissionColor",C(1,.48f,.12f)*3.5f);
   var bg=new Material(Shader.Find("Dollwright/Workbench2/SoftBackdrop"));bg.SetTexture("_BaseMap",Texture("Workshop_Background",false));bg.SetColor("_BaseColor",C(.78f,.82f,.78f));bg.SetFloat("_Blur",2.2f);
   PutAsset(bg,Root+"/Materials/WB2_Background.mat");mats["Background"]=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/WB2_Background.mat");
  }
  static void PutAsset(UnityEngine.Object obj,string path)
  {
   var current=AssetDatabase.LoadMainAssetAtPath(path);
   if(current){EditorUtility.CopySerialized(obj,current);UnityEngine.Object.DestroyImmediate(obj);EditorUtility.SetDirty(current);}else AssetDatabase.CreateAsset(obj,path);
  }
  static GameObject Primitive(string name,PrimitiveType type,Vector3 p,Vector3 scale,Material mat,Transform parent=null)
  {
   var go=GameObject.CreatePrimitive(type);go.name=name;if(parent)go.transform.SetParent(parent,false);go.transform.position=p;go.transform.localScale=scale;
   UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=mat;return go;
  }
  static void AddTrimDetails(Transform parent)
  {
   // Sparse inlay rings echo the reference's ivory turntable and antique brass fittings.
   for(int row=0;row<2;row++)
   {
    int sides=48;float radius=row==0?.554f:.533f;float width=.004f;
    var v=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
    for(int i=0;i<=sides;i++)
    {
     float a=i*Mathf.PI*2/sides;
     foreach(float r in new[]{radius-width,radius}){v.Add(new Vector3(-.1f+r*Mathf.Cos(a),.957f,-.22f+r*.9565f*Mathf.Sin(a)));uv.Add(new Vector2(i/(float)sides,r));}
     if(i<sides){int q=i*2;triangles.AddRange(new[]{q,q+1,q+2,q+1,q+3,q+2});}
    }
    var mesh=new Mesh{name="Platform_Inlay_Ring_"+row};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
    string path=Root+"/Models/Platform_Inlay_"+row+".asset";PutAsset(mesh,path);
    var go=new GameObject("Platform_Brass_Inlay_"+row);go.transform.SetParent(parent,true);go.transform.position=Vector3.zero;go.transform.rotation=Quaternion.identity;
    go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);go.AddComponent<MeshRenderer>().sharedMaterial=mats["Brass"];
   }
  }
  static void SetupCamera()
  {
   var go=new GameObject("Camera_Workbench_2_0");go.tag="MainCamera";camera=go.AddComponent<Camera>();go.AddComponent<AudioListener>();
   camera.transform.position=new Vector3(.10f,1.98f,-5.40f);camera.transform.LookAt(new Vector3(0,1.24f,0));
   camera.fieldOfView=20.5f;camera.nearClipPlane=.05f;camera.farClipPlane=40;camera.aspect=1.92f;
   camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=C(.035f,.044f,.025f);camera.allowHDR=true;
   var data=go.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=true;data.renderShadows=true;
   data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;data.antialiasingQuality=AntialiasingQuality.High;
  }
  static void SetupBackground()
  {
   var go=Primitive("Background_2D_SoftWorkshop",PrimitiveType.Quad,Vector3.zero,Vector3.one,mats["Background"]);
   go.transform.rotation=camera.transform.rotation;float distance=9;
   go.transform.position=camera.transform.position+camera.transform.forward*distance;
   float h=2*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)*distance*1.04f;
   go.transform.localScale=new Vector3(h*1.92f,h,1);
   var r=go.GetComponent<Renderer>();r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
  }
  static Light MakeLight(string name,LightType type,Vector3 position,Vector3 target,Color color,float intensity,Transform parent)
  {
   var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=position;go.transform.LookAt(target);
   var l=go.AddComponent<Light>();l.type=type;l.color=color;l.intensity=intensity;l.range=8;
   l.shadows=LightShadows.Soft;l.shadowBias=.015f;l.shadowNormalBias=.045f;l.shadowNearPlane=.05f;
   return l;
  }
  static void SetupLighting()
  {
   var root=new GameObject("Lighting_Warm_Window_And_Lamp");
   RenderSettings.ambientMode=AmbientMode.Trilight;
   RenderSettings.ambientSkyColor=C(.29f,.32f,.24f);RenderSettings.ambientEquatorColor=C(.18f,.21f,.14f);RenderSettings.ambientGroundColor=C(.10f,.075f,.045f);
   RenderSettings.ambientIntensity=1;RenderSettings.reflectionIntensity=.85f;RenderSettings.fog=false;
   var sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetColor("_SkyTint",C(.46f,.49f,.35f));sky.SetColor("_GroundColor",C(.14f,.10f,.06f));sky.SetFloat("_Exposure",.8f);
   PutAsset(sky,Root+"/Lighting/Warm_Reflection_Sky.mat");RenderSettings.skybox=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Lighting/Warm_Reflection_Sky.mat");
   var sun=MakeLight("Sun_Left_Window",LightType.Directional,new Vector3(-3.4f,5,-1.8f),new Vector3(.3f,.8f,0),C(1,.83f,.59f),2.1f,root.transform);
   RenderSettings.sun=sun;
   var fill=MakeLight("Soft_Green_Fill",LightType.Directional,new Vector3(2,2.5f,-4),new Vector3(0,1,0),C(.64f,.72f,.63f),.38f,root.transform);fill.shadows=LightShadows.None;
   var bulb=UnityEngine.Object.FindObjectsOfType<Transform>().First(t=>t.name=="Lamp_Bulb");
   var lamp=MakeLight("Lamp_Warm_Pool",LightType.Spot,bulb.position+new Vector3(0,-.09f,0),new Vector3(-.45f,.86f,-.15f),C(1,.72f,.40f),2.4f,root.transform);lamp.spotAngle=92;lamp.innerSpotAngle=52;lamp.range=2.1f;
   var bounce=MakeLight("Desk_Warm_Bounce",LightType.Point,new Vector3(-.6f,1.02f,-.35f),Vector3.zero,C(1,.66f,.32f),.22f,root.transform);bounce.range=1.6f;bounce.shadows=LightShadows.None;
   // Invisible light blockers create the reference's diagonal window shadow bands.
   var gobo=new GameObject("Window_Shadow_Casters_Only");gobo.transform.SetParent(root.transform);
   var basis=Quaternion.LookRotation(-sun.transform.forward,Vector3.up);Vector3 centre=new Vector3(-.2f,.9f,0)-sun.transform.forward*1.9f;
   for(int i=0;i<3;i++)
   {
    var go=Primitive("Window_Mullion_Shadow_"+i,PrimitiveType.Cube,centre+basis*new Vector3((i-1)*.78f,0,0),new Vector3(.055f,3.5f,.025f),mats["DarkWood"],gobo.transform);
    go.transform.rotation=basis;go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.ShadowsOnly;
   }
   var cross=Primitive("Window_Crossbar_Shadow",PrimitiveType.Cube,centre+basis*new Vector3(0,.35f,0),new Vector3(3.5f,.055f,.025f),mats["DarkWood"],gobo.transform);cross.transform.rotation=basis;cross.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.ShadowsOnly;
   var probeGo=new GameObject("Reflection_Probe_Workbench");probeGo.transform.SetParent(root.transform);probeGo.transform.position=new Vector3(-.2f,1.35f,-.35f);
   var probe=probeGo.AddComponent<ReflectionProbe>();probe.size=new Vector3(5,4,5);probe.resolution=128;probe.boxProjection=false;probe.hdr=true;probe.mode=ReflectionProbeMode.Baked;probe.clearFlags=ReflectionProbeClearFlags.Skybox;
   DynamicGI.UpdateEnvironment();
  }
  static void SetupPostProcessing()
  {
   var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Workbench_2_0_WarmGrade";
   var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
   var color=profile.Add<ColorAdjustments>(true);color.postExposure.Override(.25f);color.contrast.Override(12);color.saturation.Override(-8);
   var balance=profile.Add<WhiteBalance>(true);balance.temperature.Override(5);balance.tint.Override(-3);
   var bloom=profile.Add<Bloom>(true);bloom.threshold.Override(1.1f);bloom.intensity.Override(.15f);bloom.scatter.Override(.45f);
   var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.18f);vignette.smoothness.Override(.55f);
   // Background softness is confined to its plane, so hands/tools stay legible.
   var existing=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/Lighting/Workbench_2_0_Grade.asset");
   if(existing){foreach(var c in existing.components.ToArray())UnityEngine.Object.DestroyImmediate(c,true);existing.components.Clear();foreach(var c in profile.components){existing.components.Add(c);AssetDatabase.AddObjectToAsset(c,existing);}profile.components.Clear();UnityEngine.Object.DestroyImmediate(profile);profile=existing;EditorUtility.SetDirty(profile);}
   else {AssetDatabase.CreateAsset(profile,Root+"/Lighting/Workbench_2_0_Grade.asset");foreach(var c in profile.components)AssetDatabase.AddObjectToAsset(c,profile);}
   var go=new GameObject("Volume_Workbench_2_0_Only");var volume=go.AddComponent<Volume>();volume.isGlobal=true;volume.priority=20;volume.sharedProfile=profile;
  }

  [MenuItem("Dollwright/Art Tests/Workbench 2.0/Validate Art Scene")]
  public static void Validate()
  {
   var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!=ScenePath)throw new InvalidOperationException("Open the Workbench 2.0 art scene.");
   var renderers=UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
   foreach(var r in renderers)foreach(var m in r.sharedMaterials)if(!m||!m.shader||!m.shader.isSupported||ShaderUtil.ShaderHasError(m.shader))throw new InvalidOperationException("Invalid material/shader: "+r.name);
   var scripts=UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().Where(b=>b&&b.GetType().Namespace!=null&&b.GetType().Namespace.StartsWith("Dollwright")).ToArray();
   if(scripts.Length>0)throw new InvalidOperationException("Gameplay script found in art-only scene.");
   if(UnityEngine.Object.FindObjectsOfType<Canvas>().Length>0)throw new InvalidOperationException("Unexpected UI canvas.");
   if(EditorBuildSettings.scenes.Any(s=>s.path==ScenePath))throw new InvalidOperationException("Art test unexpectedly included in game build scenes.");
   var root=GameObject.Find("Workbench_2_0_Art_Model");var meshes=root.GetComponentsInChildren<MeshFilter>(true);
   var all=root.GetComponentsInChildren<Transform>(true);
   var footprint=new Bounds(new Vector3(0,1,0),Vector3.zero);foreach(var r in root.GetComponentsInChildren<Renderer>())footprint.Encapsulate(r.bounds);
   var summary=new ValidationReport{scene=scene.path,meshObjects=meshes.Length,triangles=meshes.Sum(f=>f.sharedMesh.triangles.Length/3),materials=renderers.SelectMany(r=>r.sharedMaterials).Distinct().Count(),gameplayScripts=scripts.Length,uiCanvases=0,backgroundPlanes=1,inBuildSettings=false,woodModuleHidden=!all.First(t=>t.name=="Wood_Station").gameObject.activeSelf,potteryModuleHidden=!all.First(t=>t.name=="Pottery_Station").gameObject.activeSelf,bounds=footprint.ToString()};
   Directory.CreateDirectory(Reports);File.WriteAllText(Reports+"/validation.json",JsonUtility.ToJson(summary,true));File.WriteAllText(Preview+"/unity-validation.json",JsonUtility.ToJson(summary,true));
  }
  [Serializable]class CaptureReport{public string image;public string[] errors;}
  [Serializable]class ValidationReport{public string scene,bounds;public int meshObjects,triangles,materials,gameplayScripts,uiCanvases,backgroundPlanes;public bool inBuildSettings,woodModuleHidden,potteryModuleHidden;}
 }

 [InitializeOnLoad]
 public static class Workbench2ArtBridge
 {
  static double next;
  [Serializable]class Request{public string id,command;}
  [Serializable]class State{public string id,result,error,scene;public bool compiling;}
  static Workbench2ArtBridge(){EditorApplication.update+=Poll;}
  static void Poll()
  {
   if(Application.isBatchMode||EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;
   next=EditorApplication.timeSinceStartup+1;
   string file=Workbench2ArtBuilder.Reports+"/request.json";
   if(!File.Exists(file))return;
   var r=JsonUtility.FromJson<Request>(File.ReadAllText(file));if(r==null||r.id==SessionState.GetString("Workbench2Art.Request",""))return;
   SessionState.SetString("Workbench2Art.Request",r.id);var state=new State{id=r.id};
   try
   {
    switch(r.command)
    {
     case "build":Workbench2ArtBuilder.BuildAndCapture();break;
     case "capture":Workbench2ArtBuilder.Capture();break;
     case "open":Workbench2ArtBuilder.Open();break;
     case "validate":Workbench2ArtBuilder.Validate();break;
     default:throw new InvalidOperationException("Unknown art-test command.");
    }
    state.result="complete";
   }
   catch(Exception ex){state.error=ex.ToString();}
   state.scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
   File.WriteAllText(Workbench2ArtBuilder.Reports+"/state.json",JsonUtility.ToJson(state,true));
  }
 }
}
