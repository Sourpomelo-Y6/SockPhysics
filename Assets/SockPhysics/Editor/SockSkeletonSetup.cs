using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SockPhysics.Editor
{
    public static class SockSkeletonSetup
    {
        public const string ScenePath = "Assets/Scenes/SockSkeleton.unity";
        private const int PointCount = 19;
        private const float Spacing = 0.3f;
        private const float Radius = 0.18f;

        [MenuItem("Sock Physics/Open Step 2 Scene")]
        public static void OpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Sock Physics/Create Step 2 Scene")]
        public static void CreateScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Step 2 scene already exists; open it instead.");
            var scene = EditorSceneManager.OpenScene(FootPrototypeSetup.ScenePath);
            UnityEngine.Object.DestroyImmediate(GameObject.Find("Test Wall"));
            var foot = UnityEngine.Object.FindObjectOfType<FootController>();
            UnityEngine.Object.FindObjectOfType<PrototypeHud>().ShowSockInstructions();
            var root = new GameObject("Sock Skeleton");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/SockPhysics/Art/PrototypeUnlit.mat");
            var upper = CreateRow(root.transform, "Upper", 0.8f, new Color(0.3f, 0.7f, 1f), material, out var upperLine);
            var lower = CreateRow(root.transform, "Lower", -0.8f, new Color(1f, 0.72f, 0.25f), material, out var lowerLine);
            root.AddComponent<SockController>().Configure(foot, upper, lower, upperLine, lowerLine);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Created " + ScenePath);
        }

        private static SockPhysicsPoint[] CreateRow(Transform root, string name, float y, Color color,
            Material material, out LineRenderer line)
        {
            var row = new GameObject(name);
            row.transform.SetParent(root);
            line = row.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = true;
            line.widthMultiplier = Radius * 2;
            line.startColor = line.endColor = color;
            line.numCapVertices = 6;
            line.numCornerVertices = 6;
            var points = new SockPhysicsPoint[PointCount];
            for (int i = 0; i < points.Length; i++)
            {
                var point = new GameObject(name + " Point " + i.ToString("00"));
                point.transform.SetParent(row.transform);
                point.transform.position = new Vector3(-1f + i * Spacing, y, 0);
                var body = point.AddComponent<Rigidbody2D>();
                body.mass = 0.25f;
                body.gravityScale = 0;
                body.drag = 5;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                point.AddComponent<CircleCollider2D>().radius = Radius;
                points[i] = point.AddComponent<SockPhysicsPoint>();
                if (i > 0)
                {
                    var joint = point.AddComponent<DistanceJoint2D>();
                    joint.connectedBody = points[i - 1].Body;
                    joint.autoConfigureConnectedAnchor = false;
                    joint.anchor = joint.connectedAnchor = Vector2.zero;
                    joint.autoConfigureDistance = false;
                    joint.distance = Spacing;
                    joint.enableCollision = false;
                }
                else
                {
                    var hold = point.AddComponent<SpringJoint2D>();
                    hold.autoConfigureConnectedAnchor = false;
                    hold.connectedAnchor = body.position;
                    hold.autoConfigureDistance = false;
                    hold.distance = 0.001f;
                    hold.frequency = 6;
                    hold.dampingRatio = 1;
                }
            }
            return points;
        }

        [MenuItem("Sock Physics/Verify Step 2 Physics")]
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
                var sock = UnityEngine.Object.FindObjectOfType<SockController>();
                var foot = UnityEngine.Object.FindObjectOfType<FootController>();
                var body = foot.GetComponent<Rigidbody2D>();
                sock.Initialize();
                Physics2D.SyncTransforms();
                Simulate(foot, sock, 500);
                foreach (var point in sock.GetComponentsInChildren<SockPhysicsPoint>())
                    Require(Vector2.Distance(point.Body.position, point.InitialPosition) < 0.03f, "Resting rows remain stable");

                for (int attempt = 0; attempt < 3; attempt++)
                {
                    foot.ResetPose();
                    Require(foot.BeginDrag(body.position), "Foot can be grabbed after reset");
                    foot.SetDragTarget(new Vector2(1, 0));
                    Simulate(foot, sock, 180);
                    Require(Vector2.Distance(body.position, new Vector2(1, 0)) < 0.15f, "Foot enters the open rows");
                    int direction = attempt == 1 ? -1 : 1;
                    var row = direction > 0 ? sock.UpperPoints : sock.LowerPoints;
                    foot.SetDragTarget(new Vector2(1, direction * 100));
                    Simulate(foot, sock, 200, row, direction);
                    float displacement = 0;
                    foreach (var point in row)
                        displacement = Mathf.Max(displacement, Vector2.Distance(point.Body.position, point.InitialPosition));
                    Require(displacement > 0.2f, "Contact deforms cloth");
                    foot.SetDragTarget(new Vector2(-3, 0));
                    Simulate(foot, sock, 300);
                    Require(body.position.x < -2.6f, "Foot can be pulled out");
                    foot.EndDrag();
                    Simulate(foot, sock, 600);
                    foreach (var point in sock.GetComponentsInChildren<SockPhysicsPoint>())
                        Require(point.Body.velocity.magnitude < 0.15f, "Cloth settles after release");
                }
                foot.ResetPose();
                foreach (var point in sock.GetComponentsInChildren<SockPhysicsPoint>())
                    Require(Vector2.Distance(point.Body.position, point.InitialPosition) < 0.001f && point.Body.velocity == Vector2.zero,
                        "Foot reset also restores all sock points");
                sock.RefreshLines();
                Require(sock.GetComponentsInChildren<LineRenderer>().Length == 2, "Both rows have a visible line");
            }
            finally
            {
                Physics2D.simulationMode = previousMode;
                if (previousSetup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            Debug.Log("STEP2_PHYSICS_VERIFICATION_PASSED");
        }

        private static void Simulate(FootController foot, SockController sock, int steps,
            SockPhysicsPoint[] barrier = null, int direction = 1)
        {
            var body = foot.GetComponent<Rigidbody2D>();
            for (int step = 0; step < steps; step++)
            {
                foot.StepPhysics(0.02f);
                Physics2D.Simulate(0.02f);
                CheckRow(sock.UpperPoints);
                CheckRow(sock.LowerPoints);
                if (barrier == null) continue;
                bool intersects = false;
                for (int i = 1; i < barrier.Length; i++)
                {
                    Vector2 a = barrier[i - 1].Body.position;
                    Vector2 b = barrier[i].Body.position;
                    if (Mathf.Abs(b.x - a.x) < 0.0001f || body.position.x < Mathf.Min(a.x, b.x) || body.position.x > Mathf.Max(a.x, b.x)) continue;
                    float y = Mathf.Lerp(a.y, b.y, (body.position.x - a.x) / (b.x - a.x));
                    Require(direction * (body.position.y - y) < 0.05f, "Foot must not cross between cloth points");
                    intersects = true;
                }
                Require(intersects, "Push remains under the cloth rather than passing around its open end");
            }
        }

        private static void CheckRow(SockPhysicsPoint[] row)
        {
            for (int i = 0; i < row.Length; i++)
            {
                Vector2 p = row[i].Body.position;
                Require(!float.IsNaN(p.x) && !float.IsInfinity(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.y), "Points remain finite");
                Require(row[i].Body.velocity.magnitude < 20, "No explosive velocity");
                if (i > 0)
                    Require(Vector2.Distance(p, row[i - 1].Body.position) < Radius * 2 + 0.02f, "Neighbor colliders maintain coverage");
            }
            Require(Vector2.Distance(row[0].Body.position, row[0].InitialPosition) < 0.5f, "Entrance remains held");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        public static void CreateAndVerify()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            VerifyPhysics();
            FootPrototypeSetup.VerifyPhysics();
        }

        // Run in batch mode without -nographics to inspect the saved scene's rendering.
        public static void CapturePreview()
        {
            EditorSceneManager.OpenScene(ScenePath);
            CaptureCurrentPreview("Logs/Step2Preview.png");
        }

        public static void CaptureCurrentPreview(string outputPath)
        {
            var camera = Camera.main;
            var target = new RenderTexture(1280, 720, 24);
            var previousTarget = RenderTexture.active;
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("Logs");
                File.WriteAllBytes(outputPath, pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previousTarget;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}
