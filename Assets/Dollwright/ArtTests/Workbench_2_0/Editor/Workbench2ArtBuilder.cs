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
   // The source's independent geometry is retained; no game behaviours are attached.
   PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/Workbench_2_0_Art.prefab");
   SetupCamera();
   SetupBackground();
   SetupLighting();
   SetupPostProcessing();
   ConfigureFocusRevision();
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
   if(!Application.isBatchMode)EditorApplication.ExecuteMenuItem("Window/General/Game");
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
  [MenuItem("Dollwright/Art Tests/Workbench 2.0/Apply Platform Focus and Blank Papers")]
  public static void FocusAndCapture()
  {
   Application.logMessageReceived-=TrackError;Application.logMessageReceived+=TrackError;errors.Clear();
   if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play mode first.");
   if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
   if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=ScenePath)EditorSceneManager.OpenScene(ScenePath);
   camera=Camera.main;ConfigureFocusRevision();
   EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
   Validate();EditorApplication.ExecuteMenuItem("Window/General/Game");
   QueueCapture("03_Unity_Focus_Revision.png",Application.isBatchMode);
  }
  static void ConfigureFocusRevision()
  {
   var model=GameObject.Find("Workbench_2_0_Art_Model");
   if(!model)throw new InvalidOperationException("Workbench model missing.");
   string handlePath=Root+"/Materials/WB2_DrawerHandle_Matte.mat";
   var handle=AssetDatabase.LoadAssetAtPath<Material>(handlePath);
   if(!handle){handle=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(handle,handlePath);}
   handle.SetColor("_BaseColor",C(.38f,.31f,.22f));handle.SetFloat("_Metallic",.15f);handle.SetFloat("_Smoothness",.08f);
   handle.SetFloat("_SpecularHighlights",0);handle.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
   handle.SetFloat("_EnvironmentReflections",0);handle.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");EditorUtility.SetDirty(handle);
   var paper=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/WB2_Paper.mat");
   paper.SetTexture("_BaseMap",null);paper.SetTexture("_MainTex",null);paper.SetColor("_BaseColor",C(.82f,.78f,.67f));paper.SetFloat("_Smoothness",.06f);EditorUtility.SetDirty(paper);
   int handles=0,papers=0;
   foreach(var r in model.GetComponentsInChildren<MeshRenderer>(true))
   {
    if(r.name.Contains("Drawer")&&r.name.Contains("Handle")){r.sharedMaterial=handle;handles++;}
    if(r.name.Contains("Paper")||r.name.StartsWith("Apron_Note")){r.sharedMaterial=paper;papers++;}
   }
   var top=model.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name=="Crafting_Platform_Top");
   var target=new Vector3(top.bounds.center.x,top.bounds.max.y+.008f,top.bounds.center.z);
   var lamp=GameObject.Find("Lamp_Warm_Pool").GetComponent<Light>();
   var bulb=model.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Lamp_Bulb");
   // Keep the lamp's oval sunlight shadow from obscuring the intended crafting target.
   model.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name=="Lamp_Bell_Shade").shadowCastingMode=ShadowCastingMode.Off;
   // Put the virtual emitter beyond the shade lip so its rim cannot block the centre pool.
   lamp.transform.position=bulb.position+(target-bulb.position).normalized*.18f;
   lamp.transform.LookAt(target);lamp.color=C(1,.83f,.57f);lamp.intensity=2.8f;lamp.spotAngle=49;lamp.innerSpotAngle=28;lamp.range=2.1f;
   var bounce=GameObject.Find("Desk_Warm_Bounce").GetComponent<Light>();bounce.intensity=.10f;
   camera=Camera.main;camera.GetUniversalAdditionalCameraData().requiresDepthTexture=true;
   MakeLampBeam(lamp.transform.position,target,top.bounds.extents.x*.53f,lamp.transform.parent);
   PrefabUtility.SaveAsPrefabAsset(model,Root+"/Prefabs/Workbench_2_0_Art.prefab");
   File.WriteAllText(Reports+"/focus-revision.txt","Drawer handle meshes: "+handles+"\nBlank paper meshes: "+papers+"\nPaper texture: none; UVs preserved.\nLamp target: "+target+"\nOriginal lamp brass unchanged.\nBeam depth clipped; no gameplay scripts.\n");
   if(handles!=14||papers!=8)throw new InvalidOperationException("Unexpected handle or paper mesh count: "+handles+", "+papers);
   if(paper.GetTexture("_BaseMap")||handle.GetFloat("_Smoothness")>.15f||handle.GetFloat("_Metallic")>.2f)throw new InvalidOperationException("Paper / matte handle checks failed.");
   if(Vector3.Dot(lamp.transform.forward,(target-lamp.transform.position).normalized)<.999f)throw new InvalidOperationException("Lamp is not aimed at crafting centre.");
  }
  static void MakeLampBeam(Vector3 origin,Vector3 target,float endRadius,Transform parent)
  {
   const string objectName="Lamp_Beam_To_Crafting_Platform";
   var old=GameObject.Find(objectName);if(old)UnityEngine.Object.DestroyImmediate(old);
   var direction=(target-origin).normalized;float length=Vector3.Distance(origin,target);float startRadius=.095f;
   var material=new Material(Shader.Find("Dollwright/Workbench2/LampBeam"));
   material.SetColor("_BeamColor",C(1,.76f,.42f));material.SetFloat("_Density",.52f);
   material.SetVector("_BeamOrigin",origin);material.SetVector("_BeamAxis",new Vector4(direction.x,direction.y,direction.z,length));material.SetVector("_BeamRadii",new Vector4(startRadius,endRadius,0,0));
   PutAsset(material,Root+"/Materials/WB2_LampBeam.mat");
   const int sides=32;var v=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
   for(int row=0;row<2;row++)for(int i=0;i<sides;i++)
   {float a=i*Mathf.PI*2/sides;float radius=row==0?startRadius:endRadius;v.Add(new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,row*length));uv.Add(new Vector2(i/(float)sides,row));}
   for(int i=0;i<sides;i++){int j=(i+1)%sides;triangles.AddRange(new[]{i,j,sides+j,i,sides+j,sides+i});}
   v.Add(Vector3.zero);uv.Add(new Vector2(.5f,0));v.Add(new Vector3(0,0,length));uv.Add(new Vector2(.5f,1));
   for(int i=0;i<sides;i++){int j=(i+1)%sides;triangles.AddRange(new[]{sides*2,j,i,sides*2+1,sides+i,sides+j});}
   var mesh=new Mesh{name="Lamp_Beam_Proxy"};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
   PutAsset(mesh,Root+"/Models/Lamp_Beam_Proxy.asset");
   var go=new GameObject(objectName);go.transform.SetParent(parent);go.transform.SetPositionAndRotation(origin,Quaternion.LookRotation(direction));
   go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Models/Lamp_Beam_Proxy.asset");var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/WB2_LampBeam.mat");r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
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
    var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;float oldFov=camera.fieldOfView;
    try
    {
     camera.transform.position=new Vector3(-1.55f,1.65f,-2.8f);camera.transform.LookAt(new Vector3(-.26f,1.08f,-.08f));camera.fieldOfView=32;
     RenderToFile(camera,Path.Combine(Preview,"02_Unity_Material_Detail.png"),1600,1000);
    }
    finally{camera.transform.SetPositionAndRotation(oldPosition,oldRotation);camera.fieldOfView=oldFov;}
    // Persist the restored main view, never the temporary material inspection camera.
    EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
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
   Mat("Wood",C(.72f,.67f,.58f),wood,.23f,0,true);
   Mat("DarkWood",C(.49f,.47f,.40f),wood,.20f,0,true);
   Mat("Recess",C(.40f,.44f,.35f),wood,.10f,0,true,C(.23f,.25f,.18f),.72f);
   Mat("GreenPaint",C(.65f,.58f,.42f),wood,.24f,0,true,C(.15f,.26f,.21f),.64f);
   Mat("TealPaint",C(.66f,.53f,.35f),wood,.30f,0,true,C(.09f,.27f,.25f),.93f);
   Mat("RedPaint",C(.62f,.47f,.30f),wood,.26f,0,true,C(.32f,.055f,.032f),.85f);
   Mat("RedPanel",C(.71f,.70f,.65f),panel,.26f);
   Mat("Brass",C(.64f,.49f,.28f),null,.49f,.78f);
   Mat("Steel",C(.53f,.59f,.56f),null,.55f,.75f);
   Mat("Ink",C(.055f,.075f,.057f),null,.32f,.18f);
   Mat("Ivory",C(.78f,.75f,.65f),wood,.24f,0,true,C(.73f,.70f,.61f),.98f);
   Mat("Ceramic",C(.65f,.67f,.59f),null,.30f);
   Mat("Paper",C(.82f,.78f,.67f),paper,.08f);
   Mat("Fabric",C(.065f,.24f,.25f),null,.12f);
   Mat("Cotton",C(.58f,.49f,.32f),null,.12f);
   Mat("Cord",C(.20f,.14f,.07f),null,.12f);
   Mat("Bristles",C(.055f,.027f,.012f),null,.18f);
   var bulb=Mat("Bulb",C(1,.76f,.36f),null,.25f);bulb.EnableKeyword("_EMISSION");bulb.SetColor("_EmissionColor",C(1,.48f,.12f)*3.5f);
   var bg=new Material(Shader.Find("Dollwright/Workbench2/SoftBackdrop"));bg.SetTexture("_BaseMap",Texture("Workshop_Background",false));bg.SetColor("_BaseColor",C(.90f,.94f,.90f));bg.SetFloat("_Blur",1.4f);
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
   RenderSettings.ambientSkyColor=C(.50f,.54f,.45f);RenderSettings.ambientEquatorColor=C(.36f,.39f,.30f);RenderSettings.ambientGroundColor=C(.23f,.19f,.13f);
   RenderSettings.ambientIntensity=1;RenderSettings.reflectionIntensity=.85f;RenderSettings.fog=false;
   var sky=new Material(Shader.Find("Skybox/Procedural"));sky.SetColor("_SkyTint",C(.46f,.49f,.35f));sky.SetColor("_GroundColor",C(.14f,.10f,.06f));sky.SetFloat("_Exposure",.8f);
   PutAsset(sky,Root+"/Lighting/Warm_Reflection_Sky.mat");RenderSettings.skybox=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Lighting/Warm_Reflection_Sky.mat");
   var sun=MakeLight("Sun_Left_Window",LightType.Directional,new Vector3(-3.4f,5,-1.8f),new Vector3(.3f,.8f,0),C(1,.88f,.69f),1.8f,root.transform);
   RenderSettings.sun=sun;
   sun.cookie=Texture("Window_Leaf_Light",false);sun.cookieSize=3.8f;
   var sunData=sun.gameObject.AddComponent<UniversalAdditionalLightData>();sunData.lightCookieSize=new Vector2(3.8f,3.8f);sunData.lightCookieOffset=new Vector2(.4f,-.3f);
   var fill=MakeLight("Soft_Green_Fill",LightType.Directional,new Vector3(2,2.5f,-4),new Vector3(0,1,0),C(.69f,.76f,.68f),.80f,root.transform);fill.shadows=LightShadows.None;
   var bulb=UnityEngine.Object.FindObjectsOfType<Transform>().First(t=>t.name=="Lamp_Bulb");
   var lamp=MakeLight("Lamp_Warm_Pool",LightType.Spot,bulb.position+new Vector3(0,-.09f,0),new Vector3(-.45f,.86f,-.15f),C(1,.81f,.58f),1.2f,root.transform);lamp.spotAngle=92;lamp.innerSpotAngle=52;lamp.range=2.1f;
   var bounce=MakeLight("Desk_Warm_Bounce",LightType.Point,new Vector3(-.6f,1.02f,-.35f),Vector3.zero,C(1,.66f,.32f),.22f,root.transform);bounce.range=1.6f;bounce.shadows=LightShadows.None;
   // Soft projected leaf/window pattern requires no extra environment geometry.
   var probeGo=new GameObject("Reflection_Probe_Workbench");probeGo.transform.SetParent(root.transform);probeGo.transform.position=new Vector3(-.2f,1.35f,-.35f);
   var probe=probeGo.AddComponent<ReflectionProbe>();probe.size=new Vector3(5,4,5);probe.resolution=128;probe.boxProjection=false;probe.hdr=true;probe.mode=ReflectionProbeMode.Baked;probe.clearFlags=ReflectionProbeClearFlags.Skybox;
   DynamicGI.UpdateEnvironment();
  }
  static void SetupPostProcessing()
  {
   var profile=ScriptableObject.CreateInstance<VolumeProfile>();profile.name="Workbench_2_0_WarmGrade";
   var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
   var color=profile.Add<ColorAdjustments>(true);color.postExposure.Override(.20f);color.contrast.Override(3);color.saturation.Override(-12);
   var balance=profile.Add<WhiteBalance>(true);balance.temperature.Override(2);balance.tint.Override(-3);
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
   foreach(var f in meshes)if(f.sharedMesh.uv.Length!=f.sharedMesh.vertexCount)throw new InvalidOperationException("Missing UV coordinates: "+f.name);
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
  static Workbench2ArtBridge()
  {
   // Commands from a closed editor session must not rebuild a scene on next launch.
   if(!SessionState.GetBool("Workbench2Art.BridgeInitialized",false))
   {
    string file=Workbench2ArtBuilder.Reports+"/request.json";
    if(File.Exists(file)){try{var r=JsonUtility.FromJson<Request>(File.ReadAllText(file));if(r!=null)SessionState.SetString("Workbench2Art.Request",r.id);}catch(IOException){}}
    SessionState.SetBool("Workbench2Art.BridgeInitialized",true);
   }
   EditorApplication.update+=Poll;
  }
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
     case "focus":Workbench2ArtBuilder.FocusAndCapture();break;
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
