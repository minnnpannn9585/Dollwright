using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dollwright.Graybox
{
    public sealed class GrayboxRun : MonoBehaviour
    {
        public GrayboxPlayer2D player;
        public bool movementOnly;
        public bool useCustomHud;
        public Vector2 initialSpawn = new Vector2(1, 0.76f);
        public string requiredHeadId = "H1";
        public bool returnToWorkbench;
        [Min(0)] public float respawnDelay = 1f;
        public GrayboxRewind Rewind { get; private set; }
        public bool IsRecovering => Time.time < recoverUntil || (Rewind != null && Rewind.IsRewinding);
        public string headPickupMessage, missingHeadMessage, completionMessage;
        public bool restoreJourneyInventory;
        readonly Dictionary<string, GrayboxPickup.Kind> materialKinds = new Dictionary<string, GrayboxPickup.Kind>();
        readonly Dictionary<string, string> craftingMaterials = new Dictionary<string, string>();
        public Dictionary<string, string> CopyCraftingInventory() => new Dictionary<string, string>(craftingMaterials);
        public Dictionary<string, GrayboxPickup.Kind> CopyInventory() { return new Dictionary<string, GrayboxPickup.Kind>(materialKinds); }
        public void RestoreInventory(Dictionary<string, GrayboxPickup.Kind> items, Dictionary<string, string> recipes = null)
        {
            foreach (var item in items)
                Collect(item.Key, item.Value, recipes != null && recipes.TryGetValue(item.Key, out var id) ? id : null);
        }
        void Awake() { Rewind = GetComponent<GrayboxRewind>(); if (Rewind == null) Rewind = gameObject.AddComponent<GrayboxRewind>(); }
        public bool ExitAvailable { get; private set; }
        public int Bases { get; private set; }
        public int Faces { get; private set; }
        public bool HasMagicianHead { get; private set; }
        public bool Completed { get; private set; }
        public int Respawns { get; private set; }
        public float Elapsed => Completed ? completedSeconds : Time.time - startedAt;
        public IReadOnlyCollection<string> Collected => collected;
        readonly HashSet<string> collected = new HashSet<string>();
        Vector2 checkpoint;
        float startedAt, completedSeconds, messageUntil, recoverUntil;
        string message;
        GUIStyle textStyle, titleStyle;
        Font uiFont;

        void Start()
        {
            checkpoint = initialSpawn;
            startedAt = Time.time;
            Rewind.Capture(initialSpawn);
            if (restoreJourneyInventory) DreamTravel.ApplyJourney(this);
            var appearance = player.GetComponent<DreamDollAppearance>();
            if (appearance != null && DreamTravel.HasSelectedDoll) appearance.Apply(DreamTravel.SelectedRecipe);
            foreach(var pickup in FindObjectsOfType<GrayboxPickup>())if(collected.Contains(pickup.materialId))pickup.gameObject.SetActive(false);
            ShowMessage(movementOnly ? "先试行走和跳跃：踩上三级台阶，再落回地面。"
                : "进入快乐梦境。先试移动与跳跃，探索游乐园。", 5);
        }
        void Update()
        {
            if (Rewind.IsRewinding) return;
            if (Input.GetKeyDown(KeyCode.R)) Restart();
            if (ExitAvailable && Input.GetKeyDown(KeyCode.E)) ConfirmExit();
        }
        public void Restart()
        {
#if UNITY_EDITOR
            // Prototype scenes need not alter the project's existing build list.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                SceneManager.GetActiveScene().path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
#endif
        }
        public bool Collect(string id, GrayboxPickup.Kind kind, string craftingMaterialId = null)
        {
            if (Completed || Rewind.IsRewinding || !collected.Add(id)) return false;
            materialKinds[id] = kind;
            craftingMaterials[id] = DollMaterialCatalog.Contains(craftingMaterialId) ? craftingMaterialId
                : DollMaterialCatalog.FromPickup(id, kind, SceneManager.GetActiveScene().path);
            switch (kind)
            {
                case GrayboxPickup.Kind.Base: Bases++; ShowMessage("获得素胚材料", 2); break;
                case GrayboxPickup.Kind.Face: Faces++; ShowMessage("获得手绘面孔", 2); break;
                case GrayboxPickup.Kind.Head:
                    HasMagicianHead = collected.Contains(requiredHeadId);
                    ShowMessage(string.IsNullOrEmpty(headPickupMessage) ? "获得魔术师头部！箱内的梦境留给魔术师人偶。" : headPickupMessage, 5);
                    break;
            }
            return true;
        }
        public void SetCheckpoint(Vector2 point, string label)
        {
            if (Rewind.IsRewinding) return;
            checkpoint = point;
            Rewind.Capture(point);
            ShowMessage(label, 3);
        }
        public void Respawn()
        {
            if (Completed || IsRecovering) return;
            Respawns++;
            ExitAvailable = false;
            player.Teleport(checkpoint);
            recoverUntil = Time.time + (movementOnly ? 0 : respawnDelay);
            ShowMessage("回到本段起点，稍候恢复操作。已收集的材料保留。", 3);
        }
        public void SetExitAvailable(bool available)
        {
            ExitAvailable = available;
            if (available && !Completed) ShowMessage("按 E 醒来，带着材料返回工作台。", 9999);
            else if (!Completed) messageUntil = Time.time;
        }
        public bool ConfirmExit() { return ExitAvailable && !IsRecovering && TryComplete(); }
        public bool TryComplete()
        {
            if (movementOnly || Completed) return Completed;
            if (!HasMagicianHead)
            {
                ShowMessage(string.IsNullOrEmpty(missingHeadMessage) ? "先去马戏团的大魔术箱顶，取得魔术师头部。" : missingHeadMessage, 5);
                return false;
            }
            completedSeconds = Time.time - startedAt;
            Completed = true;
            ShowMessage(string.IsNullOrEmpty(completionMessage) ? "已醒来。下一步：回工作台制作魔术师人偶。" : completionMessage, 9999);
            WriteReport();
            if (returnToWorkbench) StartCoroutine(ReturnAfterWake());
            return true;
        }
        System.Collections.IEnumerator ReturnAfterWake()
        {
            yield return new WaitForSeconds(0.6f);
            if (!DreamTravel.Wake(this)) ShowMessage("返回工作台失败，请重试。", 5);
        }
        public void ShowMessage(string value, float seconds)
        {
            message = value;
            messageUntil = Time.time + seconds;
        }
        void OnGUI()
        {
            if (useCustomHud) return;
            if (textStyle == null)
            {
                uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial" }, 18);
                textStyle = new GUIStyle(GUI.skin.label) { font = uiFont, fontSize = 18, wordWrap = true };
                titleStyle = new GUIStyle(textStyle) { fontSize = 22, fontStyle = FontStyle.Bold };
            }
            GUI.color = new Color(0.08f, 0.10f, 0.13f, 0.88f);
            GUI.DrawTexture(new Rect(18, 18, Mathf.Min(820, Screen.width - 36), movementOnly ? 76 : 104), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(34, 24, Screen.width - 68, 30),
                movementOnly ? "Dollwright · 移动灰盒" : "Dollwright · 快乐梦境 v03", titleStyle);
            GUI.Label(new Rect(34, 56, Screen.width - 68, 26), "A / D / 方向键：移动　空格：跳跃（长按更高）　Z：回溯　R：重开", textStyle);
            if (!movementOnly)
                GUI.Label(new Rect(34, 86, Screen.width - 68, 30),
                    $"素胚 {Bases}/3    面孔 {Faces}/2    魔术师头部 {(HasMagicianHead ? "已获得" : "未获得")}    用时 {Elapsed:F1} 秒    掉落 {Respawns}", textStyle);
            if (Time.time < messageUntil)
            {
                float width = Mathf.Min(880, Screen.width - 36);
                GUI.color = new Color(0.08f, 0.10f, 0.13f, 0.9f);
                GUI.DrawTexture(new Rect((Screen.width - width) / 2, Screen.height - 82, width, 62), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect((Screen.width - width) / 2 + 14, Screen.height - 74, width - 28, 54), message, textStyle);
            }
        }
        [Serializable] sealed class Report
        {
            public string utc, scene;
            public float seconds;
            public int jumps, respawns, bases, faces;
            public bool magicianHead;
            public string[] materials;
        }
        void WriteReport()
        {
            var report = new Report {
                utc = DateTime.UtcNow.ToString("o"), scene = SceneManager.GetActiveScene().name,
                seconds = completedSeconds, jumps = player.JumpCount, respawns = Respawns,
                bases = Bases, faces = Faces, magicianHead = HasMagicianHead,
                materials = new List<string>(collected).ToArray()
            };
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/DollwrightGraybox"));
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "playtest-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".json");
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
            Debug.Log("[Dollwright] Graybox completed: " + path);
        }
    }
}
