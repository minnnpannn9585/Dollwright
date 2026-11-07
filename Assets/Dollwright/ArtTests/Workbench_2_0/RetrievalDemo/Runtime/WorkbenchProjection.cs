using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
namespace Dollwright.RetrievalDemo {
 // Fixed-scale parallel projection onto the actual workbench picture. No pass/fail assistance.
 public class WorkbenchProjection:MonoBehaviour {
  public static readonly Vector3 AssemblyCenter=new Vector3(.05f,1.55f,-.55f),PaperCenter=new Vector3(.85f,1.65f,.15f);
  public static Quaternion ProjectionFrame=>Quaternion.LookRotation((PaperCenter-AssemblyCenter).normalized,Vector3.up);
  GameObject focusRoot;
  Transform shade, bulb;
  Quaternion restShade, restBulb;
  Vector3 restDirection, bulbOffset;
  Light focusLight, idlePool;
  Material beamMaterial;
  Mesh beamMesh;
  Light[] environmentLights;
  float[] lightLevels;
  Color sky,equator,ground;
  float reflection, lift;
  bool focused;
  const float ShadeDepth = .176f, StartRadius = .125f, EndRadius = .34f;
  RetrievalDemoController c;GameObject board;Texture2D result;Material mat;Color32[] pixels;const int N=384;float next;bool[] mask=new bool[N*N];
  public void Initialize(RetrievalDemoController owner){c=owner;CreateFocusLight();board=GameObject.CreatePrimitive(PrimitiveType.Quad);board.name="Workbench_Commission_Picture_And_Shadow";board.transform.position=PaperCenter;board.transform.rotation=ProjectionFrame;board.transform.localScale=Vector3.one*(c.commission.pictureSpan*.32f);board.GetComponent<Collider>().enabled=false;mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));result=new Texture2D(N,N,TextureFormat.RGBA32,false);result.filterMode=FilterMode.Bilinear;mat.SetTexture("_BaseMap",result);board.GetComponent<Renderer>().sharedMaterial=mat;}
  void CreateFocusLight(){var env=GameObject.Find("Lighting_Warm_Window_And_Lamp");environmentLights=env?env.GetComponentsInChildren<Light>(true):new Light[0];lightLevels=new float[environmentLights.Length];for(int i=0;i<lightLevels.Length;i++)lightLevels[i]=environmentLights[i].intensity;
   sky=RenderSettings.ambientSkyColor;equator=RenderSettings.ambientEquatorColor;ground=RenderSettings.ambientGroundColor;reflection=RenderSettings.reflectionIntensity;
   shade=GameObject.Find("Lamp_Bell_Shade").transform;
   bulb=GameObject.Find("Lamp_Bulb").transform;
   idlePool=GameObject.Find("Lamp_Warm_Pool").GetComponent<Light>();
   restShade=shade.rotation;
   restBulb=bulb.rotation;
   restDirection=-shade.forward;
   bulbOffset=bulb.position-shade.position;
   focusRoot=new GameObject("Assembly_Projection_Light");
   focusRoot.transform.SetParent(transform);
   focusLight=focusRoot.AddComponent<Light>();
   focusLight.type=LightType.Spot;
   focusLight.color=new Color(1,.86f,.64f);
   focusLight.range=2.8f;
   focusLight.spotAngle= 66;
   focusLight.innerSpotAngle=52;
   focusLight.cullingMask=1 | (1<<29);
   focusLight.shadows=LightShadows.None;
   beamMaterial=new Material(Shader.Find("Dollwright/Workbench2/LampBeam"));
   beamMaterial.SetColor("_BeamColor",new Color(1,.82f,.55f));
   beamMaterial.SetVector("_BeamRadii",new Vector4(StartRadius,EndRadius,0,0));
   const int sides=20;
   var v=new List<Vector3>();var tris=new List<int>();
   for(int row=0;row<2;row++)for(int i=0;i<sides;i++){
    float angle=i*Mathf.PI*2/sides;
    float radius=row==0?StartRadius:EndRadius;
    v.Add(new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle)*radius,row));
   }
   for(int i=0;i<sides;i++){int j=(i+1)%sides;tris.AddRange(new[]{i,j,sides+j,i,sides+j,sides+i});}v.Add(Vector3.zero);v.Add(Vector3.forward);for(int i=0;i<sides;i++){int j=(i+1)%sides;tris.AddRange(new[]{sides*2,j,i,sides*2+1,sides+i,sides+j});}
   beamMesh=new Mesh{name="Assembly_Light_Shaft"};beamMesh.SetVertices(v);beamMesh.SetTriangles(tris,0);beamMesh.RecalculateBounds();focusRoot.AddComponent<MeshFilter>().sharedMesh=beamMesh;var r=focusRoot.AddComponent<MeshRenderer>();r.sharedMaterial=beamMaterial;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;focusRoot.SetActive(false);
  }
  public void SetVisible(bool visible){
   board.SetActive(visible);
   focused=visible;
   ApplyLampPose();
  }

  void AdvanceLamp(float seconds){
   float nextLift=Mathf.MoveTowards(lift,focused?1f:0f,seconds/.58f);
   if(nextLift==lift)return;
   lift=nextLift;
   ApplyLampPose();
  }

  void ApplyLampPose(){
   float t=Mathf.SmoothStep(0,1,lift);
   Vector3 direction=Vector3.Slerp(restDirection,ProjectionFrame*Vector3.forward,t);
   Quaternion delta=Quaternion.FromToRotation(restDirection,direction);
   shade.rotation=delta*restShade;
   bulb.SetPositionAndRotation(shade.position+delta*bulbOffset,delta*restBulb);
   Vector3 mouth=shade.position+direction*ShadeDepth;
   float length=Vector3.Distance(mouth,PaperCenter);
   focusRoot.transform.SetPositionAndRotation(mouth,Quaternion.LookRotation(direction));
   focusRoot.transform.localScale=new Vector3(1,1,length);
   // Let the physical spotlight follow the shade. Dust in the beam appears as it settles.
   focusRoot.SetActive(focused || (lift>0 && c.State==RetrievalDemoController.Mode.Workbench));
   idlePool.transform.SetPositionAndRotation(mouth,focusRoot.transform.rotation);
   idlePool.enabled=c.State==RetrievalDemoController.Mode.Workbench || focused;
   idlePool.intensity=.68f*(1-t);
   idlePool.spotAngle=63;
   focusLight.intensity=Mathf.Lerp(.04f,1.48f,t);
   beamMaterial.SetFloat("_Density",.26f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.62f,1,t)));
   beamMaterial.SetVector("_BeamOrigin",mouth);
   beamMaterial.SetVector("_BeamAxis",new Vector4(direction.x,direction.y,direction.z,length));
   for(int i=0;i<environmentLights.Length;i++)
    if(environmentLights[i] && environmentLights[i]!=idlePool)environmentLights[i].intensity=lightLevels[i]*Mathf.Lerp(1,.79f,t);
   float ambient=Mathf.Lerp(1,.84f,t);
   RenderSettings.ambientSkyColor=sky*ambient;
   RenderSettings.ambientEquatorColor=equator*ambient;
   RenderSettings.ambientGroundColor=ground*ambient;
   RenderSettings.reflectionIntensity=reflection*ambient;
  }
  void LateUpdate(){AdvanceLamp(Time.unscaledDeltaTime);if(c.State==RetrievalDemoController.Mode.Assembly&&Time.unscaledTime>=next){next=Time.unscaledTime+.05f;Refresh();}}
  public void Refresh(){if(!result)return;var image=c.commission.apertureImages[c.Picture];pixels=new Color32[N*N];for(int y=0;y<N;y++)for(int x=0;x<N;x++)pixels[y*N+x]=image.GetPixelBilinear(y/(float)(N-1),x/(float)(N-1));System.Array.Clear(mask,0,mask.Length);
   Draw(c.Free.torso.transform.GetComponent<MeshFilter>());
   for(int i=0;i<6;i++)if(c.Free.equipped[i])foreach(var m in c.Free.parts[i].GetComponentsInChildren<MeshFilter>())if(m.GetComponent<Renderer>()&&m.GetComponent<Renderer>().enabled)Draw(m);
   for(int i=0;i<pixels.Length;i++)if(mask[i])pixels[i]=Color.Lerp(pixels[i],new Color(.08f,.65f,.85f),.58f);result.SetPixels32(pixels);result.Apply(false);
  }
  Vector2 Point(Transform t,Vector3 v){var w=Quaternion.Inverse(ProjectionFrame)*(t.TransformPoint(v)-c.Doll.transform.position);float span=c.commission.pictureSpan*.32f;return new Vector2((w.x/span+.5f)*(N-1),(w.y/span+.5f)*(N-1));}
  void Draw(MeshFilter m){if(!m||!m.sharedMesh)return;var vs=m.sharedMesh.vertices;var tri=m.sharedMesh.triangles;for(int i=0;i<tri.Length;i+=3)Triangle(Point(m.transform,vs[tri[i]]),Point(m.transform,vs[tri[i+1]]),Point(m.transform,vs[tri[i+2]]));}
  static float Cross(Vector2 a,Vector2 b,Vector2 p)=>(b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);
  void Triangle(Vector2 a,Vector2 b,Vector2 d){if(Mathf.Abs(Cross(a,b,d))<.02f)return;int x0=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x,Mathf.Min(b.x,d.x))),0,N-1),x1=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x,Mathf.Max(b.x,d.x))),0,N-1),y0=Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y,Mathf.Min(b.y,d.y))),0,N-1),y1=Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y,Mathf.Max(b.y,d.y))),0,N-1);
   for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++){var p=new Vector2(x+.5f,y+.5f);float u=Cross(a,b,p),v=Cross(b,d,p),w=Cross(d,a,p);if((u>=0&&v>=0&&w>=0)||(u<=0&&v<=0&&w<=0))mask[y*N+x]=true;}}
  void OnDestroy(){if(board&&focusRoot&&shade&&bulb){lift=0;SetVisible(false);}if(focusRoot)Destroy(focusRoot);if(beamMaterial)Destroy(beamMaterial);if(beamMesh)Destroy(beamMesh);if(board)Destroy(board);if(mat)Destroy(mat);if(result)Destroy(result);}
 }
}

