using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dollwright.Graybox.Editor
{
    // A local file bridge for this prototype. Commands are restricted to graybox scenes.
    [InitializeOnLoad]
    public static class GrayboxBridge
    {
        [Serializable] sealed class Request { public string id, command, scene; }
        [Serializable] sealed class State
        {
            public string utc, unityVersion, project, scene, requestId, result, error, lastRuntimeError;
            public bool playing, paused, compiling, sceneDirty;
            public float playerX, playerY;
            public int bases, faces, jumps;
            public bool magicianHead, completed;
        }
        static readonly string Folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/DollwrightGraybox/Bridge"));
        static double nextPoll;
        static string result = "ready", error = "", lastRuntimeError = "";
        static GrayboxBridge()
        {
            EditorApplication.update += Poll;
            Application.logMessageReceived += (message, trace, type) => {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) lastRuntimeError = message;
            };
        }
        static void Poll()
        {
            if (Application.isBatchMode || EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 0.5;
            try
            {
                Directory.CreateDirectory(Folder);
                string path = Path.Combine(Folder, "request.json");
                if (File.Exists(path) && !EditorApplication.isCompiling)
                {
                    var request = JsonUtility.FromJson<Request>(File.ReadAllText(path));
                    if (request != null && !string.IsNullOrEmpty(request.id)
                        && request.id != SessionState.GetString("DollwrightGraybox.LastRequest", ""))
                    {
                        SessionState.SetString("DollwrightGraybox.LastRequest", request.id);
                        error = "";
                        try { Execute(request); } catch (Exception ex) { error = ex.Message; result = "failed"; }
                    }
                }
                var run = UnityEngine.Object.FindObjectOfType<GrayboxRun>();
                var state = new State {
                    utc = DateTime.UtcNow.ToString("o"), unityVersion = Application.unityVersion,
                    project = Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
                    scene = SceneManager.GetActiveScene().path,
                    requestId = SessionState.GetString("DollwrightGraybox.LastRequest", ""),
                    result = result, error = error, lastRuntimeError = lastRuntimeError,
                    playing = EditorApplication.isPlaying, paused = EditorApplication.isPaused,
                    compiling = EditorApplication.isCompiling,
                    sceneDirty = SceneManager.GetActiveScene().isDirty
                };
                if (run != null)
                {
                    state.bases = run.Bases; state.faces = run.Faces;
                    state.magicianHead = run.HasMagicianHead; state.completed = run.Completed;
                    if (run.player != null)
                    {
                        state.playerX = run.player.transform.position.x;
                        state.playerY = run.player.transform.position.y;
                        state.jumps = run.player.JumpCount;
                    }
                }
                AtomicWrite(Path.Combine(Folder, "state.json"), JsonUtility.ToJson(state, true));
            }
            catch (IOException) { nextPoll = EditorApplication.timeSinceStartup + 1; }
        }
        static void Execute(Request request)
        {
            switch (request.command)
            {
                case "setup_main_menu": MainMenuSetup.Apply(); result="main_menu_installed"; break;
                case "workbench_art": WorkbenchArtPass.Apply(); result="workbench_art_applied"; break;
                case "inspect_art": WorkbenchArtPass.Inspect(); result="workbench_art_inspected"; break;
                case "merge_dream02": Dream02ContinuousMerger.Apply(); result="dream02_merged"; break;
                case "setup_customization": WorkbenchCustomizationSetup.Apply(); result="customization_installed"; break;
                case "craft_open":
                    UnityEngine.Object.FindObjectOfType<WorkbenchDreamUI>().OpenCrafting();
                    result="crafting_opened"; break;
                case "craft_preview":
                    var craft=UnityEngine.Object.FindObjectOfType<WorkbenchCraftingUI>();
                    craft.SelectBody(1);craft.SelectFace(3);craft.SelectHead(1);
                    result="crafting_previewed"; break;
                case "craft_reset":
                    var resetCraft=UnityEngine.Object.FindObjectOfType<WorkbenchCraftingUI>();
                    resetCraft.SelectBody(0);resetCraft.SelectFace(0);resetCraft.SelectHead(0);
                    result="crafting_reset"; break;
                case "craft_close":
                    UnityEngine.Object.FindObjectOfType<WorkbenchDreamUI>().CloseSelection();
                    result="crafting_closed"; break;
                case "apply_shelf": ShelfSelectionSetup.Apply(); result = "shelf_applied"; break;
                case "inspect_shelf": ShelfSelectionSetup.Inspect(); result = "inspected"; break;
                case "build_dream02": Dream02Builder.Build(false); result="dream02_built"; break;
                case "test_dream02": FlowTestRunner.RunDream02(); result="tests_started"; break;
                case "test_dream02_puzzle": FlowTestRunner.RunDream02Puzzle(); result="tests_started"; break;
                case "test_regression": FlowTestRunner.RunRegression(); result="tests_started"; break;
                case "test_legacy_box": FlowTestRunner.RunRegression(true); result="tests_started"; break;
                case "extend_dream02": Dream02Builder.Extend(); result="continuation_built"; break;
                case "rewind": UnityEngine.Object.FindObjectOfType<GrayboxRun>().Rewind.TryRewind(); result="rewind_requested"; break;
                case "test_continuation": FlowTestRunner.RunContinuation(); result="tests_started"; break;
                case "test_rewind": FlowTestRunner.RunContinuation(true); result="tests_started"; break;
                case "refine_saw": Dream02Builder.RefineContinuation(); result="refined"; break;
                case "status": result = "ready"; break;
                case "apply_workbench_flow": WorkbenchFlowSetup.Apply(); result = "flow_applied"; break;
                case "test_route": FlowTestRunner.RunRoute(); result = "tests_started"; break;
                case "preview_body_workshops": BodyWorkshopPreview.Start(); result="preview_started"; break;
                case "test_body_workshop": FlowTestRunner.RunBodyWorkshop(); result="tests_started"; break;
                case "test_flow": FlowTestRunner.Run(); result = "tests_started"; break;
                case "test_ui": FlowTestRunner.RunUI(); result = "tests_started"; break;
                case "fix_ui_icons": WorkbenchFlowSetup.RepairIconRenderers(); result = "icons_repaired"; break;
                case "ui_select":
                    UnityEngine.Object.FindObjectOfType<WorkbenchDreamUI>().bedButton.onClick.Invoke();
                    result = "selection_opened"; break;
                case "ui_enter":
                    var shelfUI = UnityEngine.Object.FindObjectOfType<WorkbenchDreamUI>();
                    shelfUI.TrySelectAtScreenPoint(shelfUI.viewCamera.WorldToScreenPoint(shelfUI.dollCollider.bounds.center));
                    result = "dream_requested"; break;
                case "refresh":
                    if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before importing edited scripts.");
                    AssetDatabase.Refresh(); result = "refresh_requested"; break;
                case "open_scene":
                    if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
                    if (request.scene != MainMenuUI.ScenePath && request.scene != GrayboxBuilder.MovementScene && request.scene != GrayboxBuilder.DreamScene && request.scene != DreamTravel.WorkbenchScene && request.scene != Dream02Builder.Entry && request.scene != Dream02Builder.Stage)
                        throw new InvalidOperationException("Only the two graybox scenes may be opened by the bridge.");
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                        if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the modified scene before switching.");
                    EditorSceneManager.OpenScene(request.scene);
                    result = "scene_opened";
                    break;
                case "play":
                    if (UnityEngine.Object.FindObjectOfType<MainMenuUI>() == null && UnityEngine.Object.FindObjectOfType<GrayboxRun>() == null && UnityEngine.Object.FindObjectOfType<WorkbenchDreamUI>() == null) throw new InvalidOperationException("Open a Dollwright scene first.");
                    EditorApplication.isPlaying = true; result = "play_requested"; break;
                case "stop": EditorApplication.isPlaying = false; result = "stop_requested"; break;
                case "pause": EditorApplication.isPaused = true; result = "paused"; break;
                case "resume": EditorApplication.isPaused = false; result = "resumed"; break;
                case "capture":
                    if (!EditorApplication.isPlaying || EditorApplication.isPaused) throw new InvalidOperationException("Capture requires unpaused Play Mode.");
                    Application.runInBackground = true;
                    EditorApplication.ExecuteMenuItem("Window/General/Game");
                    ScreenCapture.CaptureScreenshot(Path.Combine(Folder, "game-view.png"));
                    result = "capture_requested";
                    break;
                case "restart":
                    if (!EditorApplication.isPlaying) throw new InvalidOperationException("Restart requires Play Mode.");
                    var activeRun = UnityEngine.Object.FindObjectOfType<GrayboxRun>();
                    if (activeRun == null) throw new InvalidOperationException("Open a graybox scene first.");
                    activeRun.Restart();
                    result = "restart_requested";
                    break;
                case "preview_circus":
                    if (!EditorApplication.isPlaying) throw new InvalidOperationException("Preview requires Play Mode.");
                    var run = UnityEngine.Object.FindObjectOfType<GrayboxRun>();
                    if (run == null || run.movementOnly) throw new InvalidOperationException("Open Dream 01 first.");
                    run.player.SetInputOverride(0);
                    run.player.Teleport(new Vector2(61.6f, 7.22f));
                    result = "circus_preview_ready";
                    break;
                case "preview_flags":
                    Preview(new Vector2(29, 4.42f));
                    result = "flags_preview_ready";
                    break;
                case "preview_push":
                    Preview(new Vector2(11.5f, 0.72f));
                    result = "push_preview_ready";
                    break;
                case "preview_exit":
                    Preview(new Vector2(135.5f, 3.92f));
                    result = "exit_preview_ready";
                    break;
                case "preview_circus_push":
                    Preview(new Vector2(44, 2.96f));
                    result = "circus_push_preview_ready"; break;
                case "restore_test_settings":
                    if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
                    // Unity's batch test runner changed the original false setting to true.
                    PlayerSettings.runInBackground = false;
                    AssetDatabase.SaveAssets();
                    result = "test_settings_restored";
                    break;
                default: throw new InvalidOperationException("Unknown graybox command.");
            }
        }
        static void Preview(Vector2 point)
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Preview requires Play Mode.");
            var run = UnityEngine.Object.FindObjectOfType<GrayboxRun>();
            if (run == null || run.movementOnly) throw new InvalidOperationException("Open Dream 01 first.");
            Application.runInBackground = true;
            run.player.SetInputOverride(0);
            run.player.Teleport(point);
        }
        static void AtomicWrite(string path, string content)
        {
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, content);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
        }
    }
}
