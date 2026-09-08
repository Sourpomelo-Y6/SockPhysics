using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SockPhysics.Editor
{
    public static class FootPrototypeSetup
    {
        public const string ScenePath = "Assets/Scenes/FootPrototype.unity";
        private const string ArtPath = "Assets/SockPhysics/Art";

        [MenuItem("Sock Physics/Open Step 1 Scene")]
        public static void OpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Sock Physics/Create Step 1 Scene")]
        public static void CreateScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("The prototype scene already exists. Open it instead of overwriting it.");

            Directory.CreateDirectory(ArtPath);
            var texture = new Texture2D(128, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 128; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, 32f, 96f);
                bool inside = new Vector2(x + 0.5f - cx, y + 0.5f - 32f).sqrMagnitude <= 32f * 32f;
                texture.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
            texture.Apply();
            File.WriteAllBytes(ArtPath + "/Foot.png", texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.Refresh();
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArtPath + "/Foot.png");
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + "/Foot.png");
            var material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            AssetDatabase.CreateAsset(material, ArtPath + "/PrototypeUnlit.mat");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.11f);
            cameraObject.AddComponent<UniversalAdditionalCameraData>();

            var footObject = new GameObject("Foot");
            footObject.transform.position = new Vector3(-3, 0, 0);
            var renderer = footObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = new Color(0.3f, 0.88f, 0.57f);
            var collider = footObject.AddComponent<CapsuleCollider2D>();
            collider.direction = CapsuleDirection2D.Horizontal;
            collider.size = new Vector2(2, 1);
            var body = footObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.mass = 1;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var foot = footObject.AddComponent<FootController>();

            CreateWall("Test Wall", new Vector2(3, 0), new Vector2(0.6f, 5), material);
            CreateWall("Left Boundary", new Vector2(-7, 0), new Vector2(0.4f, 8.4f), material);
            CreateWall("Right Boundary", new Vector2(7, 0), new Vector2(0.4f, 8.4f), material);
            CreateWall("Top Boundary", new Vector2(0, 4), new Vector2(14, 0.4f), material);
            CreateWall("Bottom Boundary", new Vector2(0, -4), new Vector2(14, 0.4f), material);
            new GameObject("Prototype HUD").AddComponent<PrototypeHud>().Configure(foot);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Created " + ScenePath);
        }

        private static void CreateWall(string name, Vector2 position, Vector2 size, Material material)
        {
            var wall = new GameObject(name);
            wall.transform.position = position;
            wall.AddComponent<BoxCollider2D>().size = size;
            // A closed thick outline keeps the collision rectangle visible without font assets.
            var line = wall.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.widthMultiplier = 0.07f;
            line.startColor = line.endColor = new Color(0.3f, 0.65f, 1);
            line.positionCount = 4;
            line.SetPositions(new[] {
                new Vector3(-size.x / 2, -size.y / 2, 0), new Vector3(-size.x / 2, size.y / 2, 0),
                new Vector3(size.x / 2, size.y / 2, 0), new Vector3(size.x / 2, -size.y / 2, 0)
            });
        }

        [MenuItem("Sock Physics/Verify Step 1 Physics")]
        public static void VerifyPhysics()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var previousSetup = EditorSceneManager.GetSceneManagerSetup();
            var previousMode = Physics2D.simulationMode;
            try
            {
                EditorSceneManager.OpenScene(ScenePath);
                Physics2D.simulationMode = SimulationMode2D.Script;
                var foot = UnityEngine.Object.FindObjectOfType<FootController>();
                var body = foot.GetComponent<Rigidbody2D>();
                var start = body.position;
                Physics2D.SyncTransforms();
                Require(!foot.BeginDrag(new Vector2(0, 3)), "Background click must not grab foot");
                Require(foot.BeginDrag(start), "Foot must be draggable");
                foot.SetDragTarget(new Vector2(-1, 1));
                Simulate(foot, 150);
                Require(Vector2.Distance(body.position, new Vector2(-1, 1)) < 0.1f, "Free movement follows pointer");
                foot.SetDragTarget(new Vector2(100, 1));
                Simulate(foot, 500);
                Require(body.position.x > 1.4f && body.position.x < 1.8f, "Long push must stop at wall");
                foot.SetDragTarget(new Vector2(-3, 1));
                Simulate(foot, 150);
                Require(body.position.x < -2.8f, "Foot must pull back from wall");
                foot.SetDragTarget(new Vector2(-3, 100));
                Simulate(foot, 200);
                Require(body.position.y < 3.4f, "Fast drag must stop at boundary");
                foot.EndDrag();
                Simulate(foot, 100);
                Require(body.velocity.magnitude < 0.01f, "Release must settle");
                for (int i = 0; i < 5; i++)
                {
                    foot.ResetPose();
                    Require(Vector2.Distance(body.position, start) < 0.001f && body.velocity == Vector2.zero && !foot.IsDragging,
                        "Reset restores position, velocity and input state");
                    Require(foot.BeginDrag(start), "Dragging works after reset");
                    foot.SetDragTarget(new Vector2(100, 0));
                    Simulate(foot, 150);
                    Require(body.position.x < 1.8f, "Repeated pushes cannot cross wall");
                }
            }
            finally
            {
                Physics2D.simulationMode = previousMode;
                if (previousSetup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                else
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            Debug.Log("STEP1_PHYSICS_VERIFICATION_PASSED");
        }

        private static void Simulate(FootController foot, int steps)
        {
            var body = foot.GetComponent<Rigidbody2D>();
            for (int i = 0; i < steps; i++)
            {
                foot.StepPhysics(0.02f);
                Physics2D.Simulate(0.02f);
                Require(!float.IsNaN(body.position.x) && !float.IsNaN(body.position.y), "Position remains finite");
                Require(body.velocity.magnitude <= foot.MaxSpeed + 0.01f, "Speed stays capped");
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        public static void CreateAndVerify()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            VerifyPhysics();
        }
    }
}
