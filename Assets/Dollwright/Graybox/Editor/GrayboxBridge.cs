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
            public bool playing, paused, compiling;
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
                    compiling = EditorApplication.isCompiling
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
                case "status": result = "ready"; break;
                case "refresh":
                    if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before importing edited scripts.");
                    AssetDatabase.Refresh(); result = "refresh_requested"; break;
                case "open_scene":
                    if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
                    if (request.scene != GrayboxBuilder.MovementScene && request.scene != GrayboxBuilder.DreamScene)
                        throw new InvalidOperationException("Only the two graybox scenes may be opened by the bridge.");
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                        if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save the modified scene before switching.");
                    EditorSceneManager.OpenScene(request.scene);
                    result = "scene_opened";
                    break;
                case "play":
                    if (UnityEngine.Object.FindObjectOfType<GrayboxRun>() == null) throw new InvalidOperationException("Open a graybox scene first.");
                    EditorApplication.isPlaying = true; result = "play_requested"; break;
                case "stop": EditorApplication.isPlaying = false; result = "stop_requested"; break;
                case "pause": EditorApplication.isPaused = true; result = "paused"; break;
                case "resume": EditorApplication.isPaused = false; result = "resumed"; break;
                case "capture":
                    if (!EditorApplication.isPlaying || EditorApplication.isPaused) throw new InvalidOperationException("Capture requires unpaused Play Mode.");
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
                    run.player.Teleport(new Vector2(53, 5.12f));
                    result = "circus_preview_ready";
                    break;
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
        static void AtomicWrite(string path, string content)
        {
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, content);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporary, path);
        }
    }
}
