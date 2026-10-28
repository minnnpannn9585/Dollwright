using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Dollwright.Graybox.Editor
{
    public static class WorkbenchCustomizationSetup
    {
        const string Root="Assets/Dollwright/Graybox/";
        static Color Ink=new Color(.20f,.23f,.26f),Paper=new Color(.93f,.92f,.88f,.97f);
        [MenuItem("Dollwright/Graybox/Set up workbench customization")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveOpenScenes())
                throw new InvalidOperationException("Unable to save open scenes.");
            var scene=EditorSceneManager.OpenScene(DreamTravel.WorkbenchScene);
            var flow=UnityEngine.Object.FindObjectOfType<WorkbenchDreamUI>();
            if(flow==null)throw new InvalidOperationException("The workbench flow is missing.");
            var existing=GameObject.Find("Workbench_Customization");
            if(existing!=null)UnityEngine.Object.DestroyImmediate(existing);
            var old=flow.GetComponent<WorkbenchCraftingUI>();if(old!=null)UnityEngine.Object.DestroyImmediate(old);
            foreach(string name in new[]{"Doll01_White_Placeholder","Doll02_Magician_Test"})
            {var obj=GameObject.Find(name);if(obj!=null)UnityEngine.Object.DestroyImmediate(obj);}

            var root=new GameObject("Workbench_Customization").transform;
            var fill=new GameObject("Prototype_Fill").AddComponent<Light>();
            fill.transform.SetParent(root,false);
            fill.transform.rotation=Quaternion.Euler(15,180,0);
            fill.type=LightType.Directional;fill.intensity=.75f;
            fill.color=Color.white;fill.shadows=LightShadows.None;
            var crafting=flow.gameObject.AddComponent<WorkbenchCraftingUI>();flow.crafting=crafting;
            crafting.palette=Palette();
            var bottom=GameObject.Find("Display_Bottom").GetComponent<Renderer>().bounds;
            crafting.shelfRoot=new GameObject("Crafted_Dolls").transform;crafting.shelfRoot.SetParent(root);
            crafting.shelfFirstPosition=new Vector3(bottom.max.x-.43f,bottom.max.y,bottom.center.z+.10f);
            flow.dollCollider=null;flow.magicianCollider=null;flow.initialDoll=null;
            crafting.previewAnchor=new GameObject("Crafting_Preview").transform;crafting.previewAnchor.SetParent(root);
            var tops=UnityEngine.Object.FindObjectsOfType<Renderer>().Where(x=>x.name.StartsWith("Crafting_Platform_Top_Plank")).ToArray();
            if(tops.Length==0)throw new InvalidOperationException("Cannot find the crafting platform.");
            Bounds platform=tops[0].bounds;foreach(var item in tops.Skip(1))platform.Encapsulate(item.bounds);
            crafting.previewAnchor.position=new Vector3(platform.center.x,platform.max.y+.015f,platform.center.z);
            var pose=new GameObject("Crafting_CameraPose").transform;pose.SetParent(root);
            Vector3 target=crafting.previewAnchor.position+Vector3.up*.91f+Vector3.right*.45f;
            pose.position=target+new Vector3(0,.48f,6.4f);
            pose.rotation=Quaternion.LookRotation(target-pose.position);
            flow.craftingPose=pose;flow.craftingFieldOfView=29;
            var hit=new GameObject("Crafting_Platform_ClickTarget").AddComponent<BoxCollider>();hit.transform.SetParent(root);
            hit.transform.position=new Vector3(platform.center.x,platform.max.y,platform.center.z);
            hit.size=new Vector3(platform.size.x,.16f,platform.size.z);
            flow.craftingPlatform=hit;
            var ring=new GameObject("Crafting_Hover_Outline");ring.transform.SetParent(root);
            var line=ring.AddComponent<LineRenderer>();line.sharedMaterial=crafting.palette[4];line.useWorldSpace=true;
            line.positionCount=24;line.loop=true;line.startWidth=line.endWidth=.025f;
            for(int i=0;i<24;i++){float a=i*Mathf.PI/12;line.SetPosition(i,new Vector3(platform.center.x+Mathf.Cos(a)*1.28f,platform.max.y+.025f,platform.center.z+Mathf.Sin(a)*1.28f));}
            crafting.hoverRing=ring;ring.SetActive(false);

            BuildControls(flow,crafting);
            var initial=AssetDatabase.LoadAssetAtPath<DollDefinition>(Root+"Data/Doll01.asset");
            initial.recipe=default;initial.instanceId="starter-01";initial.displayName=initial.recipe.DisplayName;EditorUtility.SetDirty(initial);
            var magician=AssetDatabase.LoadAssetAtPath<DollDefinition>(Root+"Data/Doll02_Magician.asset");
            magician.recipe=new DollRecipe(DollBody.Cotton,DollFace.HappyPaint,DollHead.Magician);EditorUtility.SetDirty(magician);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            string[] paths={DreamTravel.DreamScene,Root+"Scenes/MovementTest.unity",Root+"Scenes/Dream02_MagicBox.unity",
                Root+"Scenes/Dream02_MagicStage.unity"};
            foreach(string path in paths)
            {
                var dream=EditorSceneManager.OpenScene(path);
                var player=UnityEngine.Object.FindObjectOfType<GrayboxPlayer2D>();
                var avatar=player.GetComponent<DreamDollAppearance>()??player.gameObject.AddComponent<DreamDollAppearance>();
                avatar.circle=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Circle.png");
                avatar.square=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Square.png");
                avatar.spriteMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Art/GrayboxSprite.mat");
                foreach(var pickup in UnityEngine.Object.FindObjectsOfType<GrayboxPickup>())
                    pickup.craftingMaterialId=DollMaterialCatalog.FromPickup(pickup.materialId,pickup.kind,path);
                EditorSceneManager.MarkSceneDirty(dream);EditorSceneManager.SaveScene(dream);
            }
            AssetDatabase.SaveAssets();EditorSceneManager.OpenScene(DreamTravel.WorkbenchScene);
            Debug.Log("[Dollwright] Workbench customization installed; existing level geometry preserved.");
        }
        static Material[] Palette()
        {
            string[] names={"Cotton","Wood","Ceramic","Ink","Light","Detail","Bubble"};
            Color[] colors={new Color(.94f,.93f,.89f),new Color(.68f,.65f,.60f),new Color(.83f,.86f,.88f),
                new Color(.22f,.25f,.28f),new Color(.98f,.97f,.93f),new Color(.58f,.59f,.59f),new Color(.70f,.82f,.89f,.18f)};
            var palette=new Material[names.Length];
            for(int i=0;i<names.Length;i++)
            {
                string path=Root+"Art/Craft_"+names[i]+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
                mat.SetColor("_BaseColor",colors[i]);mat.SetFloat("_Smoothness",i == 2 ? .3f : .1f);
                if(i==6)
                {
                    mat.SetFloat("_Surface",1);mat.SetFloat("_Blend",0);mat.SetFloat("_AlphaClip",0);
                    mat.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);mat.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                    mat.SetFloat("_ZWrite",0);mat.SetFloat("_Cull",(float)CullMode.Back);
                    mat.SetOverrideTag("RenderType","Transparent");mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.renderQueue=(int)RenderQueue.Transparent;
                }
                palette[i]=mat;EditorUtility.SetDirty(mat);
            }
            return palette;
        }
        static void BuildControls(WorkbenchDreamUI flow,WorkbenchCraftingUI ui)
        {
            var previous=flow.transform.Find("Customization_Controls");
            if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            previous=flow.transform.Find("Customization_ShelfControls");
            if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var scaler=flow.GetComponent<CanvasScaler>();scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            var controls=Rect(flow.transform,"Customization_Controls",Vector2.zero,Vector2.zero);
            controls.anchorMin=Vector2.zero;controls.anchorMax=Vector2.one;controls.offsetMin=controls.offsetMax=Vector2.zero;
            ui.controls=controls.gameObject;
            var panel=Rect(controls,"Materials",new Vector2(175,10),new Vector2(310,440));panel.anchorMin=panel.anchorMax=new Vector2(0,.5f);
            var background=panel.gameObject.AddComponent<Image>();background.color=new Color(.87f,.87f,.83f,.94f);background.raycastTarget=false;
            var titles=new[]{"素胚","脸部","头饰"};
            var bodyNames=new[]{"棉花","木头","陶瓷"};var headNames=new[]{"","魔术师","泡泡"};
            ui.bodyButtons=new Button[3];ui.faceButtons=new Button[4];ui.headButtons=new Button[3];
            ui.bodyCounts=new Text[3];ui.faceCounts=new Text[4];ui.headCounts=new Text[3];
            for(int category=0;category<3;category++)
            {
                float row=144-category*145;
                Label(panel,"Category_"+category,titles[category],new Vector2(-111,row+49),new Vector2(65,24),17);
                int count=category==1?4:3;float width=category==1?64:89;
                for(int option=0;option<count;option++)
                {
                    float x=(option-(count-1)*.5f)*(width+8);
                    var button=Button(panel,"Option_"+category+"_"+option,new Vector2(x,row-12),new Vector2(width,95));
                    var icon=Rect(button.transform,"Icon",new Vector2(0,20),new Vector2(42,42)).gameObject.AddComponent<DollOptionGraphic>();
                    icon.category=category;icon.option=option;icon.raycastTarget=false;
                    var number=Label(button.transform,"Count","",new Vector2(0,-35),new Vector2(width,20),16);
                    string title=category==0?bodyNames[option]:category==2?headNames[option]:"";
                    if(!string.IsNullOrEmpty(title))Label(button.transform,"MaterialName",title,new Vector2(0,-16),new Vector2(width,18),14);
                    if(category==0){ui.bodyButtons[option]=button;ui.bodyCounts[option]=number;}
                    else if(category==1){ui.faceButtons[option]=button;ui.faceCounts[option]=number;}
                    else{ui.headButtons[option]=button;ui.headCounts[option]=number;}
                }
            }
            ui.dollName=Label(controls,"Doll_Name","",new Vector2(110,130),new Vector2(670,42),25);
            ui.dollName.rectTransform.anchorMin=ui.dollName.rectTransform.anchorMax=new Vector2(.5f,0);
            ui.makeButton=Button(controls,"Craft_Button",new Vector2(110,72),new Vector2(180,52));
            ((RectTransform)ui.makeButton.transform).anchorMin=((RectTransform)ui.makeButton.transform).anchorMax=new Vector2(.5f,0);
            Label(ui.makeButton.transform,"Label","制作",Vector2.zero,new Vector2(150,44),22);
            var shelf=Rect(flow.transform,"Customization_ShelfControls",Vector2.zero,Vector2.zero);
            shelf.anchorMin=Vector2.zero;shelf.anchorMax=Vector2.one;shelf.offsetMin=shelf.offsetMax=Vector2.zero;ui.shelfControls=shelf.gameObject;
            ui.shelfName=Label(shelf,"Doll_Name","",new Vector2(0,103),new Vector2(650,42),24);
            ui.shelfName.rectTransform.anchorMin=ui.shelfName.rectTransform.anchorMax=new Vector2(.5f,0);
            ui.previousPage=Button(shelf,"Previous_Page",new Vector2(-95,52),new Vector2(62,46));
            ui.nextPage=Button(shelf,"Next_Page",new Vector2(95,52),new Vector2(62,46));
            foreach(var button in new[]{ui.previousPage,ui.nextPage})
                ((RectTransform)button.transform).anchorMin=((RectTransform)button.transform).anchorMax=new Vector2(.5f,0);
            Label(ui.previousPage.transform,"Label","‹",Vector2.zero,new Vector2(56,42),30);
            Label(ui.nextPage.transform,"Label","›",Vector2.zero,new Vector2(56,42),30);
            var back=flow.backButton.GetComponent<RectTransform>();back.anchorMin=back.anchorMax=new Vector2(1,1);
            back.anchoredPosition=new Vector2(-42,-42);back.sizeDelta=new Vector2(52,52);
            var backLabel=flow.backButton.GetComponentInChildren<Text>(true);backLabel.text="×";
            backLabel.rectTransform.anchoredPosition=Vector2.zero;backLabel.rectTransform.sizeDelta=back.sizeDelta;
            controls.gameObject.SetActive(false);shelf.gameObject.SetActive(false);
        }
        static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        static Button Button(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var rect=Rect(parent,name,position,size);var image=rect.gameObject.AddComponent<Image>();image.color=Paper;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;return button;
        }
        static Text Label(Transform parent,string name,string content,Vector2 position,Vector2 size,int fontSize)
        {
            var text=Rect(parent,name,position,size).gameObject.AddComponent<Text>();
            text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=fontSize;
            text.text=content;text.color=Ink;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return text;
        }
        [MenuItem("Dollwright/Graybox/Grant customization test materials")]
        public static void GrantTestMaterials()
        {
            DollCraftingStore.Current.AddMaterials(DollMaterialCatalog.Ids);
            Debug.Log("[Dollwright] Added one of each material for customization testing.");
        }
    }
}

