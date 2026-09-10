using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SockPhysics.Editor
{
    public static class BentStageSetup
    {
        public const string ScenePath = "Assets/Scenes/BentStage.unity";
        [MenuItem("Sock Physics/Open Step 7 Scene")]
        public static void OpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }
        public static void CreateAndVerify()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            VerifyPhysics();
            StraightStageSetup.VerifyPhysics();
            AnklePrototypeSetup.VerifyPhysics();
            PrototypeTuningSetup.VerifyPhysics();
            ClosedSockSetup.VerifyPhysics();
            SockSkeletonSetup.VerifyPhysics();
            FootPrototypeSetup.VerifyPhysics();
        }
        private static void CreateScene()
        {
            var scene = EditorSceneManager.OpenScene(StraightStageSetup.ScenePath);
            UnityEngine.Object.DestroyImmediate(UnityEngine.Object.FindObjectOfType<SockController>().gameObject);
            UnityEngine.Object.DestroyImmediate(UnityEngine.Object.FindObjectOfType<SockToeClosure>().gameObject);
            UnityEngine.Object.DestroyImmediate(UnityEngine.Object.FindObjectOfType<FitEvaluator>().gameObject);
            var leg = UnityEngine.Object.FindObjectOfType<FootController>();
            var ankle = UnityEngine.Object.FindObjectOfType<AnkleController>();
            leg.transform.position = new Vector3(-4, 0, 0);
            leg.GetComponent<Rigidbody2D>().position = new Vector2(-4, 0);
            ankle.transform.rotation = Quaternion.identity;
            ankle.transform.position = new Vector3(-2.6f, 0, 0);
            ankle.GetComponent<Rigidbody2D>().position = new Vector2(-2.6f, 0);
            ankle.GetComponent<Rigidbody2D>().rotation = 0;
            ankle.ConfigureBend();
            var hinge = ankle.GetComponent<HingeJoint2D>();
            hinge.limits = new JointAngleLimits2D { min = -20, max = 100 };
            var root = new GameObject("Bent Sock");
            var outer = CreateRow(root.transform, "Outer", 1.65f, new Color(0.3f, 0.7f, 1), out var outerLine);
            var inner = CreateRow(root.transform, "Inner", 0.35f, new Color(1, 0.72f, 0.25f), out var innerLine);
            var sock = root.AddComponent<SockController>();
            sock.Configure(leg, outer, inner, outerLine, innerLine);
            root.AddComponent<SockRecovery>();
            var cap = new GameObject("Bent Toe");
            var line = MakeLine(cap, new Color(0.65f, 0.65f, 1));
            var nodes = new SockPhysicsPoint[4];
            var previous = outer[outer.Length - 1].Body;
            for (int i = 0; i < 4; i++)
            {
                nodes[i] = MakePoint(cap.transform, "Toe " + i, new Vector2(1.89f - i * 0.26f, i == 0 || i == 3 ? -2.35f : -2.4f));
                Connect(nodes[i].Body, previous);
                Hold(nodes[i].Body, 18);
                previous = nodes[i].Body;
            }
            Connect(previous, inner[inner.Length - 1].Body);
            var closure = cap.AddComponent<SockToeClosure>();
            closure.Configure(leg, outer[outer.Length - 1], inner[inner.Length - 1], nodes, line);
            closure.RefreshLine();
            var fit = new GameObject("Bend Fit").AddComponent<FitEvaluator>();
            fit.Configure(leg, ankle, sock);
            fit.ConfigureTargets(new Vector2(1.5f, -1.95f), new Vector2(1.3f, -0.45f), new Vector2(1.5f, -0.5f), new Vector2(0.75f, -0.5f), -90);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }
        private static SockPhysicsPoint[] CreateRow(Transform root, string name, float radius, Color color, out LineRenderer line)
        {
            var row = new GameObject(name); row.transform.SetParent(root);
            line = MakeLine(row, color);
            var positions = new List<Vector2>();
            for (int i = 0; i <= 8; i++) positions.Add(new Vector2(-1.5f + i * 0.25f, -1.25f + radius));
            int arcSteps = Mathf.CeilToInt(radius * Mathf.PI / 2 / 0.28f);
            for (int i = 1; i <= arcSteps; i++)
            {
                float a = Mathf.PI / 2 * (1f - (float)i / arcSteps);
                positions.Add(new Vector2(0.5f + radius * Mathf.Cos(a), -1.25f + radius * Mathf.Sin(a)));
            }
            for (int i = 1; i <= 4; i++) positions.Add(new Vector2(0.5f + radius, -1.25f - i * 0.95f / 4));
            var points = new SockPhysicsPoint[positions.Count];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = MakePoint(row.transform, name + " " + i, positions[i]);
                if (i > 0) Connect(points[i].Body, points[i - 1].Body);
                if (i == 0 || i >= 8) Hold(points[i].Body, 12);
            }
            return points;
        }
        private static LineRenderer MakeLine(GameObject obj, Color color)
        {
            var line = obj.AddComponent<LineRenderer>();
            line.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SockPhysics/Art/PrototypeUnlit.mat");
            line.widthMultiplier = 0.36f; line.numCapVertices = line.numCornerVertices = 6;
            line.startColor = line.endColor = color; return line;
        }
        private static SockPhysicsPoint MakePoint(Transform parent, string name, Vector2 position)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent); obj.transform.position = position;
            var body = obj.AddComponent<Rigidbody2D>(); body.mass = 0.25f; body.gravityScale = 0; body.drag = 5;
            body.constraints = RigidbodyConstraints2D.FreezeRotation; body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            obj.AddComponent<CircleCollider2D>().radius = 0.18f;
            return obj.AddComponent<SockPhysicsPoint>();
        }
        private static void Connect(Rigidbody2D body, Rigidbody2D previous)
        {
            var joint = body.gameObject.AddComponent<DistanceJoint2D>(); joint.connectedBody = previous;
            joint.autoConfigureConnectedAnchor = joint.autoConfigureDistance = false;
            joint.distance = Vector2.Distance(body.position, previous.position); joint.enableCollision = false;
        }
        private static void Hold(Rigidbody2D body, float frequency)
        {
            var joint = body.gameObject.AddComponent<SpringJoint2D>(); joint.autoConfigureConnectedAnchor = joint.autoConfigureDistance = false;
            joint.connectedAnchor = body.position; joint.distance = 0.001f; joint.frequency = frequency; joint.dampingRatio = 1;
        }
        [MenuItem("Sock Physics/Verify Step 7 Physics")]
        public static void VerifyPhysics()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var mode = Physics2D.simulationMode; int positions = Physics2D.positionIterations, velocities = Physics2D.velocityIterations;
            try
            {
                EditorSceneManager.OpenScene(ScenePath); Physics2D.simulationMode = SimulationMode2D.Script;
                UnityEngine.Object.FindObjectOfType<StagePhysicsSettings>().Apply();
                var leg = UnityEngine.Object.FindObjectOfType<FootController>();
                var ankle = UnityEngine.Object.FindObjectOfType<AnkleController>();
                var sock = UnityEngine.Object.FindObjectOfType<SockController>();
                var cap = UnityEngine.Object.FindObjectOfType<SockToeClosure>();
                var fit = UnityEngine.Object.FindObjectOfType<FitEvaluator>();
                sock.Initialize(); ankle.Initialize(); cap.Initialize(); fit.Initialize(); Physics2D.SyncTransforms();
                var body = leg.GetComponent<Rigidbody2D>();
                for (int pass = 0; pass < 2; pass++)
                {
                    leg.BeginDrag(body.position); leg.SetDragTarget(new Vector2(3, 0));
                    Simulate(leg, ankle, sock, fit, 600);
                    Debug.Log("STEP7_BLOCKED " + body.position + " angle=" + ankle.GetComponent<Rigidbody2D>().rotation);
                    Require(body.position.x < 0.5f && !fit.Cleared, "Straight push is blocked by bend");
                    Require(Mathf.Abs(ankle.GetComponent<Rigidbody2D>().rotation) < 5, "Pushing alone must not fold the ankle");
                    leg.SetDragTarget(new Vector2(-0.7f, 0)); Simulate(leg, ankle, sock, fit, 300);
                    ankle.SetTargetAngle(-90); Simulate(leg, ankle, sock, fit, 300);
                    leg.SetDragTarget(new Vector2(0.75f, -0.5f)); Simulate(leg, ankle, sock, fit, 700);
                    Debug.Log("STEP7_FIT " + fit.FitScore + " leg=" + body.position + " angle=" + ankle.GetComponent<Rigidbody2D>().rotation);
                    float stretch = 0;
                    foreach (var row in new[] { sock.UpperPoints, sock.LowerPoints })
                        for (int i = 1; i < row.Length; i++) stretch = Mathf.Max(stretch, Vector2.Distance(row[i].Body.position, row[i - 1].Body.position) / row[i].GetComponent<DistanceJoint2D>().distance);
                    Debug.Log("STEP7_STATUS hold=" + fit.HoldTime + " stretch=" + stretch + " speed=" + ankle.GetComponent<Rigidbody2D>().velocity + " angular=" + ankle.GetComponent<Rigidbody2D>().angularVelocity);
                    Require(fit.Cleared, "Turned foot clears bent stage");
                    Require(leg.InputLocked && ankle.InputLocked && !leg.IsDragging, "Clear stops player input");
                    leg.ResetPose(); Require(!fit.Cleared && ankle.TargetAngle == 0 && !leg.InputLocked, "Retry resets bend stage");
                }
            }
            finally
            {
                Physics2D.simulationMode = mode;
                if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup); else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Physics2D.positionIterations = positions; Physics2D.velocityIterations = velocities;
            }
            Debug.Log("STEP7_PHYSICS_VERIFICATION_PASSED");
        }
        internal static void Simulate(FootController leg, AnkleController ankle, SockController sock, FitEvaluator fit, int count)
        {
            var points = UnityEngine.Object.FindObjectsOfType<SockPhysicsPoint>();
            var parts = new[] { leg.GetComponent<Collider2D>(), ankle.GetComponent<Collider2D>() };
            for (int i = 0; i < count; i++)
            {
                leg.StepPhysics(0.02f); ankle.StepPhysics(); sock.GetComponent<SockRecovery>().StepPhysics(); Physics2D.Simulate(0.02f);
                foreach (var point in points)
                {
                    Require(point.Body.position.sqrMagnitude < 1000 && point.Body.velocity.magnitude < 20, "Stable cloth");
                    foreach (var joint in point.GetComponents<DistanceJoint2D>())
                        Require(Vector2.Distance(point.Body.position, joint.connectedBody.position) < 0.38f, "Continuous cloth coverage");
                    foreach (var part in parts) Require(point.GetComponent<Collider2D>().Distance(part).distance > -0.08f, "No deep penetration");
                }
                fit.Evaluate(0.02f);
            }
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        public static void VerifyAndCapture()
        {
            CreateAndVerify();
            var mode = Physics2D.simulationMode;
            int positions = Physics2D.positionIterations, velocities = Physics2D.velocityIterations;
            try
            {
                EditorSceneManager.OpenScene(ScenePath); Physics2D.simulationMode = SimulationMode2D.Script;
                UnityEngine.Object.FindObjectOfType<StagePhysicsSettings>().Apply();
                var leg = UnityEngine.Object.FindObjectOfType<FootController>();
                var ankle = UnityEngine.Object.FindObjectOfType<AnkleController>();
                var sock = UnityEngine.Object.FindObjectOfType<SockController>();
                var cap = UnityEngine.Object.FindObjectOfType<SockToeClosure>();
                var fit = UnityEngine.Object.FindObjectOfType<FitEvaluator>();
                sock.Initialize(); ankle.Initialize(); cap.Initialize(); fit.Initialize(); Physics2D.SyncTransforms();
                SockSkeletonSetup.CaptureCurrentPreview("Logs/Step7Initial.png");
                leg.BeginDrag(leg.GetComponent<Rigidbody2D>().position);
                leg.SetDragTarget(new Vector2(3, 0)); Simulate(leg, ankle, sock, fit, 600);
                sock.RefreshLines(); cap.RefreshLine();
                SockSkeletonSetup.CaptureCurrentPreview("Logs/Step7Blocked.png");
                leg.SetDragTarget(new Vector2(-0.7f, 0)); Simulate(leg, ankle, sock, fit, 300);
                ankle.SetTargetAngle(-90); Simulate(leg, ankle, sock, fit, 300);
                leg.SetDragTarget(new Vector2(0.75f, -0.5f)); Simulate(leg, ankle, sock, fit, 700);
                Require(fit.Cleared, "Preview reaches clear");
                sock.RefreshLines(); cap.RefreshLine();
                SockSkeletonSetup.CaptureCurrentPreview("Logs/Step7Clear.png");
            }
            finally
            {
                Physics2D.simulationMode = mode;
                Physics2D.positionIterations = positions; Physics2D.velocityIterations = velocities;
            }
        }
    }
}
