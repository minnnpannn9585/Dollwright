using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dollwright.Graybox.Editor
{
    public static class WorkbenchFlowSetup
    {
        const string Root = GrayboxBuilder.Root;
        static readonly Color Ink = new Color(.22f, .25f, .29f);
        static readonly Color Paper = new Color(.93f, .92f, .88f);

        // Targeted migration of the user's edited scenes; never invokes the scene generator.
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
            if (!EditorSceneManager.SaveOpenScenes()) throw new InvalidOperationException("Cannot save current scene.");
            string backup = "Logs/DollwrightGraybox/BeforeFlow-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            foreach (string path in new[] { GrayboxBuilder.DreamScene, DreamTravel.WorkbenchScene, "ProjectSettings/EditorBuildSettings.asset" })
                File.Copy(path, Path.Combine(backup, Path.GetFileName(path)), true);

            var dream = EditorSceneManager.OpenScene(GrayboxBuilder.DreamScene);
            var player = UnityEngine.Object.FindObjectOfType<GrayboxPlayer2D>();
            var run = UnityEngine.Object.FindObjectOfType<GrayboxRun>();
            var platform = GameObject.Find("CircusRest").GetComponent<BoxCollider2D>();
            Physics2D.SyncTransforms();
            var bounds = platform.bounds;
            var crate = GameObject.Find("PushCrate_Circus");
            if (crate == null)
            {
                var source = GameObject.Find("PushCrate_Required");
                crate = UnityEngine.Object.Instantiate(source, source.transform.parent);
                crate.name = "PushCrate_Circus";
                foreach (Transform child in crate.transform.Cast<Transform>().ToArray())
                    if (child.name != "PushMark") UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            var mistakenMarker = GameObject.Find("PushMark (1)");
            if (mistakenMarker != null) UnityEngine.Object.DestroyImmediate(mistakenMarker);
            crate.transform.position = new Vector3(bounds.min.x + 2.2f, bounds.max.y + .6f, 0);
            var push = crate.GetComponent<GrayboxPushCrate>();
            push.player = player;
            push.minX = bounds.min.x + .6f;
            push.maxX = bounds.max.x - .6f;
            // A checkpoint on the approach side is reachable before moving the crate.
            MoveCheckpoint("CircusCheckpoint", bounds.min.x + .7f, bounds.max.y,
                "把箱子推到舞台右边缘，再踩箱跳上高台。长按空格跳得更高。");

            var counter = GameObject.Find("BubbleCounter_Solid").GetComponent<BoxCollider2D>();
            Physics2D.SyncTransforms();
            float top = counter.bounds.max.y, start = counter.bounds.max.x, end = start + 8;
            var floor = GameObject.Find("BubbleExitWalkway");
            if (floor == null)
            {
                floor = UnityEngine.Object.Instantiate(counter.gameObject, counter.transform.parent);
                floor.name = "BubbleExitWalkway";
            }
            floor.transform.position = new Vector3((start + end)/2, top/2, 0);
            floor.transform.localScale = new Vector3(end-start, top, 1);
            var gate = GameObject.Find("DreamExit").transform;
            var gateLabel = gate.GetComponentInChildren<TextMesh>();
            float exitX = start + 4.5f;
            gate.position += new Vector3(exitX - gateLabel.transform.position.x, top - (gateLabel.transform.position.y - 5), 0);
            var exit = GameObject.Find("WakeUpExit").transform;
            exit.position = new Vector3(exitX, top + 1.5f, 0);
            GameObject.Find("RightBoundary").transform.position = new Vector3(end + .5f, top + 4, 0);
            Camera.main.GetComponent<GrayboxCamera2D>().worldMaxX = end;
            MoveCheckpoint("ExitCheckpoint", exitX - 1.5f, top, "已经到达梦境出口，按 E 醒来，返回工作台。");
            GameObject.Find("BonusPushHint").GetComponent<GrayboxTrigger>().message = "推箱靠近泡泡摊，踩箱登台取材。醒来的门就在摊位右侧。";
            GameObject.Find("BubbleStallLabel").GetComponent<TextMesh>().text = "BUBBLES";
            GameObject.Find("DescentCheckpoint").GetComponent<GrayboxTrigger>().message = "穿过泡泡摊后就是出口。";
            run.returnToWorkbench = true;
            EditorSceneManager.SaveScene(dream);

            Directory.CreateDirectory(Root + "/Data");
            AssetDatabase.Refresh();
            string dollPath = Root + "/Data/Doll01.asset";
            var doll = AssetDatabase.LoadAssetAtPath<DollDefinition>(dollPath);
            if (doll == null)
            {
                doll = ScriptableObject.CreateInstance<DollDefinition>();
                AssetDatabase.CreateAsset(doll, dollPath);
            }
            var workbench = EditorSceneManager.OpenScene(DreamTravel.WorkbenchScene);
            if (UnityEngine.Object.FindObjectOfType<WorkbenchDreamUI>() == null) CreateUI(doll);
            EditorSceneManager.SaveScene(workbench);
            var paths = new[] { DreamTravel.WorkbenchScene, GrayboxBuilder.DreamScene };
            EditorBuildSettings.scenes = paths.Select(p => new EditorBuildSettingsScene(p, true))
                .Concat(EditorBuildSettings.scenes.Where(s => !paths.Contains(s.path))).ToArray();
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = UnityEngine.Object.FindObjectOfType<WorkbenchDreamUI>().gameObject;
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            Debug.Log("[Dollwright] Workbench flow applied. Saved original scenes in " + backup);
        }

        static void MoveCheckpoint(string name, float x, float top, string message)
        {
            var trigger = GameObject.Find(name).GetComponent<GrayboxTrigger>();
            trigger.transform.position = new Vector3(x, top + 1.5f, 0);
            trigger.checkpoint = new Vector2(x, top + .76f);
            trigger.message = message;
            GameObject.Find(name + "_Marker").transform.position = new Vector3(x, top + .1f, 0);
        }

        public static void RepairIconRenderers()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
            if (SceneManager.GetActiveScene().path != DreamTravel.WorkbenchScene)
                throw new InvalidOperationException("Open Workbench first.");
            foreach (var icon in UnityEngine.Object.FindObjectsOfType<DreamIconGraphic>(true))
            {
                if (icon.GetComponent<CanvasRenderer>() == null) icon.gameObject.AddComponent<CanvasRenderer>();
                icon.SetAllDirty();
            }
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        static void CreateUI(DollDefinition doll)
        {
            var root = new GameObject("Workbench_DreamUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;
            var ui = root.AddComponent<WorkbenchDreamUI>();
            ui.initialDoll = doll;
            var bed = Panel(root.transform, "BedButton", new Vector2(-158, 134), new Vector2(236, 172), Paper);
            bed.rectTransform.anchorMin = bed.rectTransform.anchorMax = new Vector2(1, 0);
            ui.bedButton = bed.gameObject.AddComponent<Button>();
            ui.bedButton.targetGraphic = bed;
            Icon(bed.transform, "BedIcon", new Vector2(0, 20), new Vector2(118, 94), DreamIconGraphic.Icon.Bed, Ink);
            Label(bed.transform, "BedLabel", "入梦", new Vector2(0, -53), new Vector2(190, 36), 27, Ink);
            UnityEventTools.AddPersistentListener(ui.bedButton.onClick, ui.OpenSelection);

            var footer = Panel(root.transform, "SessionSummary", new Vector2(348, 84), new Vector2(610, 88), new Color(.14f,.16f,.19f,.88f));
            footer.rectTransform.anchorMin = footer.rectTransform.anchorMax = Vector2.zero;
            ui.summaryLabel = Label(footer.transform, "Summary", "选择一个人偶，走进它的梦。", Vector2.zero, new Vector2(570, 64), 23, Color.white);

            var shade = Panel(root.transform, "DollSelection", Vector2.zero, Vector2.zero, new Color(.08f,.10f,.13f,.74f));
            shade.rectTransform.anchorMin = Vector2.zero;
            shade.rectTransform.anchorMax = Vector2.one;
            shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
            ui.selectionPanel = shade.gameObject;
            var sheet = Panel(shade.transform, "SelectionSheet", Vector2.zero, new Vector2(680, 780), Paper);
            Label(sheet.transform, "Title", "选择入梦人偶", new Vector2(0, 314), new Vector2(500, 60), 34, Ink);
            Label(sheet.transform, "Subtitle", "让人偶带你走进它的梦", new Vector2(0, 258), new Vector2(500, 40), 22, Ink);
            var card = Panel(sheet.transform, "Doll01Card", new Vector2(0, 29), new Vector2(420, 380), new Color(.76f,.79f,.78f));
            ui.dollButton = card.gameObject.AddComponent<Button>();
            ui.dollButton.targetGraphic = card;
            var border = Panel(card.transform, "SelectedUnderline", new Vector2(0, -185), new Vector2(420, 10), new Color(.35f,.46f,.43f));
            border.raycastTarget = false;
            ui.selectionBorder = border;
            Icon(card.transform, "DollIcon", new Vector2(0, 74), new Vector2(156, 176), DreamIconGraphic.Icon.Doll, new Color(.98f,.97f,.93f));
            ui.nameLabel = Label(card.transform, "DollName", doll.displayName, new Vector2(0, -48), new Vector2(390, 45), 29, Ink);
            ui.dreamLabel = Label(card.transform, "DreamName", doll.dreamName, new Vector2(0, -101), new Vector2(390, 40), 23, Ink);
            ui.abilitiesLabel = Label(card.transform, "Abilities", doll.abilities, new Vector2(0, -145), new Vector2(390, 36), 21, Ink);
            UnityEventTools.AddPersistentListener(ui.dollButton.onClick, ui.SelectInitialDoll);
            var enter = Panel(sheet.transform, "EnterDreamButton", new Vector2(0, -236), new Vector2(420, 70), new Color(.32f,.41f,.39f));
            ui.enterButton = enter.gameObject.AddComponent<Button>();
            ui.enterButton.targetGraphic = enter;
            Label(enter.transform, "Label", "进入梦境", Vector2.zero, new Vector2(380, 52), 27, Color.white);
            UnityEventTools.AddPersistentListener(ui.enterButton.onClick, ui.EnterSelectedDream);
            ui.statusLabel = Label(sheet.transform, "Status", "", new Vector2(0, -299), new Vector2(580, 38), 22, Ink);
            var close = Panel(sheet.transform, "CloseButton", new Vector2(0, -341), new Vector2(210, 44), new Color(.85f,.84f,.80f));
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.targetGraphic = close;
            Label(close.transform, "Label", "返回工作台", Vector2.zero, new Vector2(190, 40), 21, Ink);
            UnityEventTools.AddPersistentListener(closeButton.onClick, ui.CloseSelection);
            shade.gameObject.SetActive(false);
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
                new GameObject("UI_EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
        static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
        static Image Panel(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var image = Rect(parent, name, position, size).gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }
        static Text Label(Transform parent, string name, string content, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var label = Rect(parent, name, position, size).gameObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.text = content;
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }
        static void Icon(Transform parent, string name, Vector2 position, Vector2 size, DreamIconGraphic.Icon icon, Color color)
        {
            var graphic = Rect(parent, name, position, size).gameObject.AddComponent<DreamIconGraphic>();
            graphic.icon = icon;
            graphic.color = color;
            graphic.raycastTarget = false;
        }
    }
}
