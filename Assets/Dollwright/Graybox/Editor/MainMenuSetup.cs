using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Dollwright.Graybox.Editor
{
    public static class MainMenuSetup
    {
        static readonly Color Ink = new Color(.25f,.28f,.31f);
        static readonly Color Lavender = new Color(.73f,.72f,.81f);
        static readonly Color Blue = new Color(.67f,.77f,.78f);
        [MenuItem("Dollwright/Graybox/Set up main menu")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveOpenScenes()) return;
            var workbench = EditorSceneManager.OpenScene(DreamTravel.WorkbenchScene);
            var background = GameObject.Find("Background_2D_PaintedWorkshop");
            if (background) Object.DestroyImmediate(background);
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(.43f,.41f,.43f);
            var flow = Object.FindObjectOfType<WorkbenchDreamUI>();
            var craft = flow.crafting;
            foreach (Transform child in craft.shelfRoot.Cast<Transform>().ToArray())
                if (child.name == "Starter_Doll_EditorPreview") Object.DestroyImmediate(child.gameObject);
            flow.initialDoll = null; flow.dollCollider = null; flow.magicianCollider = null;
            var oldButton = flow.transform.Find("Return_MainMenu");
            if (oldButton) Object.DestroyImmediate(oldButton.gameObject);
            var home = Button(flow.transform,"Return_MainMenu","主菜单",new Vector2(79,-35),new Vector2(118,42),Blue,18);
            var rect = (RectTransform)home.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0,1);
            home.gameObject.AddComponent<WorkbenchMenuButton>();
            EditorSceneManager.MarkSceneDirty(workbench);
            EditorSceneManager.SaveScene(workbench);
            // Only delete the unused background assets. All furniture materials remain intact.
            foreach (var path in new[] {
                "Assets/Dollwright/WorkbenchArt/SunlitMaterials/Sunlit_Background.mat",
                "Assets/Dollwright/WorkbenchArt/SunlitTextures/Sunlit_Workshop_Background.png" })
            {
                bool used = AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath)
                    .Any(s => AssetDatabase.GetDependencies(s).Contains(path));
                if (!used && AssetDatabase.LoadMainAssetAtPath(path)) AssetDatabase.DeleteAsset(path);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera = new GameObject("MainMenu_Camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.92f,.90f,.85f);
            camera.gameObject.AddComponent<AudioListener>();
            var canvas = new GameObject("MainMenu_Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280,720); scaler.matchWidthOrHeight = .5f;
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            var menu = canvas.AddComponent<MainMenuUI>();
            var main = Rect(canvas.transform,"Main_Panel",Vector2.zero,new Vector2(600,540));
            menu.mainPanel = main.gameObject;
            var accent = Rect(main,"Title_Block",new Vector2(0,185),new Vector2(76,10)).gameObject.AddComponent<Image>();
            accent.color = Lavender; accent.raycastTarget = false;
            Label(main,"Title","玩偶",new Vector2(0,120),new Vector2(420,80),52);
            menu.newGameButton = Button(main,"New_Game","新游戏",new Vector2(0,17),new Vector2(320,66),Blue,25);
            menu.continueButton = Button(main,"Continue","继续",new Vector2(0,-69),new Vector2(320,66),Lavender,25);
            menu.settingsButton = Button(main,"Settings","设置",new Vector2(0,-155),new Vector2(320,66),new Color(.83f,.79f,.73f),25);
            var settings = Rect(canvas.transform,"Settings_Panel",Vector2.zero,new Vector2(500,370));
            settings.gameObject.AddComponent<Image>().color = new Color(.86f,.84f,.82f);
            Label(settings,"Title","设置",new Vector2(0,112),new Vector2(360,60),32);
            Label(settings,"Empty","暂无可设置内容",new Vector2(0,20),new Vector2(360,50),21);
            menu.settingsBackButton = Button(settings,"Back","返回",new Vector2(0,-105),new Vector2(240,58),Blue,23);
            menu.settingsPanel = settings.gameObject;settings.gameObject.SetActive(false);
            menu.errorLabel = Label(canvas.transform,"Error","",new Vector2(0,-300),new Vector2(700,45),18);
            menu.errorLabel.color = new Color(.55f,.24f,.25f);
            EditorSceneManager.SaveScene(scene,MainMenuUI.ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(MainMenuUI.ScenePath,true) }
                .Concat(EditorBuildSettings.scenes.Where(s => s.path != MainMenuUI.ScenePath)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[Dollwright] Main menu installed; workbench background removed. Player save untouched.");
        }
        static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var r = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);
            r.anchoredPosition=position;r.sizeDelta=size;return r;
        }
        static Text Label(Transform parent,string name,string content,Vector2 position,Vector2 size,int fontSize)
        {
            var t=Rect(parent,name,position,size).gameObject.AddComponent<Text>();
            t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=fontSize;
            t.text=content;t.alignment=TextAnchor.MiddleCenter;t.color=Ink;t.raycastTarget=false;return t;
        }
        static Button Button(Transform parent,string name,string title,Vector2 pos,Vector2 size,Color color,int fontSize)
        {
            var r=Rect(parent,name,pos,size);var image=r.gameObject.AddComponent<Image>();image.color=color;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;
            var colors=b.colors;colors.highlightedColor=new Color(1.08f,1.08f,1.08f);colors.pressedColor=new Color(.84f,.84f,.84f);
            colors.disabledColor=new Color(.65f,.65f,.65f,.4f);b.colors=colors;
            Label(r,"Label",title,Vector2.zero,size,fontSize);return b;
        }
    }
}
