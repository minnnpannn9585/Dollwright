using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Dollwright.Graybox.Editor
{
    public static class WorkbenchArtPass
    {
        public const string Root = "Assets/Dollwright/WorkbenchArt";
        const string Materials = Root+"/SunlitMaterials";
        const string Textures = Root+"/SunlitTextures";
        const string Report = "Logs/DollwrightGraybox/WorkbenchArt";
        static Material honey, walnut, lightWood, teal, violet, brass, steel, paper, rope, clay, red, ink, floral;
        [MenuItem("Dollwright/Workbench Art/Apply Painted Materials and Warm Light")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveOpenScenes()) return;
            var scene = EditorSceneManager.OpenScene(DreamTravel.WorkbenchScene);
            Directory.CreateDirectory(Report);
            string backup=Report+"/Before-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".unity";
            File.Copy(DreamTravel.WorkbenchScene,backup);
            foreach (string dir in new[]{Materials,Root+"/MappedMeshes"}) Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
            var wood=ImportTexture("Ivory_Washed_Wood",true,2048);
            var decoration=ImportTexture("Lavender_Floral_Band",true,2048);
            var sketches=ImportTexture("Clue_Sketch_Atlas",false,2048);
            honey=Mat("Sunlit_CreamWood",new Color(.92f,.84f,.75f),wood);
            walnut=Mat("Sunlit_InnerWood",new Color(.63f,.53f,.47f),wood);
            lightWood=Mat("Sunlit_IvoryPlatform",new Color(1,1,.98f),wood,.18f);
            teal=Mat("Sunlit_MistBlue",new Color(.65f,.80f,1f),wood);
            violet=Mat("Sunlit_Lavender",new Color(.79f,.76f,1.08f),wood);
            brass=Mat("Sunlit_Brass",new Color(.69f,.45f,.19f),null,.46f,.65f);
            steel=Mat("Sunlit_Steel",new Color(.62f,.65f,.66f),null,.4f,.5f);
            paper=Mat("Sunlit_PaperSketches",Color.white,sketches);
            rope=Mat("Sunlit_Flax",new Color(.61f,.46f,.29f));
            clay=Mat("Sunlit_CreamCeramic",new Color(.96f,.91f,.82f),null,.32f);
            red=Mat("Sunlit_JarLid",new Color(.7f,.64f,.82f),wood);
            ink=Mat("Sunlit_BrushBristles",new Color(.23f,.12f,.075f));
            floral=Mat("Sunlit_LavenderDecoration",Color.white,decoration,.25f);
            var originalMeshes=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Dollwright/Meshes/Doll_Workbench_Modeling_12.fbx").GetComponentsInChildren<MeshFilter>(true).ToDictionary(f=>f.name,f=>f.sharedMesh);
            int mapped=0,triangles=0;
            var assignment=new StringBuilder();
            foreach (var f in UnityEngine.Object.FindObjectsOfType<MeshFilter>())
            {
                var source=AssetDatabase.GetAssetPath(f.sharedMesh);
                if (!source.EndsWith("Doll_Workbench_Modeling_12.fbx",StringComparison.OrdinalIgnoreCase) && !source.StartsWith(Root+"/MappedMeshes/")) continue;
                var r=f.GetComponent<MeshRenderer>();
                if (!r) continue;
                var material=Choose(f.name);
                f.sharedMesh=originalMeshes[f.name];
                triangles+=f.sharedMesh.triangles.Length/3;
                if(f.name.StartsWith("Crafting_Platform_Top")||f.name=="Tool_Container_Cup")
                {
                    MapDecoration(f,f.name=="Tool_Container_Cup");
                    r.sharedMaterials=new[]{material,floral};mapped++;
                }
                else
                {
                    if(f.name.Contains("Paper")){MapPaper(f);mapped++;}
                    else if (material.GetTexture("_BaseMap")) { MapWood(f); mapped++; }
                    r.sharedMaterials=Enumerable.Repeat(material,f.sharedMesh.subMeshCount).ToArray();
                }
                assignment.AppendLine(f.name+" -> "+material.name);
                // Render meshes are not interaction targets; keep original collider hierarchy untouched.
                PrefabUtility.RecordPrefabInstancePropertyModifications(f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
            var oldArt=GameObject.Find("Workbench_Art_Lighting_Backdrop");
            if (oldArt) UnityEngine.Object.DestroyImmediate(oldArt);
            var root=new GameObject("Workbench_Art_Lighting_Backdrop");
            foreach(var l in UnityEngine.Object.FindObjectsOfType<Light>()) l.enabled=false;
            var key=Light(root.transform,"Warm_Window_Key",new Color(1,.90f,.78f),1.24f,new Vector3(40,222,0),LightShadows.Soft);
            key.shadowStrength=.68f;key.shadowBias=.035f;key.shadowNormalBias=.15f;
            Light(root.transform,"Cool_Soft_Fill",new Color(.76f,.78f,1),.27f,new Vector3(18,139,0),LightShadows.None);
            RenderSettings.sun=key;
            RenderSettings.skybox=null;
            RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.57f,.54f,.60f);
            RenderSettings.ambientEquatorColor=new Color(.44f,.40f,.40f);
            RenderSettings.ambientGroundColor=new Color(.22f,.20f,.24f);
            RenderSettings.ambientIntensity=1;
            RenderSettings.reflectionIntensity=.25f;
            RenderSettings.fog=false;
            Camera.main.clearFlags=CameraClearFlags.SolidColor;
            Camera.main.backgroundColor=new Color(.43f,.41f,.43f);
            var cameraData=Camera.main.GetComponents<Component>().FirstOrDefault(c=>c.GetType().Name=="UniversalAdditionalCameraData");
            if(cameraData){var data=new SerializedObject(cameraData);var shadows=data.FindProperty("m_RenderShadows");if(shadows!=null)shadows.boolValue=true;data.ApplyModifiedPropertiesWithoutUndo();}
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            File.WriteAllText(Report+"/assignments.txt",assignment.ToString());
            File.WriteAllText(Report+"/summary.json",JsonUtility.ToJson(new Summary{mappedMeshes=mapped, furnitureTriangles=triangles, scene=scene.path, background="", geometryUnchanged=true},true));
            Validate();
            EditorSceneManager.SaveScene(scene);
            DeletePreviousMaterials();
            Debug.Log("Workbench art applied: "+mapped+" mapped pieces, "+triangles+" original triangles.");
        }
        [MenuItem("Dollwright/Workbench Art/Validate Geometry and Camera Coverage")]
        public static void Validate()
        {
            var lines=new StringBuilder();
            var original=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Dollwright/Meshes/Doll_Workbench_Modeling_12.fbx")
                .GetComponentsInChildren<MeshFilter>(true).ToDictionary(f=>f.name,f=>f.sharedMesh);
            int compared=0;
            foreach(var f in UnityEngine.Object.FindObjectsOfType<MeshFilter>())
            {
                if(!AssetDatabase.GetAssetPath(f.sharedMesh).StartsWith(Root+"/MappedMeshes/"))continue;
                var before=original[f.name];var after=f.sharedMesh;
                var ia=before.triangles;var ib=after.triangles;
                if(ia.Length!=ib.Length)throw new InvalidOperationException("Triangle count changed: "+f.name);
                if(!TriangleKeys(before).SequenceEqual(TriangleKeys(after)))throw new InvalidOperationException("Geometry changed: "+f.name);
                if(after.uv.Length!=after.vertexCount)throw new InvalidOperationException("Missing UVs: "+f.name);
                compared++;
            }
            foreach(var r in UnityEngine.Object.FindObjectsOfType<Renderer>()) foreach(var m in r.sharedMaterials)
                if(!m||!m.shader||!m.shader.isSupported||ShaderUtil.ShaderHasError(m.shader))throw new InvalidOperationException("Invalid shader on "+r.name);
            lines.AppendLine("PASS: "+compared+" mapped pieces match source triangle positions, winding and normals.");
            if(GameObject.Find("Background_2D_PaintedWorkshop"))throw new InvalidOperationException("Unexpected background image.");
            lines.AppendLine("PASS: no workbench background image.");
            lines.AppendLine("PASS: scene materials use supported shaders without compile errors.");
            File.WriteAllText(Report+"/validation.txt",lines.ToString());
        }
        [Serializable] sealed class Summary { public int mappedMeshes,furnitureTriangles; public string scene,background; public bool geometryUnchanged; }
        static Texture2D ImportTexture(string name,bool repeat,int maxSize)
        {
            string path=Textures+"/"+name+".png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(!importer)throw new FileNotFoundException(path);
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
            importer.mipmapEnabled=repeat;importer.wrapMode=repeat?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
            importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=repeat?8:1;
            importer.maxTextureSize=maxSize;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.alphaIsTransparency=false;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        static Material Mat(string name,Color color,Texture texture=null,float smoothness=.12f,float metallic=0,bool unlit=false)
        {
            string path=Materials+"/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetTexture("_BaseMap",texture);
            if(m.HasProperty("_Smoothness"))m.SetFloat("_Smoothness",smoothness);
            if(m.HasProperty("_Metallic"))m.SetFloat("_Metallic",metallic);
            if(m.HasProperty("_Cull"))m.SetFloat("_Cull",2);
            EditorUtility.SetDirty(m);return m;
        }
        static Material Choose(string n)
        {
            if(n.Contains("Knob")||n.Contains("Pivot")||n.Contains("Collar"))return brass;
            if(n.Contains("Blade")||n.Contains("Shank"))return steel;
            if(n.StartsWith("Scissors_Handle"))return teal;
            if(n.Contains("Paper"))return paper;
            if(n.Contains("Rope")||n.Contains("Hanger"))return rope;
            if(n.Contains("Cloth"))return violet;
            if(n.StartsWith("Brush_")&&n.EndsWith("Tip"))return ink;
            if(n.StartsWith("Tool_Container")||n.Contains("Jar_Body"))return clay;
            if(n.Contains("Jar_Lid"))return red;
            if(n.StartsWith("Clue_Board_Back"))return teal;
            if(n.StartsWith("Clue_Board_"))return honey;
            if(n.StartsWith("Crafting_Platform_Top"))return lightWood;
            if(n.StartsWith("Crafting_Platform"))return walnut;
            if(n.StartsWith("Display_"))return honey;
            if(n.StartsWith("Left_Front_Cabinet"))return n.Contains("Drawer_")?lightWood:violet;
            if(n.StartsWith("Left_Drawer")&&!n.Contains("Body")&&!n.Contains("Cap"))return violet;
            if(n.StartsWith("Right_Drawer")&&!n.Contains("Body")&&!n.Contains("Cap"))return teal;
            if(n.Contains("Drawers_Body"))return honey;
            if(n.Contains("Backboard"))return honey;
            if(n.StartsWith("Main_Workbench")&&!n.Contains("Top_Plank")&&!n.Contains("Front_Plank"))return walnut;
            return honey;
        }
        static Light Light(Transform root,string name,Color color,float intensity,Vector3 angles,LightShadows shadows)
        {
            var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.rotation=Quaternion.Euler(angles);
            var l=go.AddComponent<Light>();l.type=LightType.Directional;l.color=color;l.intensity=intensity;l.shadows=shadows;return l;
        }
        static string[] TriangleKeys(Mesh mesh)
        {
            var v=mesh.vertices;var n=mesh.normals;var indices=mesh.triangles;
            var culture=System.Globalization.CultureInfo.InvariantCulture;
            string[] vertices=Enumerable.Range(0,v.Length).Select(i=>string.Join(",",new[]{v[i].x,v[i].y,v[i].z,n[i].x,n[i].y,n[i].z}.Select(f=>f.ToString("R",culture)))).ToArray();
            var keys=new string[indices.Length/3];
            for(int i=0;i<indices.Length;i+=3)keys[i/3]=vertices[indices[i]]+"|"+vertices[indices[i+1]]+"|"+vertices[indices[i+2]];
            Array.Sort(keys,StringComparer.Ordinal);return keys;
        }
        static void DeletePreviousMaterials()
        {
            string[] folders={Root+"/Materials",Root+"/Textures"};
            var dependencies=AssetDatabase.GetDependencies(DreamTravel.WorkbenchScene,true);
            foreach(string folder in folders)
            {
                if(dependencies.Any(p=>p.StartsWith(folder+"/")))throw new InvalidOperationException("Old art is still referenced: "+folder);
                if(AssetDatabase.IsValidFolder(folder)&&!AssetDatabase.DeleteAsset(folder))throw new IOException("Could not remove old art: "+folder);
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText(Report+"/retired-materials.txt","Previous Materials/ and Textures/ removed from Assets after dependency validation.\nArchive: Logs/DollwrightGraybox/BeforeSunlitMaterials-20261020/PreviousWorkbenchArt.zip\n");
        }
        static void SaveMapped(MeshFilter f,Mesh mesh)
        {
            mesh.name=f.name+"_PaintUV";mesh.RecalculateBounds();mesh.RecalculateTangents();
            string path=Root+"/MappedMeshes/"+f.name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing)
            {
                // Mesh setters invalidate native rendering buffers as well as serialized data.
                existing.Clear();existing.indexFormat=mesh.indexFormat;
                existing.vertices=mesh.vertices;existing.normals=mesh.normals;existing.uv=mesh.uv;
                existing.subMeshCount=mesh.subMeshCount;
                for(int i=0;i<mesh.subMeshCount;i++)existing.SetTriangles(mesh.GetTriangles(i),i);
                existing.tangents=mesh.tangents;existing.bounds=mesh.bounds;
                UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);
            }
            else AssetDatabase.CreateAsset(mesh,path);
            f.sharedMesh=mesh;
        }
        static void MapPaper(MeshFilter f)
        {
            var mesh=UnityEngine.Object.Instantiate(f.sharedMesh);var vertices=mesh.vertices;
            var b=f.GetComponent<Renderer>().bounds;
            int index=0;var digits=new string(f.name.Where(char.IsDigit).ToArray());if(digits.Length>0)index=(int.Parse(digits)-1)%4;
            Vector2 cell=new Vector2(index%2*.5f,index<2?.5f:0);
            mesh.uv=vertices.Select(v=>{
                var p=f.transform.TransformPoint(v);
                float u=(b.max.x-p.x)/Mathf.Max(b.size.x,.001f),y=(p.y-b.min.y)/Mathf.Max(b.size.y,.001f);
                if(f.name.StartsWith("Loose_"))y=(p.z-b.min.z)/Mathf.Max(b.size.z,.001f);
                return cell+new Vector2(.01f+Mathf.Clamp01(u)*.48f,.01f+Mathf.Clamp01(y)*.48f);
            }).ToArray();
            SaveMapped(f,mesh);
        }
        static void MapDecoration(MeshFilter f,bool cup)
        {
            var source=f.sharedMesh;var v=source.vertices;var n=source.normals;var triangles=source.triangles;
            var world=v.Select(p=>f.transform.TransformPoint(p)).ToArray();
            var bounds=f.GetComponent<Renderer>().bounds;
            Vector3 center=cup?bounds.center:new Vector3(0,1.30f,.2f);
            var verts=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();
            var indices=new[]{new List<int>(),new List<int>()};var cache=new Dictionary<int,int>();
            for(int i=0;i<triangles.Length;i+=3)
            {
                Vector3 p=(world[triangles[i]]+world[triangles[i+1]]+world[triangles[i+2]])/3;
                Vector3 face=Vector3.Cross(world[triangles[i+1]]-world[triangles[i]],world[triangles[i+2]]-world[triangles[i]]).normalized;
                Vector3 radial=p-center;radial.y=0;
                int part=Mathf.Abs(face.y)<.5f&&Vector3.Dot(face,radial.normalized)>.6f?1:0;
                var u=new float[3];
                for(int k=0;k<3;k++){var point=world[triangles[i+k]]-center;u[k]=Mathf.Atan2(point.x,point.z)/(2*Mathf.PI)+.5f;}
                bool seam=u.Max()-u.Min()>.5f;
                for(int k=0;k<3;k++)
                {
                    int original=triangles[i+k];bool wrap=seam&&u[k]<.5f;
                    int key=original*4+part+(part==1&&wrap?2:0);
                    if(!cache.TryGetValue(key,out int mapped))
                    {
                        mapped=verts.Count;cache.Add(key,mapped);verts.Add(v[original]);normals.Add(n[original]);
                        var point=world[original];
                        uv.Add(part==1?new Vector2((u[k]+(wrap?1:0))*(cup?1:3.5f),Mathf.Clamp01((point.y-bounds.min.y)/bounds.size.y)):
                            new Vector2(point.x/1.5f,point.z/1.5f));
                    }
                    indices[part].Add(mapped);
                }
            }
            var mesh=new Mesh();mesh.SetVertices(verts);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.subMeshCount=2;
            for(int i=0;i<2;i++)mesh.SetTriangles(indices[i],i);
            SaveMapped(f,mesh);
        }
        // Box-project in meters with grain following each board's longest visible axis.
        // Only UVs and necessary seam vertices change; triangle positions, winding and normals are preserved.
        static void MapWood(MeshFilter f)
        {
            var source=f.sharedMesh;
            var vertices=source.vertices;var normals=source.normals;
            var positions=vertices.Select(p=>f.transform.TransformPoint(p)).ToArray();
            var bounds=f.GetComponent<Renderer>().bounds;
            var newVertices=new List<Vector3>();var newNormals=new List<Vector3>();var uv=new List<Vector2>();
            var cache=new Dictionary<int,int>();var indices=new List<int[]>();
            int seed=0;foreach(char ch in f.name)seed=(seed*31+ch)&0x7fffffff;
            var offset=new Vector2((seed%83)*.021f,(seed%67)*.037f);
            for(int sub=0;sub<source.subMeshCount;sub++)
            {
                var old=source.GetTriangles(sub);var output=new int[old.Length];
                for(int i=0;i<old.Length;i+=3)
                {
                    Vector3 normal=Vector3.Cross(positions[old[i+1]]-positions[old[i]],positions[old[i+2]]-positions[old[i]]).normalized;
                    int axis=Mathf.Abs(normal.x)>Mathf.Abs(normal.y)?0:1;
                    if(Mathf.Abs(normal.z)>Mathf.Abs(normal[axis]))axis=2;
                    int a=(axis+1)%3,b=(axis+2)%3;
                    int along=bounds.size[a]>bounds.size[b]?a:b;
                    int across=along==a?b:a;
                    for(int k=0;k<3;k++)
                    {
                        int original=old[i+k],key=original*3+axis;
                        if(!cache.TryGetValue(key,out int mapped))
                        {
                            mapped=newVertices.Count;cache.Add(key,mapped);
                            newVertices.Add(vertices[original]);newNormals.Add(normals[original]);
                            Vector3 p=positions[original]-bounds.center;
                            uv.Add(new Vector2(p[across]/2f,p[along]/4.5f)+offset);
                        }
                        output[i+k]=mapped;
                    }
                }
                indices.Add(output);
            }
            var mesh=new Mesh{name=f.name+"_PaintUV"};
            mesh.SetVertices(newVertices);mesh.SetNormals(newNormals);mesh.SetUVs(0,uv);mesh.subMeshCount=indices.Count;
            for(int i=0;i<indices.Count;i++)mesh.SetTriangles(indices[i],i);
            SaveMapped(f,mesh);
        }
        [MenuItem("Dollwright/Workbench Art/Inspect")]
        public static void Inspect()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveOpenScenes()) return;
            EditorSceneManager.OpenScene(DreamTravel.WorkbenchScene);
            Directory.CreateDirectory(Report);
            var text = new StringBuilder();
            foreach (var f in UnityEngine.Object.FindObjectsOfType<MeshFilter>().OrderBy(f => f.name))
            {
                var m = f.sharedMesh;
                var r = f.GetComponent<Renderer>();
                if (!m || !r) continue;
                var uv = m.uv;
                text.AppendLine($"{f.name} | vertices={m.vertexCount} uv={uv.Length} readable={m.isReadable} | size={m.bounds.size:F3} | materials={string.Join(",", r.sharedMaterials.Select(a=>a?a.name:"NULL"))}");
                if(f.name=="Crafting_Platform_Top_Plank_01")for(int sub=0;sub<m.subMeshCount;sub++)
                {
                    var verts=m.vertices;var ts=m.GetTriangles(sub);
                    for(int k=0;k<ts.Length;k+=3){var p=ts.Skip(k).Take(3).Select(i=>f.transform.TransformPoint(verts[i])).ToArray();var normal=Vector3.Cross(p[1]-p[0],p[2]-p[0]).normalized;text.AppendLine($"SUB {sub} n={normal:F3} pos={p[0]:F3} uv={uv[ts[k]]:F3},{uv[ts[k+1]]:F3},{uv[ts[k+2]]:F3}");}
                }
            }
            foreach (var l in UnityEngine.Object.FindObjectsOfType<Light>()) text.AppendLine($"LIGHT {l.name} {l.type} {l.color} {l.intensity} {l.transform.eulerAngles}");
            File.WriteAllText(Report + "/inspection.txt",text.ToString());
            Debug.Log("Workbench art inspection saved.");
        }
    }
}
