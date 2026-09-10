using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SockPhysics.Editor
{
    public static class StraightStageSetup
    {
        public const string ScenePath = "Assets/Scenes/StraightStage.unity";
        [MenuItem("Sock Physics/Open Step 6 Scene")]
        public static void OpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }
        public static void CreateAndVerify()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            else
            {
                var scene = EditorSceneManager.OpenScene(ScenePath);
                if (UnityEngine.Object.FindObjectOfType<StagePhysicsSettings>() == null)
                {
                    new GameObject("Stage Physics Settings").AddComponent<StagePhysicsSettings>();
                    EditorSceneManager.SaveScene(scene);
                }
            }
            VerifyPhysics();
            AnklePrototypeSetup.VerifyPhysics();
            PrototypeTuningSetup.VerifyPhysics();
            ClosedSockSetup.VerifyPhysics();
            SockSkeletonSetup.VerifyPhysics();
            FootPrototypeSetup.VerifyPhysics();
        }
        private static void CreateScene()
        {
            var scene = EditorSceneManager.OpenScene(AnklePrototypeSetup.ScenePath);
            new GameObject("Stage Physics Settings").AddComponent<StagePhysicsSettings>();
            var leg = UnityEngine.Object.FindObjectOfType<FootController>();
            var ankle = UnityEngine.Object.FindObjectOfType<AnkleController>();
            var sock = UnityEngine.Object.FindObjectOfType<SockController>();
            ankle.ShowFitInstructions();
            var lower = sock.LowerPoints;
            for (int i = 10; i <= 15; i++)
            {
                float bulge = 0.2f * Mathf.Sin((i - 10) * Mathf.PI / 5);
                Vector2 position = new Vector2(lower[i].Body.position.x, -0.65f - bulge);
                lower[i].transform.position = position;
                lower[i].Body.position = position;
            }
            for (int i = 1; i < lower.Length; i++)
                lower[i].GetComponent<DistanceJoint2D>().distance = Vector2.Distance(lower[i].Body.position, lower[i - 1].Body.position);
            var cap = new GameObject("Closed Toe");
            var line = cap.AddComponent<LineRenderer>();
            line.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SockPhysics/Art/PrototypeUnlit.mat");
            line.widthMultiplier = 0.36f;
            line.startColor = line.endColor = new Color(0.65f, 0.65f, 1);
            line.numCornerVertices = line.numCapVertices = 6;
            var points = new SockPhysicsPoint[4];
            Rigidbody2D previous = sock.UpperPoints[18].Body;
            for (int i = 0; i < points.Length; i++)
            {
                var node = new GameObject("Toe Point " + i);
                node.transform.SetParent(cap.transform);
                node.transform.position = new Vector3(i == 0 || i == 3 ? 4.55f : 4.6f, 0.39f - i * 0.26f, 0);
                var body = node.AddComponent<Rigidbody2D>();
                body.mass = 0.25f; body.gravityScale = 0; body.drag = 5;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                node.AddComponent<CircleCollider2D>().radius = 0.18f;
                points[i] = node.AddComponent<SockPhysicsPoint>();
                Connect(body, previous);
                var hold = node.AddComponent<SpringJoint2D>();
                hold.autoConfigureConnectedAnchor = false;
                hold.connectedAnchor = body.position;
                hold.autoConfigureDistance = false;
                hold.distance = 0.001f; hold.frequency = 18; hold.dampingRatio = 1;
                previous = body;
            }
            Connect(previous, lower[18].Body);
            cap.AddComponent<SockToeClosure>().Configure(leg, sock.UpperPoints[18], lower[18], points, line);
            cap.GetComponent<SockToeClosure>().RefreshLine();
            new GameObject("Fit and Clear").AddComponent<FitEvaluator>().Configure(leg, ankle, sock);
            sock.RefreshLines();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }
        private static void Connect(Rigidbody2D body, Rigidbody2D previous)
        {
            var joint = body.gameObject.AddComponent<DistanceJoint2D>();
            joint.connectedBody = previous;
            joint.autoConfigureConnectedAnchor = false;
            joint.autoConfigureDistance = false;
            joint.distance = Vector2.Distance(body.position, previous.position);
            joint.enableCollision = false;
        }
        [MenuItem("Sock Physics/Verify Step 6 Physics")]
        public static void VerifyPhysics()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var mode = Physics2D.simulationMode;
            int oldPositionIterations = Physics2D.positionIterations;
            int oldVelocityIterations = Physics2D.velocityIterations;
            try
            {
                EditorSceneManager.OpenScene(ScenePath);
                Physics2D.simulationMode = SimulationMode2D.Script;
                UnityEngine.Object.FindObjectOfType<StagePhysicsSettings>().Apply();
                var leg = UnityEngine.Object.FindObjectOfType<FootController>();
                var ankle = UnityEngine.Object.FindObjectOfType<AnkleController>();
                var sock = UnityEngine.Object.FindObjectOfType<SockController>();
                var cap = UnityEngine.Object.FindObjectOfType<SockToeClosure>();
                var fit = UnityEngine.Object.FindObjectOfType<FitEvaluator>();
                sock.Initialize(); ankle.Initialize(); cap.Initialize(); fit.Initialize();
                Physics2D.SyncTransforms();
                var body = leg.GetComponent<Rigidbody2D>();
                var footBody = ankle.GetComponent<Rigidbody2D>();
                // A toe at the target with a misaligned heel/ankle must never clear.
                footBody.rotation = 30;
                footBody.position = new Vector2(4.2f, 0) - (Vector2)(Quaternion.Euler(0, 0, 30) * new Vector3(0.8f, 0));
                Physics2D.SyncTransforms();
                for (int i = 0; i < 100; i++) fit.Evaluate(0.02f);
                Require(fit.ToeFits && !fit.Cleared && !fit.HeelFits, "Toe-only pose cannot clear");
                leg.ResetPose();
                body.position = new Vector2(2, 0);
                footBody.position = new Vector2(3.4f, 0);
                footBody.rotation = 0;
                Physics2D.SyncTransforms();
                fit.Evaluate(0.3f);
                Require(fit.HoldTime > 0 && !fit.Cleared, "Brief correct pose does not clear immediately");
                footBody.position += Vector2.up;
                fit.Evaluate(0.02f);
                Require(fit.HoldTime == 0 && !fit.Cleared, "Leaving fit resets dwell");
                footBody.position = new Vector2(3.4f, 0);
                fit.Evaluate(0.3f);
                Require(!fit.Cleared, "Discontinuous valid poses do not accumulate dwell");
                leg.ResetPose();
                for (int pass = 0; pass < 2; pass++)
                {
                    Debug.Log("STEP6_START pass=" + pass);
                    Require(leg.BeginDrag(body.position), "Can start/retry");
                    leg.SetDragTarget(new Vector2(3, 0.5f));
                    Simulate(leg, ankle, sock, cap, fit, 600);
                    Require(body.position.x < 0.5f && !fit.Cleared, "Bent foot stops without clearing");
                    leg.SetDragTarget(new Vector2(-2.5f, 0));
                    Debug.Log("STEP6_PULLBACK");
                    Simulate(leg, ankle, sock, cap, fit, 300);
                    ankle.SetTargetAngle(0);
                    Simulate(leg, ankle, sock, cap, fit, 200);
                    leg.SetDragTarget(new Vector2(2, 0));
                    Debug.Log("STEP6_INSERT");
                    Simulate(leg, ankle, sock, cap, fit, 650);
                    Debug.Log("STEP6_FIT " + fit.FitScore + " cleared=" + fit.Cleared + " leg=" + body.position);
                    Require(fit.Cleared && fit.HoldTime >= 0.75f, "Correct fit clears after dwell");
                    Require(leg.InputLocked && ankle.InputLocked && !leg.BeginDrag(body.position), "Clear locks dragging");
                    leg.ResetPose();
                    Require(!fit.Cleared && fit.HoldTime == 0 && !leg.InputLocked && !ankle.InputLocked, "Retry clears state");
                }
                // Press far beyond the toe without the clear latch to test the physical closure.
                fit.enabled = false;
                leg.BeginDrag(body.position); ankle.SetTargetAngle(0);
                Simulate(leg, ankle, sock, cap, null, 200);
                leg.SetDragTarget(new Vector2(100, 0));
                Simulate(leg, ankle, sock, cap, null, 700);
                Require(footBody.position.x < 4, "Closed toe blocks pushing through");
                leg.ResetPose();
            }
            finally
            {
                Physics2D.simulationMode = mode;
                if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Physics2D.positionIterations = oldPositionIterations;
                Physics2D.velocityIterations = oldVelocityIterations;
            }
            Debug.Log("STEP6_PHYSICS_VERIFICATION_PASSED");
        }
        private static void Simulate(FootController leg, AnkleController ankle, SockController sock, SockToeClosure cap, FitEvaluator fit, int count)
        {
            for (int i = 0; i < count; i++)
            {
                AnklePrototypeSetup.Simulate(leg, ankle, sock, 1);
                foreach (var point in cap.GetComponentsInChildren<SockPhysicsPoint>())
                {
                    Require(point.Body.position.sqrMagnitude < 1000 && point.Body.velocity.magnitude < 20, "Toe remains stable");
                    foreach (var joint in point.GetComponents<DistanceJoint2D>())
                        Require(Vector2.Distance(point.Body.position, joint.connectedBody.position) < 0.38f, "Toe coverage maintained");
                    Require(point.GetComponent<Collider2D>().Distance(ankle.GetComponent<Collider2D>()).distance > -0.08f, "No deep toe penetration");
                }
                if (fit != null) fit.Evaluate(0.02f);
            }
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        public static void CapturePreview()
        {
            var mode = Physics2D.simulationMode;
            int positions = Physics2D.positionIterations;
            int velocities = Physics2D.velocityIterations;
            try
            {
                EditorSceneManager.OpenScene(ScenePath);
                Physics2D.simulationMode = SimulationMode2D.Script;
                UnityEngine.Object.FindObjectOfType<StagePhysicsSettings>().Apply();
                var leg = UnityEngine.Object.FindObjectOfType<FootController>();
                var ankle = UnityEngine.Object.FindObjectOfType<AnkleController>();
                var sock = UnityEngine.Object.FindObjectOfType<SockController>();
                var cap = UnityEngine.Object.FindObjectOfType<SockToeClosure>();
                var fit = UnityEngine.Object.FindObjectOfType<FitEvaluator>();
                sock.Initialize(); ankle.Initialize(); cap.Initialize(); fit.Initialize(); Physics2D.SyncTransforms();
                SockSkeletonSetup.CaptureCurrentPreview("Logs/Step6Initial.png");
                leg.BeginDrag(leg.GetComponent<Rigidbody2D>().position);
                leg.SetDragTarget(new Vector2(3, 0.5f)); Simulate(leg, ankle, sock, cap, fit, 600);
                leg.SetDragTarget(new Vector2(-2.5f, 0)); Simulate(leg, ankle, sock, cap, fit, 300);
                ankle.SetTargetAngle(0); Simulate(leg, ankle, sock, cap, fit, 200);
                leg.SetDragTarget(new Vector2(2, 0)); Simulate(leg, ankle, sock, cap, fit, 650);
                Require(fit.Cleared, "Preview reaches clear state");
                sock.RefreshLines(); cap.RefreshLine();
                SockSkeletonSetup.CaptureCurrentPreview("Logs/Step6Clear.png");
            }
            finally
            {
                Physics2D.simulationMode = mode;
                Physics2D.positionIterations = positions;
                Physics2D.velocityIterations = velocities;
            }
        }
    }
}
