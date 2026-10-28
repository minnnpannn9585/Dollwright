using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dollwright.Graybox
{
    // Travel carries the selected crafted doll and unbanked rewards between dream segments.
    public static class DreamTravel
    {
        public const string WorkbenchScene = "Assets/Dollwright/Graybox/Scenes/Workbench.unity";
        public const string DreamScene = "Assets/Dollwright/Graybox/Scenes/Dream01_Graybox.unity";
        public static string SelectedDollId { get; private set; }
        public static string SelectedDollInstanceId { get; private set; }
        public static DollRecipe SelectedRecipe { get; private set; }
        public static bool HasSelectedDoll => SelectedDollId != null;
        static string journeyId;
        public static bool IsLoading { get; private set; }
        public static int LastBases { get; private set; }
        public static int LastFaces { get; private set; }
        public static bool LastHead { get; private set; }
        public static bool HasLastDream { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetSession()
        {
            SelectedDollId = null;
            SelectedDollInstanceId = null;
            SelectedRecipe = default;
            journeyId = null;
            ClearJourney();
            IsLoading = HasLastDream = LastHead = false;
            LastBases = LastFaces = 0;
        }
        public static bool Enter(DollDefinition doll)
        {
            if (doll == null || IsLoading || string.IsNullOrEmpty(doll.dreamScenePath)) return false;
            ClearJourney();
            if (!Load(doll.dreamScenePath)) return false;
            SelectedDollId = doll.dollId;
            SelectedDollInstanceId = doll.instanceId;
            SelectedRecipe = doll.recipe;
            journeyId = Guid.NewGuid().ToString("N");
            return true;
        }
        public static bool Wake(GrayboxRun run)
        {
            if (IsLoading || run == null || !run.Completed) return false;
            if (journeyId == null) journeyId = Guid.NewGuid().ToString("N");
            var inventory = DollCraftingStore.Current;
            if (!inventory.HasBankedJourney(journeyId)
                && !inventory.BankJourney(journeyId, run.CopyCraftingInventory().Values)) return false;
            LastBases = run.Bases;
            LastFaces = run.Faces;
            LastHead = run.HasMagicianHead;
            HasLastDream = true;
            return Load(WorkbenchScene);
        }
        static string arrivalScene;
        static Dictionary<string,GrayboxPickup.Kind> arrivalItems = new Dictionary<string,GrayboxPickup.Kind>();
        static Dictionary<string,string> arrivalMaterials = new Dictionary<string,string>();
        public static void ClearJourney() { arrivalScene=null;arrivalItems.Clear();arrivalMaterials.Clear(); }
        public static bool MoveWithinDream(string path,GrayboxRun run)
        {
            if(IsLoading||run==null)return false;
            var previousScene=arrivalScene;var previousItems=arrivalItems;
            var previousMaterials=arrivalMaterials;
            arrivalScene=path;arrivalItems=run.CopyInventory();
            arrivalMaterials=run.CopyCraftingInventory();
            if(Load(path))return true;
            arrivalScene=previousScene;arrivalItems=previousItems;arrivalMaterials=previousMaterials;return false;
        }
        public static void ApplyJourney(GrayboxRun run)
        {
            if(SceneManager.GetActiveScene().path==arrivalScene)run.RestoreInventory(arrivalItems,arrivalMaterials);
        }
        public static bool TravelTo(string path) { return !IsLoading && Load(path); }
        static bool Load(string path)
        {
            IsLoading = true;
            try
            {
#if UNITY_EDITOR
                var operation = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                    path, new LoadSceneParameters(LoadSceneMode.Single));
#else
                var operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
#endif
                if (operation == null) { IsLoading = false; return false; }
                operation.completed += _ => IsLoading = false;
                return true;
            }
            catch (Exception exception)
            {
                IsLoading = false;
                Debug.LogException(exception);
                return false;
            }
        }
    }
}
