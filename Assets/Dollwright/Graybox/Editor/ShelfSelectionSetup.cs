using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.Events;
namespace Dollwright.Graybox.Editor
{
    public static class ShelfSelectionSetup
    {
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first.");
            if (!EditorSceneManager.SaveOpenScenes()) return;
            string backup = "Logs/DollwrightGraybox/BeforeShelf-" + System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
            Directory.CreateDirectory(backup);
            foreach (var path in new[] { DreamTravel.DreamScene, DreamTravel.WorkbenchScene })
                File.Copy(path, Path.Combine(backup, Path.GetFileName(path)));
            var dream = EditorSceneManager.OpenScene(DreamTravel.DreamScene);
            var crate = GameObject.Find("PushCrate_Circus").GetComponent<GrayboxPushCrate>();
            crate.fallAndRespawn = true;
            crate.supportPlatform = GameObject.Find("CircusRest").GetComponent<BoxCollider2D>();
            EditorSceneManager.MarkSceneDirty(dream);
            EditorSceneManager.SaveScene(dream);

            var scene = EditorSceneManager.OpenScene(DreamTravel.WorkbenchScene);
            var ui = Object.FindObjectOfType<WorkbenchDreamUI>();
            var old = ui.transform.Find("DollSelection");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var summary = ui.transform.Find("SessionSummary");
            if (summary != null) Object.DestroyImmediate(summary.gameObject);
            ui.selectionPanel = null;
            ui.viewCamera = Camera.main;
            var root = GameObject.Find("Shelf_DollSelection") ?? new GameObject("Shelf_DollSelection");
            foreach (Transform child in root.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            string materialPath = GrayboxBuilder.Root + "/Art/ShelfDoll_Clay.mat";
            var clay = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (clay == null) { clay = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(clay, materialPath); }
            clay.SetColor("_BaseColor", new Color(.94f,.93f,.88f));
            clay.SetFloat("_Smoothness", .15f);
            string inkPath = GrayboxBuilder.Root + "/Art/ShelfDoll_Ink.mat";
            var ink = AssetDatabase.LoadAssetAtPath<Material>(inkPath);
            if (ink == null) { ink = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(ink, inkPath); }
            ink.SetColor("_BaseColor", new Color(.15f,.18f,.21f));
            var bottom = GameObject.Find("Display_Bottom").GetComponent<Renderer>().bounds;
            float y = bottom.max.y;
            var doll = new GameObject("Doll01_White_Placeholder");
            doll.transform.SetParent(root.transform);
            doll.transform.position = new Vector3(bottom.center.x, y, bottom.center.z + .1f);
            Part(doll.transform, "Head", PrimitiveType.Sphere, new Vector3(0,.99f,0), new Vector3(.38f,.41f,.34f), clay);
            Part(doll.transform, "Body", PrimitiveType.Capsule, new Vector3(0,.58f,0), new Vector3(.29f,.28f,.23f), clay);
            foreach (float side in new[] { -1f, 1f })
            {
                Part(doll.transform, side < 0 ? "LeftLeg" : "RightLeg", PrimitiveType.Capsule, new Vector3(side*.095f,.18f,0), new Vector3(.115f,.18f,.13f), clay);
                var arm = Part(doll.transform, side < 0 ? "LeftArm" : "RightArm", PrimitiveType.Capsule, new Vector3(side*.223f,.59f,0), new Vector3(.10f,.20f,.11f), clay);
                arm.transform.localRotation = Quaternion.Euler(0,0,side*12);
                Part(doll.transform, "Eye", PrimitiveType.Sphere, new Vector3(side*.064f,1.02f,.156f), new Vector3(.026f,.034f,.016f), ink);
            }
            var hit = doll.AddComponent<BoxCollider>();
            hit.center = new Vector3(0,.61f,0);
            hit.size = new Vector3(.62f,1.24f,.4f);
            ui.dollCollider = hit;
            var pose = new GameObject("ShelfFocus_CameraPose").transform;
            pose.SetParent(root.transform);
            var target = new Vector3(bottom.center.x, 2.25f, bottom.center.z);
            pose.position = target + new Vector3(0,.30f,6.5f);
            pose.rotation = Quaternion.LookRotation(target-pose.position);
            ui.focusPose = pose;
            ui.focusFieldOfView = 25;

            if (ui.backButton != null) Object.DestroyImmediate(ui.backButton);
            var back = new GameObject("Shelf_BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = back.GetComponent<RectTransform>();
            rect.SetParent(ui.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f,0);
            rect.anchoredPosition = new Vector2(0,65);
            rect.sizeDelta = new Vector2(250,56);
            back.GetComponent<Image>().color = new Color(.93f,.92f,.88f,.96f);
            UnityEventTools.AddPersistentListener(back.GetComponent<Button>().onClick, ui.CloseSelection);
            var label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var text = label.GetComponent<Text>();
            label.transform.SetParent(back.transform, false);
            text.rectTransform.sizeDelta = rect.sizeDelta;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(.22f,.25f,.29f);
            text.text = "返回工作台 · Esc";
            text.raycastTarget = false;
            ui.backButton = back;
            back.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        static GameObject Part(Transform parent, string name, PrimitiveType kind, Vector3 position, Vector3 size, Material material)
        {
            var part = GameObject.CreatePrimitive(kind);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            part.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            return part;
        }
        public static void Inspect()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play first.");
            if (!EditorSceneManager.SaveOpenScenes()) return;
            EditorSceneManager.OpenScene(DreamTravel.WorkbenchScene);
            var lines = Object.FindObjectsOfType<Renderer>().Select(r => r.name + " | " + r.transform.position.ToString("F3") + " | center " + r.bounds.center.ToString("F3") + " | size " + r.bounds.size.ToString("F3"));
            File.WriteAllLines("Logs/DollwrightGraybox/shelf-bounds.txt", lines);
        }
    }
}
