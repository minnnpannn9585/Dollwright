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

        [MenuItem("Dollwright/Graybox/Rebuild Dream 01")]
        public static void RebuildDream()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareArt();
            BuildDream();
            AssetDatabase.SaveAssets();
            WriteLayoutReport();
            Debug.Log("[Dollwright] Rebuilt Dream01_Graybox v03 only.");
        }

        static void BuildDream()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var geometry = Group("01_CollisionGeometry");
            var scenery = Group("02_Landmarks_NoCollision");
            var resources = Group("03_Materials");
            var triggers = Group("04_CheckpointsAndPrompts");
            var run = CreateRun(false);
            var player = CreatePlayer(run);

            // Safe tutorial: the 3.2-unit ledge exceeds an unaided full jump (2.41).
            Platform(geometry, "Ground_EntryPush", -3, 20, 0, 1);
            Block(geometry, "LeftBoundary", new Vector2(-3.5f, 4), new Vector2(1, 12), GroundColor, true);
            Block(geometry, "RightBoundary", new Vector2(136.5f, 4), new Vector2(1, 12), GroundColor, true);
            Gate(scenery, "ParkGate", 2.5f, "DREAM PARK");
            Block(geometry, "EntrySmallHop", new Vector2(6, 0.275f), new Vector2(1.1f, 0.55f), StructureColor, true);
            PushCrate(geometry, player, "PushCrate_Required", 13.2f, 12.4f, 19.4f);
            Platform(geometry, "RequiredHighLedge", 20, 25.5f, 3.2f, 3.2f);
            Pickup(resources, "B1", GrayboxPickup.Kind.Base, new Vector2(22.5f, 3.95f));
            Label(scenery, "PushLessonLabel", "PUSH  >  CLIMB", new Vector2(16.5f, 4.4f), 0.085f);
            Trigger(triggers, "EntryPushHint", new Vector2(10.5f, 1.5f), new Vector2(1, 3), GrayboxTrigger.Action.Message,
                "前方高台跳不上去。贴着箱子向右走，把它推到高台边，再踩箱登台。长按空格跳得更高。");

            // Flags are the warmup BEFORE the circus. There is deliberately no floor below.
            Platform(geometry, "FloatingFlags_01", 27.2f, 30.6f, 3.7f);
            Platform(geometry, "FloatingFlags_02", 32.4f, 35.8f, 4.6f);
            Platform(geometry, "FloatingFlags_03", 37.7f, 41.4f, 3.4f);
            Pickup(resources, "F1", GrayboxPickup.Kind.Face, new Vector2(33.8f, 5.35f));
            for (int i = 0; i < 9; i++)
            {
                var flag = Block(scenery, "Flag_" + i, new Vector2(26.5f + i * 1.85f, 6.8f + Mathf.Sin(i * 0.65f) * 0.3f),
                    new Vector2(0.7f, 0.85f), StructureColor, false, -8, triangle);
                flag.transform.rotation = Quaternion.Euler(0, 0, 180);
            }
            Checkpoint(triggers, "FlagsCheckpoint", 24, "彩旗区：看好下一块平台。掉落会回到这里，材料保留。", 3.2f);

            // A wide rest beat separates the floating warmup from the main ascent.
            Platform(geometry, "CircusRest", 43, 51.5f, 2.2f, 0.6f);
            Gate(scenery, "CircusEntry", 47, "CIRCUS", 2.2f);
            Checkpoint(triggers, "CircusCheckpoint", 47, "先在舞台前歇一歇。沿外侧平台登上大魔术箱，寻找头部材料。", 2.2f);
            Pickup(resources, "F2", GrayboxPickup.Kind.Face, new Vector2(48.5f, 2.95f));
            Block(scenery, "CircusCanopy", new Vector2(64.5f, 11), new Vector2(29, 3.5f),
                new Color(0.81f, 0.81f, 0.80f), false, -15, triangle);
            Platform(geometry, "CircusClimb_01", 52.5f, 55.7f, 3.5f);
            Platform(geometry, "CircusClimb_02", 57, 60, 5);
            Platform(geometry, "CircusClimb_03", 60.9f, 62.7f, 6.5f);
            Block(geometry, "MagicBox_SolidForHappiness", new Vector2(68, 5.1f), new Vector2(8, 5.8f), StructureColor, true);
            Block(scenery, "MagicBox_RoofTrim", new Vector2(68, 7.85f), new Vector2(8, 0.3f), GroundColor, false, 2);
            Block(scenery, "MagicBox_LockedFront", new Vector2(68, 4.9f), new Vector2(6.4f, 4.5f), new Color(0.72f, 0.70f, 0.77f), false, 2);
            Label(scenery, "BoxLabel", "MAGIC BOX", new Vector2(68, 5.3f), 0.12f);
            Label(scenery, "BoxLockLabel", "MAGICIAN ROUTE", new Vector2(68, 4.3f), 0.075f);
            Pickup(resources, "H1", GrayboxPickup.Kind.Head, new Vector2(68.5f, 8.75f));
            Checkpoint(triggers, "SummitCheckpoint", 70, "登顶成功！沿右侧宽平台向下走，出口在游乐园尽头。", 8);

            // The climax ends on the box roof. The descending platforms become wider.
            Platform(geometry, "Descent_01", 73.3f, 78.1f, 6.6f, 0.5f);
            Platform(geometry, "Descent_02", 79.3f, 84.4f, 4.9f, 0.5f);
            Platform(geometry, "Descent_03", 85.5f, 90.8f, 3.2f, 0.5f);
            Pickup(resources, "B2", GrayboxPickup.Kind.Base, new Vector2(82, 5.65f));
            Platform(geometry, "AfterBoxRest", 92, 105, 3.2f, 0.4f);
            // Visible from the main route; reserved for a later magician visit, no level 2 yet.
            Platform(geometry, "RevisitAlcoveFloor", 93.8f, 103.2f, 1.3f, 0.35f);
            var violet = new Color(0.63f, 0.57f, 0.73f);
            Block(geometry, "MagicianRevisitEntrance_Reserved", new Vector2(94.1f, 2.05f), new Vector2(0.6f, 1.5f), violet, true);
            Block(geometry, "RevisitAlcoveEndWall", new Vector2(103, 2.05f), new Vector2(0.4f, 1.5f), StructureColor, true);
            Label(scenery, "RevisitStar", "*", new Vector2(94.1f, 2.1f), 0.16f);
            Label(scenery, "RevisitLabel", "MAGICIAN ONLY", new Vector2(98.5f, 2.05f), 0.07f);
            Trigger(triggers, "RevisitPrompt", new Vector2(95, 4.3f), new Vector2(2, 2), GrayboxTrigger.Action.Message,
                "桥下的星纹屏障暂时无法通过。下次带魔术师人偶，来看看另一条路线。（本版仅预留入口）");
            Checkpoint(triggers, "DescentCheckpoint", 101, "前面是出口与泡泡摊，最后一份素胚可以选择收集。", 3.2f);
            Platform(geometry, "QuietLanding", 106.2f, 114, 1.4f, 0.55f);
            Platform(geometry, "Ground_ExitRest", 114, 136, 0, 1);

            // Wake is before the optional stall. Explicit E avoids ending exploration on contact.
            Checkpoint(triggers, "ExitCheckpoint", 116, "已经找到出口。按 E 醒来，或去右侧泡泡摊再找一份素胚。");
            Gate(scenery, "DreamExit", 118.5f, "E : WAKE UP");
            Trigger(triggers, "WakeUpExit", new Vector2(118.5f, 1.5f), new Vector2(3, 3), GrayboxTrigger.Action.Exit, "");
            PushCrate(geometry, player, "PushCrate_Bonus", 124, 123.2f, 128.4f);
            Platform(geometry, "BubbleCounter_Solid", 129, 134, 3.2f, 3.2f);
            Pickup(resources, "B3", GrayboxPickup.Kind.Base, new Vector2(131.2f, 3.95f));
            Block(scenery, "BubbleStallRoof", new Vector2(131.5f, 6.2f), new Vector2(5.7f, 1.3f), StructureColor, false, -7, triangle);
            Label(scenery, "BubbleStallLabel", "BUBBLES / BONUS", new Vector2(131.5f, 6.7f), 0.09f);
            for (int i = 0; i < 3; i++)
                Block(scenery, "BubblePreview_" + i, new Vector2(124.5f + i * 1.4f, 4.4f + i * 0.5f),
                    Vector2.one * (0.6f + i * 0.12f), new Color(0.81f, 0.85f, 0.87f), false, -6, circle);
            Trigger(triggers, "BonusPushHint", new Vector2(122, 1.5f), new Vector2(1, 3), GrayboxTrigger.Action.Message,
                "可选素胚在摊位顶。再试一次：推箱靠近柜台，踩箱跳上去。收集后回左侧出口按 E 醒来。");
            CreateCamera(player, 136, true);
            MakeFog(player);
            EditorSceneManager.SaveScene(scene, DreamScene);
        }

        static void PushCrate(Transform parent, GrayboxPlayer2D player, string name, float x, float minX, float maxX)
        {
            var crate = Block(parent, name, new Vector2(x, 0.6f), new Vector2(1.2f, 1.2f), new Color(0.66f, 0.62f, 0.53f), true);
            var body = crate.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var push = crate.AddComponent<GrayboxPushCrate>();
            push.player = player;
            push.minX = minX;
            push.maxX = maxX;
            Label(crate.transform, "PushMark", ">", new Vector2(x, 0.6f), 0.14f);
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
        static void Gate(Transform parent, string name, float x, string label, float floorY = 0)
        {
            var gate = new GameObject(name).transform;
            gate.SetParent(parent, false);
            Block(gate, "Post_Left", new Vector2(x - 2, 2.5f), new Vector2(0.5f, 5), StructureColor, false, -10);
            Block(gate, "Post_Right", new Vector2(x + 2, 2.5f), new Vector2(0.5f, 5), StructureColor, false, -10);
            Block(gate, "Header", new Vector2(x, 5), new Vector2(4.5f, 0.7f), StructureColor, false, -10);
            Label(gate, "Label", label, new Vector2(x, 5), 0.075f);
            gate.position += Vector3.up * floorY;
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
        static void CreateCamera(GrayboxPlayer2D player, float maxX, bool vertical = false)
        {
            var obj = new GameObject("MainCamera_StraightSideView");
            obj.tag = "MainCamera";
            obj.transform.position = new Vector3(6, 3.25f, -10);
            var camera = obj.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = vertical ? 5.2f : 4.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100;
            obj.AddComponent<AudioListener>();
            var follow = obj.AddComponent<GrayboxCamera2D>();
            follow.target = player.transform;
            follow.worldMaxX = maxX;
            follow.followHeight = vertical;
            if (vertical) follow.lookAhead = 2.8f;
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
        static void Checkpoint(Transform parent, string name, float x, string message, float floorY = 0)
        {
            Trigger(parent, name, new Vector2(x, floorY + 1.5f), new Vector2(1, 3), GrayboxTrigger.Action.Checkpoint, message, new Vector2(x, floorY + 0.76f));
            Block(parent, name + "_Marker", new Vector2(x, floorY + 0.1f), new Vector2(0.55f, 0.2f), new Color(0.65f, 0.72f, 0.70f), false, 1);
        }
        static void MakeFog(GrayboxPlayer2D player)
        {
            var root = new GameObject("07_Fog_RevealAhead");
            var fog = root.AddComponent<GrayboxFog2D>();
            fog.player = player.transform;
            fog.revealCircusLandmark = true;
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
                "Dollwright Dream01 graybox v03\n" +
                "Editor: " + Application.unityVersion + "\n" +
                "Dream: " + DreamScene + "\n" +
                "Move speed 4.2; push speed 2.4; jump speed 11.5; gravity scale 2.8\n" +
                "Full held jump ideal height 2.41; ideal horizontal reach 3.52\n" +
                "Mandatory push x13.2 ->19.4; ledge top3.2 exceeds unaided jump\n" +
                "Flags x27.2..41.4 BEFORE circus; no ground under gaps\n" +
                "Circus rest x43..51.5 -> climb -> box roof top8, H1 at68.5,8.75\n" +
                "Wide descent x73..114; reserved magician alcove x94..103\n" +
                "Wake E at118.5; optional push crate124 ->128.4 for B3 on solid counter131.2,3.95\n" +
                "Fall: local checkpoint + 1s input recovery, inventory kept.\n" +
                "60 seconds is a target, not a measured first-play duration.\n");
        }
    }
}
