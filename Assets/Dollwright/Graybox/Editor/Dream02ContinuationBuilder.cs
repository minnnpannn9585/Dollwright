using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Dollwright.Graybox.Editor
{
    public static partial class Dream02Builder
    {
        // Migration inputs only. These scenes are archived after the merge, never loaded by gameplay.
        public const string LegacySawLesson=GrayboxBuilder.Root+"/Scenes/Dream02_SawLesson.unity";
        public const string LegacyDoubleCrate=GrayboxBuilder.Root+"/Scenes/Dream02_DoubleCrate.unity";
        public static void Extend() { Dream02ContinuousMerger.Apply(); }
        [MenuItem("Dollwright/Graybox/Open Dream 02 - Saw section")]
        public static void OpenSaw() { OpenContinuousSection("Section_02_Saw"); }
        [MenuItem("Dollwright/Graybox/Open Dream 02 - Double crate section")]
        public static void OpenDouble() { OpenContinuousSection("Section_03_DoubleCrate"); }
        public static void RefineContinuation() { OpenContinuousSection("Section_02_Saw"); }
        static void OpenContinuousSection(string name)
        {
            if(EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(Stage);
            var section=GameObject.Find(name);
            if(section==null)return;
            Selection.activeGameObject=section;
            if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.FrameSelected();
        }
    }
}
