using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dollwright.Graybox
{
    // Session state survives scene changes. It is not a crafting/inventory implementation.
    public static class DreamTravel
    {
        public const string WorkbenchScene = "Assets/Dollwright/Graybox/Scenes/Workbench.unity";
        public const string DreamScene = "Assets/Dollwright/Graybox/Scenes/Dream01_Graybox.unity";
        public static string SelectedDollId { get; private set; }
        public static bool IsLoading { get; private set; }
        public static int LastBases { get; private set; }
        public static int LastFaces { get; private set; }
        public static bool LastHead { get; private set; }
        public static bool HasLastDream { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            SelectedDollId = null;
            IsLoading = HasLastDream = LastHead = false;
            LastBases = LastFaces = 0;
        }
        public static bool Enter(DollDefinition doll)
        {
            if (doll == null || IsLoading || string.IsNullOrEmpty(doll.dreamScenePath)) return false;
            if (!Load(doll.dreamScenePath)) return false;
            SelectedDollId = doll.dollId;
            return true;
        }
        public static bool Wake(GrayboxRun run)
        {
            if (IsLoading || run == null || !run.Completed) return false;
            LastBases = run.Bases;
            LastFaces = run.Faces;
            LastHead = run.HasMagicianHead;
            HasLastDream = true;
            return Load(WorkbenchScene);
        }
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
