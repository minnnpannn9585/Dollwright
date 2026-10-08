using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dollwright.Graybox.Editor
{
    public static class GrayboxBuilder
    {
        public const string Root = "Assets/Dollwright/Graybox";
        public const string MovementScene = Root + "/Scenes/MovementTest.unity";
        public const string DreamScene = Root + "/Scenes/Dream01_Graybox.unity";
        static Sprite square, circle, triangle;
        static Material spriteMaterial;
        static PhysicsMaterial2D noFriction, crateFriction;
        static readonly Color GroundColor = new Color(0.29f, 0.33f, 0.38f);
        static readonly Color BackgroundColor = new Color(0.88f, 0.89f, 0.90f);
        static readonly Color StructureColor = new Color(0.63f, 0.66f, 0.69f);

        [MenuItem("Dollwright/Graybox/Build Both Test Scenes")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before generating scenes.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareArt();
            BuildMovement();
            BuildDream();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            WriteLayoutReport();
            Debug.Log("[Dollwright] Generated MovementTest and Dream01_Graybox. Existing scenes and build settings were not modified.");
        }

        [MenuItem("Dollwright/Graybox/Open Movement Test")]
        public static void OpenMovement() { Open(MovementScene); }
        [MenuItem("Dollwright/Graybox/Open Dream 01")]
        public static void OpenForUser() { Open(DreamScene); }
        static void Open(string path)
        {
            if (EditorApplication.isPlaying) return;
            EditorApplication.isPaused = false;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(path);
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(new Vector3(7, 2.5f, 0), Quaternion.identity, 9, true);
            Selection.activeGameObject = UnityEngine.Object.FindObjectOfType<GrayboxPlayer2D>().gameObject;
            EditorApplication.ExecuteMenuItem("Window/General/Game");
        }

        static void PrepareArt()
        {
            Directory.CreateDirectory(Root + "/Art");
            Directory.CreateDirectory(Root + "/Scenes");
            square = MakeSprite("Square", 0);
            circle = MakeSprite("Circle", 1);
            triangle = MakeSprite("Triangle", 2);
            const string materialPath = Root + "/Art/GrayboxSprite.mat";
            spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (spriteMaterial == null)
            {
                var shader = Shader.Find("Dollwright/GrayboxSprite");
                if (shader == null) throw new InvalidOperationException("Graybox sprite shader has not imported.");
                spriteMaterial = new Material(shader);
                AssetDatabase.CreateAsset(spriteMaterial, materialPath);
            }
            noFriction = MakePhysicsMaterial("PlayerFriction", 0);
            crateFriction = MakePhysicsMaterial("CrateFriction", 0.6f);
        }

        static Sprite MakeSprite(string name, int shape)
        {
            string path = Root + "/Art/" + name + ".png";
            if (!File.Exists(path))
            {
                const int pixels = 64;
                var texture = new Texture2D(pixels, pixels, TextureFormat.RGBA32, false);
                for (int y = 0; y < pixels; y++)
                    for (int x = 0; x < pixels; x++)
                    {
                        float u = (x + 0.5f) / pixels, v = (y + 0.5f) / pixels;
                        bool inside = shape == 0 || (shape == 1
                            ? (u - 0.5f) * (u - 0.5f) + (v - 0.5f) * (v - 0.5f) < 0.24f
                            : Mathf.Abs(u - 0.5f) < (1 - v) * 0.5f);
                        texture.SetPixel(x, y, inside ? Color.white : Color.clear);
                    }
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static PhysicsMaterial2D MakePhysicsMaterial(string name, float friction)
        {
            string path = Root + "/Art/" + name + ".physicsMaterial2D";
            var value = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (value != null) return value;
            value = new PhysicsMaterial2D(name) { friction = friction, bounciness = 0 };
            AssetDatabase.CreateAsset(value, path);
            return value;
        }

        static void BuildMovement()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var environment = Group("01_TestGeometry");
            Platform(environment, "Floor", -3, 27, 0);
            Block(environment, "LeftBoundary", new Vector2(-3.5f, 4), new Vector2(1, 10), GroundColor, true);
            Platform(environment, "Step_01", 6, 8.5f, 1.15f);
            Platform(environment, "Step_02", 9.5f, 12, 2.3f);
            Platform(environment, "Step_03", 13, 15.5f, 3.45f);
            Label(environment, "MovementLabel", "WALK / JUMP", new Vector2(2.5f, 4.8f), 0.12f);
            var run = CreateRun(true);
            var player = CreatePlayer(run);
            CreateCamera(player, 27);
            EditorSceneManager.SaveScene(scene, MovementScene);
        }

        static void BuildDream()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var geometry = Group("01_CollisionGeometry");
            var scenery = Group("02_Landmarks_NoCollision");
            var resources = Group("03_Materials");
            var triggers = Group("04_CheckpointsAndPrompts");
            var foreground = Group("05_ForegroundOcclusion");
            // A traversable ground route with three short, visible gaps.
            Platform(geometry, "Ground_EntryToCircus", -3, 77, 0, 1);
            Platform(geometry, "Ground_Carousel", 80, 100, 0, 1);
            Platform(geometry, "Ground_Flags", 103, 124, 0, 1);
            Platform(geometry, "Ground_Exit", 126.5f, 159, 0, 1);
            Block(geometry, "LeftBoundary", new Vector2(-3.5f, 3), new Vector2(1, 12), GroundColor, true);
            Block(geometry, "RightBoundary", new Vector2(159.5f, 3), new Vector2(1, 12), GroundColor, true);
            Gate(scenery, "ParkGate", 5, "DREAM PARK");
            // The push crate is a candidate interaction, not a required key.
            var crate = Block(geometry, "PushCrate_Optional", new Vector2(13.3f, 0.62f), new Vector2(1.2f, 1.2f), StructureColor, true);
            var body = crate.AddComponent<Rigidbody2D>();
            body.mass = 2;
            body.drag = 2;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            crate.GetComponent<BoxCollider2D>().sharedMaterial = crateFriction;
            Label(crate.transform, "CrateMark", "+", new Vector2(13.3f, 0.62f), 0.14f);
            Platform(geometry, "EntryStep_01", 16, 18.6f, 1.5f);
            Platform(geometry, "EntryStep_02", 20, 22.6f, 2.4f);
            Platform(geometry, "EntryLanding", 24, 26.8f, 1.2f);
            Pickup(resources, "B1", GrayboxPickup.Kind.Base, new Vector2(17.3f, 2.1f));
            Gate(scenery, "CircusEntry", 33, "CIRCUS");
            Block(scenery, "CircusCanopy", new Vector2(53, 6.5f), new Vector2(27, 3), new Color(0.79f, 0.79f, 0.78f), false, -15, triangle);
            Pickup(resources, "F1", GrayboxPickup.Kind.Face, new Vector2(37, 0.8f));
            Platform(geometry, "CircusStep_01", 40, 42.8f, 1.1f);
            Platform(geometry, "CircusStep_02", 44, 46.5f, 2.2f);
            Platform(geometry, "CircusStep_03", 48, 50.5f, 3.3f);
            // This block is deliberately solid for the Happiness doll.
            Block(geometry, "MagicBox_SolidForHappiness", new Vector2(55.5f, 2.2f), new Vector2(8, 4.4f), StructureColor, true);
            Block(scenery, "MagicBox_RoofTrim", new Vector2(55.5f, 4.25f), new Vector2(8.1f, 0.3f), GroundColor, false, 2);
            Block(scenery, "MagicBox_LockedFront", new Vector2(55.5f, 2.1f), new Vector2(5.8f, 3.35f), new Color(0.72f, 0.70f, 0.75f), false, 2);
            Label(scenery, "BoxLabel", "MAGIC BOX", new Vector2(55.5f, 2.7f), 0.10f);
            Label(scenery, "BoxLockLabel", "MAGICIAN ROUTE", new Vector2(55.5f, 1.6f), 0.065f);
            Pickup(resources, "H1", GrayboxPickup.Kind.Head, new Vector2(55.5f, 5.15f));
            Platform(geometry, "CircusExitStep", 62, 65, 2.3f);
            Pickup(resources, "B2", GrayboxPickup.Kind.Base, new Vector2(70, 0.8f));
            // Three short park traversals, with space to see each landing.
            Platform(geometry, "CarouselJumpStart", 73, 77, 1.2f);
            Platform(geometry, "CarouselLanding", 80, 83.5f, 1.2f);
            Block(scenery, "CarouselColumn", new Vector2(89, 1.8f), new Vector2(0.6f, 3.6f), StructureColor, false, -8);
            Block(scenery, "CarouselCanopy", new Vector2(89, 3.8f), new Vector2(8, 2.2f), StructureColor, false, -8, triangle);
            Label(scenery, "CarouselLabel", "CAROUSEL", new Vector2(89, 5.5f), 0.09f);
            Platform(geometry, "FlagsJumpStart", 97, 100, 0.8f);
            Platform(geometry, "FlagsLanding", 103, 106, 0.8f);
            Block(geometry, "LowCircusObstacle", new Vector2(112, 0.55f), new Vector2(1.2f, 1.1f), StructureColor, true);
            Pickup(resources, "F2", GrayboxPickup.Kind.Face, new Vector2(109, 0.8f));
            for (int i = 0; i < 7; i++)
            {
                var flag = Block(scenery, "Flag_" + i, new Vector2(107 + i * 1.3f, 4.3f), new Vector2(0.7f, 0.7f), StructureColor, false, -8, triangle);
                flag.transform.rotation = Quaternion.Euler(0, 0, 180);
            }
            Platform(geometry, "BubbleJumpStart", 121, 124, 0.65f);
            Platform(geometry, "BubbleLanding", 126.5f, 129.5f, 0.65f);
            Label(scenery, "BubbleStallLabel", "BUBBLES", new Vector2(143, 4.7f), 0.09f);
            Block(scenery, "BubbleStallRoof", new Vector2(143, 2.9f), new Vector2(4.5f, 1.4f), StructureColor, false, -7, triangle);
            for (int i = 0; i < 3; i++)
                Block(scenery, "BubblePreview_" + i, new Vector2(138 + i * 1.6f, 3.7f + i * 0.7f), Vector2.one * (0.6f + i * 0.12f), new Color(0.81f, 0.85f, 0.87f), false, -6, circle);
            Pickup(resources, "B3", GrayboxPickup.Kind.Base, new Vector2(143, 0.8f));
            var run = CreateRun(false);
            var player = CreatePlayer(run);
            CreateCamera(player, 159);
            var counter = Block(foreground, "StallCounter_HidesB3", new Vector2(143, 0.85f), new Vector2(3.8f, 1.7f), StructureColor, false, 30);
            counter.AddComponent<GrayboxOccluder>().player = player.transform;
            Trigger(triggers, "EntryPushHint", new Vector2(10.5f, 1), new Vector2(1, 2), GrayboxTrigger.Action.Message,
                "箱子可以推，也可以跳上去。先试一次跳跃，前方落脚点会逐渐显露。");
            Checkpoint(triggers, "CircusCheckpoint", 34, "进入马戏团。大魔术箱暂时进不去，试试登上箱顶。");
            Trigger(triggers, "MagicBoxReminder", new Vector2(50.8f, 2), new Vector2(0.7f, 4), GrayboxTrigger.Action.Message,
                "箱内路线尚未开放。沿台阶登上箱顶，寻找头部材料。");
            Checkpoint(triggers, "CarouselCheckpoint", 71, "离开马戏团。前方是三段短跳跃，起跳前先看落点。");
            Checkpoint(triggers, "FlagsCheckpoint", 95, "彩旗区：留意前方的小缺口。");
            Checkpoint(triggers, "BubbleCheckpoint", 119, "泡泡摊就在前方，遮挡后可能藏着材料。");
            Gate(scenery, "DreamExit", 153, "WAKE UP");
            Trigger(triggers, "WakeUpExit", new Vector2(154, 1.5f), new Vector2(1.5f, 3), GrayboxTrigger.Action.Exit, "");
            MakeFog(player);
            EditorSceneManager.SaveScene(scene, DreamScene);
        }

        static Transform Group(string name) { return new GameObject(name).transform; }
        static GameObject Block(Transform parent, string name, Vector2 center, Vector2 size,
            Color color, bool solid, int sorting = 0, Sprite shape = null)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = new Vector3(center.x, center.y, 0);
            obj.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = shape != null ? shape : square;
            renderer.sharedMaterial = spriteMaterial;
            renderer.color = color;
            renderer.sortingOrder = sorting;
            if (solid)
            {
                var collider = obj.AddComponent<BoxCollider2D>();
                collider.sharedMaterial = noFriction;
            }
            return obj;
        }
        static void Platform(Transform parent, string name, float left, float right, float top, float depth = 0.35f)
        {
            Block(parent, name, new Vector2((left + right) / 2, top - depth / 2), new Vector2(right - left, depth), GroundColor, true);
        }
        static void Label(Transform parent, string name, string text, Vector2 position, float size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = new Vector3(position.x, position.y, -0.1f);
            var label = obj.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 48;
            label.characterSize = size;
            label.color = new Color(0.22f, 0.26f, 0.31f);
            label.GetComponent<MeshRenderer>().sortingOrder = 20;
            // A scaled crate parent must not distort the letter marker.
            if (parent != null && parent.localScale != Vector3.one)
                obj.transform.localScale = new Vector3(1 / parent.localScale.x, 1 / parent.localScale.y, 1);
        }
        static void Gate(Transform parent, string name, float x, string label)
        {
            var gate = new GameObject(name).transform;
            gate.SetParent(parent, false);
            Block(gate, "Post_Left", new Vector2(x - 2, 2.5f), new Vector2(0.5f, 5), StructureColor, false, -10);
            Block(gate, "Post_Right", new Vector2(x + 2, 2.5f), new Vector2(0.5f, 5), StructureColor, false, -10);
            Block(gate, "Header", new Vector2(x, 5), new Vector2(4.5f, 0.7f), StructureColor, false, -10);
            Label(gate, "Label", label, new Vector2(x, 5), 0.075f);
        }
        static GrayboxRun CreateRun(bool movement)
        {
            var run = new GameObject("06_GrayboxRun").AddComponent<GrayboxRun>();
            run.movementOnly = movement;
            return run;
        }
        static GrayboxPlayer2D CreatePlayer(GrayboxRun run)
        {
            var obj = new GameObject("Player_WhiteHappinessDoll");
            obj.transform.position = run.initialSpawn;
            var rb = obj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2.8f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var collider = obj.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.65f, 1.4f);
            collider.sharedMaterial = noFriction;
            var player = obj.AddComponent<GrayboxPlayer2D>();
            player.run = run;
            run.player = player;
            Block(obj.transform, "Body", new Vector2(1, 0.57f), new Vector2(0.52f, 0.85f), new Color(0.97f, 0.97f, 0.94f), false, 10);
            Block(obj.transform, "Head", new Vector2(1, 1.22f), new Vector2(0.65f, 0.65f), new Color(0.97f, 0.97f, 0.94f), false, 11, circle);
            Block(obj.transform, "Eye_Left", new Vector2(0.88f, 1.24f), Vector2.one * 0.055f, GroundColor, false, 12, circle);
            Block(obj.transform, "Eye_Right", new Vector2(1.12f, 1.24f), Vector2.one * 0.055f, GroundColor, false, 12, circle);
            return player;
        }
        static void CreateCamera(GrayboxPlayer2D player, float maxX)
        {
            var obj = new GameObject("MainCamera_StraightSideView");
            obj.tag = "MainCamera";
            obj.transform.position = new Vector3(6, 3.25f, -10);
            var camera = obj.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100;
            obj.AddComponent<AudioListener>();
            var follow = obj.AddComponent<GrayboxCamera2D>();
            follow.target = player.transform;
            follow.worldMaxX = maxX;
        }
        static void Pickup(Transform parent, string id, GrayboxPickup.Kind kind, Vector2 point)
        {
            var obj = new GameObject("Material_" + id);
            obj.transform.SetParent(parent, false);
            obj.transform.position = point;
            var collider = obj.AddComponent<CircleCollider2D>();
            collider.radius = 0.42f;
            collider.isTrigger = true;
            var pickup = obj.AddComponent<GrayboxPickup>();
            pickup.materialId = id;
            pickup.kind = kind;
            Color color = kind == GrayboxPickup.Kind.Base ? new Color(0.4f, 0.63f, 0.55f)
                : kind == GrayboxPickup.Kind.Face ? new Color(0.75f, 0.50f, 0.59f) : new Color(0.86f, 0.66f, 0.32f);
            var visual = Block(obj.transform, "Icon", point, Vector2.one * 0.6f, color, false, 10,
                kind == GrayboxPickup.Kind.Head ? square : circle);
            if (kind == GrayboxPickup.Kind.Head) visual.transform.rotation = Quaternion.Euler(0, 0, 45);
            pickup.visual = visual.transform;
            Label(obj.transform, "ID", id, point + Vector2.up * 0.55f, 0.07f);
        }
        static void Trigger(Transform parent, string name, Vector2 center, Vector2 size,
            GrayboxTrigger.Action action, string message, Vector2 checkpoint = default)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.position = center;
            var collider = obj.AddComponent<BoxCollider2D>();
            collider.size = size;
            collider.isTrigger = true;
            var trigger = obj.AddComponent<GrayboxTrigger>();
            trigger.action = action;
            trigger.message = message;
            trigger.checkpoint = checkpoint;
        }
        static void Checkpoint(Transform parent, string name, float x, string message)
        {
            Trigger(parent, name, new Vector2(x, 1.5f), new Vector2(1, 3), GrayboxTrigger.Action.Checkpoint, message, new Vector2(x, 0.76f));
            Block(parent, name + "_Marker", new Vector2(x, 0.1f), new Vector2(0.55f, 0.2f), new Color(0.65f, 0.72f, 0.70f), false, 1);
        }
        static void MakeFog(GrayboxPlayer2D player)
        {
            var root = new GameObject("07_Fog_RevealAhead");
            var fog = root.AddComponent<GrayboxFog2D>();
            fog.player = player.transform;
            root.transform.position = new Vector3(player.transform.position.x + fog.revealAhead + fog.curtainWidth / 2, 4, 0);
            Block(root.transform, "UnseenArea", root.transform.position, new Vector2(fog.curtainWidth, 40), BackgroundColor, false, 100);
            for (int i = 0; i < 4; i++)
            {
                var color = BackgroundColor;
                color.a = 0.2f + i * 0.2f;
                float edge = player.transform.position.x + fog.revealAhead;
                Block(root.transform, "FogEdge_" + i, new Vector2(edge - 1.4f + i * 0.4f, 4), new Vector2(0.42f, 40), color, false, 101);
            }
        }
        static void WriteLayoutReport()
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/DollwrightGraybox"));
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "layout.txt"),
                "Dollwright Dream01 graybox v01\n" +
                "Editor: " + Application.unityVersion + "\n" +
                "Movement: " + MovementScene + "\nDream: " + DreamScene + "\n" +
                "Move speed: 4.2; jump speed: 11.5; gravity scale: 2.8\n" +
                "Full held jump ideal height: 2.41; ideal horizontal reach: 3.52\n" +
                "Materials: B1 (17.3,2.1), F1 (37,0.8), H1 (55.5,5.15), B2 (70,0.8), F2 (109,0.8), optional B3 (143,0.8)\n" +
                "Gaps: [77,80], [100,103], [124,126.5]\n" +
                "Length 162 units; 60 seconds is a design target, not a timer or a measured first-play result.\n");
        }
    }
}
