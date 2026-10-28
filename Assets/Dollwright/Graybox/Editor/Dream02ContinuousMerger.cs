using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace Dollwright.Graybox.Editor
{
    public static class Dream02ContinuousMerger
    {
        const string Archive="Assets/Dollwright/Graybox/Archive/Dream02_BeforeContinuous";
        sealed class Imported
        {
            public Transform root;
            public float originalFloorLeft,revealAhead;
            public Vector2 spawn;
            public Bounds floor;
        }
        [MenuItem("Dollwright/Graybox/Merge Dream 02 into continuous stage")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before merging scenes.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveOpenScenes())throw new InvalidOperationException("Unable to save open scenes.");
            var stage=EditorSceneManager.OpenScene(Dream02Builder.Stage);
            if(Components<Dream02ContinuousLayout>(stage).Any())
            {
                Debug.Log("[Dollwright] Dream 02 already has one continuous stage.");return;
            }
            string backup="Logs/DollwrightGraybox/MergeInput-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            foreach(string path in new[]{Dream02Builder.Stage,Dream02Builder.Entry,Dream02Builder.LegacySawLesson,Dream02Builder.LegacyDoubleCrate})
                if(File.Exists(path)){File.Copy(path,Path.Combine(backup,Path.GetFileName(path)));File.Copy(path+".meta",Path.Combine(backup,Path.GetFileName(path)+".meta"));}
            File.Copy("ProjectSettings/EditorBuildSettings.asset",Path.Combine(backup,"EditorBuildSettings.asset"));
            var run=Components<GrayboxRun>(stage).Single();
            var layout=run.gameObject.AddComponent<Dream02ContinuousLayout>();
            var fog=Components<GrayboxFog2D>(stage).Single();
            layout.stageRevealAhead=fog.revealAhead;
            var end=Named(stage,"Continuation_Preview").GetComponent<Renderer>().bounds;
            layout.magicStage=Group(stage,"Section_01_MagicStage");
            foreach(var root in stage.GetRootGameObjects().Where(IsContent).ToArray())
                if(root!=layout.magicStage.gameObject)root.transform.SetParent(layout.magicStage,true);
            Remove(layout.magicStage,"Prototype_RightBoundary");
            Remove(layout.magicStage,"Prototype_End");
            Remove(layout.magicStage,"ContinueLabel");

            var saw=Import(stage,Source(Dream02Builder.LegacySawLesson),"Section_02_Saw",new Vector2(end.max.x,end.max.y),run.player);
            layout.sawSection=saw.root;layout.sawLocalLeft=saw.originalFloorLeft;layout.sawRevealAhead=saw.revealAhead;
            var doubles=Import(stage,Source(Dream02Builder.LegacyDoubleCrate),"Section_03_DoubleCrate",new Vector2(saw.floor.max.x,saw.floor.max.y),run.player,true);
            layout.doubleSection=doubles.root;layout.doubleLocalLeft=doubles.originalFloorLeft;layout.doubleRevealAhead=doubles.revealAhead;
            run.returnToWorkbench=true;run.restoreJourneyInventory=true;run.requiredHeadId="H2";
            run.headPickupMessage="获得吹泡泡头部材料。前方出口按 E 醒来。";
            run.missingHeadMessage="先到电锯后的平台，取得吹泡泡头部材料。";
            run.completionMessage="带着吹泡泡头部醒来，返回工作台。";
            var follow=Components<GrayboxCamera2D>(stage).Single();follow.worldMaxX=doubles.floor.max.x+3;
            fog.player=run.player.transform;
            Physics2D.SyncTransforms();
            EditorSceneManager.MarkSceneDirty(stage);EditorSceneManager.SaveScene(stage);

            EnsureFolder(Archive);
            foreach(var original in new[]{Dream02Builder.LegacySawLesson,Dream02Builder.LegacyDoubleCrate})
            {
                string source=Source(original),destination=Archive+"/"+Path.GetFileName(original);
                if(source==destination)continue;
                string error=AssetDatabase.MoveAsset(source,destination);
                if(!string.IsNullOrEmpty(error))throw new IOException(error);
            }
            EditorBuildSettings.scenes=EditorBuildSettings.scenes.Where(x=>
                x.path!=Dream02Builder.LegacySawLesson && x.path!=Dream02Builder.LegacyDoubleCrate
                && x.path!=Archive+"/"+Path.GetFileName(Dream02Builder.LegacySawLesson)
                && x.path!=Archive+"/"+Path.GetFileName(Dream02Builder.LegacyDoubleCrate)).ToArray();
            AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/DollwrightGraybox/continuous-layout.json",JsonUtility.ToJson(new Report
            {
                scene=stage.path,sawOffset=layout.SawOrigin,doubleOffset=layout.DoubleOrigin,
                sawStart=layout.SawStartX,doubleStart=layout.DoubleStartX,cameraMax=follow.worldMaxX,
                players=Components<GrayboxPlayer2D>(stage).Count(),runs=Components<GrayboxRun>(stage).Count(),
                cameras=Components<Camera>(stage).Count(),fogs=Components<GrayboxFog2D>(stage).Count(),
                boxes=Components<GrayboxPushCrate>(stage).Count(),portals=Components<Dream02Portal>(stage).Count()
            },true));
            Debug.Log("[Dollwright] Dream 02 merged: MagicBox -> continuous MagicStage / Saw / DoubleCrate -> Workbench.");
        }
        static Imported Import(Scene destination,string source,string name,Vector2 join,GrayboxPlayer2D player,bool final=false)
        {
            var scene=EditorSceneManager.OpenScene(source,OpenSceneMode.Additive);
            var sourceFloor=Named(scene,"RoomFloor").GetComponent<Renderer>().bounds;
            var sourceRun=Components<GrayboxRun>(scene).Single();
            var offset=new Vector3(join.x-sourceFloor.min.x,join.y-sourceFloor.max.y,0);
            var result=new Imported{root=Group(destination,name),originalFloorLeft=sourceFloor.min.x,
                revealAhead=Components<GrayboxFog2D>(scene).Single().revealAhead,spawn=sourceRun.initialSpawn};
            foreach(var root in scene.GetRootGameObjects().Where(IsContent).ToArray())
            {
                SceneManager.MoveGameObjectToScene(root,destination);root.transform.SetParent(result.root,true);
            }
            result.root.position=offset;
            Remove(result.root,"RoomLeftBoundary");
            foreach(var portal in result.root.GetComponentsInChildren<Dream02Portal>(true).ToArray())Object.DestroyImmediate(portal.gameObject);
            foreach(var crate in result.root.GetComponentsInChildren<GrayboxPushCrate>(true))
            {crate.player=player;crate.minX+=offset.x;crate.maxX+=offset.x;}
            foreach(var trigger in result.root.GetComponentsInChildren<GrayboxTrigger>(true))
                if(trigger.action==GrayboxTrigger.Action.Checkpoint)trigger.checkpoint+=(Vector2)offset;
            foreach(var line in result.root.GetComponentsInChildren<LineRenderer>(true))
            {
                if(!line.useWorldSpace)continue;
                var points=new Vector3[line.positionCount];line.GetPositions(points);
                for(int i=0;i<points.Length;i++)points[i]+=offset;
                line.SetPositions(points);
            }
            var checkpoint=new GameObject(final?"DoubleCrate_Checkpoint":"Saw_Checkpoint");
            checkpoint.transform.SetParent(result.root,false);
            checkpoint.transform.localPosition=new Vector3(result.spawn.x,sourceFloor.max.y+1,0);
            var hit=checkpoint.AddComponent<BoxCollider2D>();hit.isTrigger=true;hit.size=new Vector2(1,2);
            var save=checkpoint.AddComponent<GrayboxTrigger>();save.action=GrayboxTrigger.Action.Checkpoint;
            save.checkpoint=result.spawn+(Vector2)offset;save.message=final?"双箱区检查点":"电锯区检查点";
            Physics2D.SyncTransforms();
            result.floor=result.root.GetComponentsInChildren<Renderer>().Single(x=>x.name=="RoomFloor").bounds;
            EditorSceneManager.CloseScene(scene,true);
            return result;
        }
        static bool IsContent(GameObject root)
        {
            return root.GetComponent<GrayboxRun>()==null && root.GetComponent<GrayboxPlayer2D>()==null
                && root.GetComponent<Camera>()==null && root.GetComponent<GrayboxFog2D>()==null;
        }
        static Transform Group(Scene scene,string name)
        {
            var root=new GameObject(name);SceneManager.MoveGameObjectToScene(root,scene);return root.transform;
        }
        static System.Collections.Generic.IEnumerable<T> Components<T>(Scene scene) where T:Component
            =>scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<T>(true));
        static GameObject Named(Scene scene,string name)
            =>Components<Transform>(scene).Single(x=>x.name==name).gameObject;
        static void Remove(Transform root,string name)
        {
            foreach(var item in root.GetComponentsInChildren<Transform>(true).Where(x=>x.name==name).ToArray())
                Object.DestroyImmediate(item.gameObject);
        }
        static string Source(string original)
        {
            if(File.Exists(original))return original;
            string archived=Archive+"/"+Path.GetFileName(original);
            if(File.Exists(archived))return archived;
            throw new FileNotFoundException("Missing merge source: "+original);
        }
        static void EnsureFolder(string path)
        {
            string parent=Path.GetDirectoryName(path).Replace('\\','/');
            if(!AssetDatabase.IsValidFolder(parent))EnsureFolder(parent);
            if(!AssetDatabase.IsValidFolder(path))AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        [Serializable] sealed class Report
        {
            public string scene;public Vector2 sawOffset,doubleOffset;
            public float sawStart,doubleStart,cameraMax;
            public int players,runs,cameras,fogs,boxes,portals;
        }
    }
}

