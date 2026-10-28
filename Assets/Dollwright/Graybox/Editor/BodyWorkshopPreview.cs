using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Dollwright.Graybox.Editor
{
    // Editor-only demo uses an in-memory inventory, never the player's actual save.
    [InitializeOnLoad]
    public static class BodyWorkshopPreview
    {
        const string Flag="Dollwright.BodyWorkshopPreview";
        static IDisposable scope;
        static bool opening;
        static BodyWorkshopPreview()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Flag,false))
                {
                    var inventory=new DollCraftingInventory();
                    for(int i=0;i<3;i++)inventory.AddMaterials(DollMaterialCatalog.Ids);
                    scope=DollCraftingStore.UseTemporary(inventory);
                    DreamTravel.ResetSession();DreamTravel.TravelTo(DreamTravel.WorkbenchScene);opening=true;
                }
                if(state==PlayModeStateChange.ExitingPlayMode || state==PlayModeStateChange.EnteredEditMode)
                {scope?.Dispose();scope=null;opening=false;SessionState.SetBool(Flag,false);}
            };
            EditorApplication.update+=()=>
            {
                if(!opening||!EditorApplication.isPlaying||DreamTravel.IsLoading)return;
                var flow=UnityEngine.Object.FindObjectOfType<WorkbenchDreamUI>();
                if(flow==null||!flow.HomeReady)return;
                flow.OpenCrafting();opening=false;
            };
        }
        [MenuItem("Dollwright/Graybox/Preview body workshops (temporary materials)")]
        public static void Start()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
            if(!EditorSceneManager.SaveOpenScenes())return;
            EditorSceneManager.OpenScene(MainMenuUI.ScenePath);
            SessionState.SetBool(Flag,true);EditorApplication.isPlaying=true;
        }
        public static void OpenWorkbench()
        {
            if(EditorApplication.isPlaying)return;
            if(!EditorSceneManager.SaveOpenScenes())return;
            EditorSceneManager.OpenScene(DreamTravel.WorkbenchScene);
        }
    }
}
