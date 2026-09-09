using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SockPhysics.Editor
{
    public static class ClosedSockSetup
    {
        public const string ScenePath = "Assets/Scenes/ClosedSock.unity";

        [MenuItem("Sock Physics/Open Step 3 Scene")]
        public static void OpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Sock Physics/Create Step 3 Scene")]
        public static void CreateScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            if (File.Exists(ScenePath)) throw new InvalidOperationException("Step 3 scene already exists; open it instead.");
            var scene = EditorSceneManager.OpenScene(SockSkeletonSetup.ScenePath);
            var sock = UnityEngine.Object.FindObjectOfType<SockController>();
            ShapeRow(sock.UpperPoints, 1);
            ShapeRow(sock.LowerPoints, -1);
            sock.gameObject.name = "Closed Sock";
            sock.gameObject.AddComponent<SockRecovery>();
            sock.RefreshLines();
            UnityEngine.Object.FindObjectOfType<PrototypeHud>().ShowClosedSockInstructions();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        private static void ShapeRow(SockPhysicsPoint[] row, float direction)
        {
            for (int i = 0; i < row.Length; i++)
            {
                float taper = Mathf.Max(0, 1f - i / 6f);
                Vector2 position = new Vector2(-1 + i * 0.3f, direction * (0.2f + 0.6f * taper * taper));
                row[i].transform.position = position;
                row[i].Body.position = position;
                if (i > 0)
                    row[i].GetComponent<DistanceJoint2D>().distance = Vector2.Distance(position, row[i - 1].Body.position);
            }
            Physics2D.SyncTransforms();
        }

        [MenuItem("Sock Physics/Verify Step 3 Physics")]
        public static void VerifyPhysics()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var mode = Physics2D.simulationMode;
            try
            {
                EditorSceneManager.OpenScene(ScenePath);
                Physics2D.simulationMode = SimulationMode2D.Script;
                var sock = UnityEngine.Object.FindObjectOfType<SockController>();
                var foot = UnityEngine.Object.FindObjectOfType<FootController>();
                var recovery = sock.GetComponent<SockRecovery>();
                var body = foot.GetComponent<Rigidbody2D>();
                sock.Initialize();
                Physics2D.SyncTransforms();
                Simulate(foot, sock, recovery, 500);
                CheckClosed(sock);
                for (int pass = 0; pass < 5; pass++)
                {
                    Require(foot.BeginDrag(body.position), "Foot can be grabbed");
                    foot.SetDragTarget(new Vector2(1.5f, pass % 2 == 0 ? 0 : 0.1f));
                    Simulate(foot, sock, recovery, 500);
                    Require(body.position.x > 1, "Foot enters closed sock: " + body.position);
                    Require(sock.UpperPoints[8].Body.position.y - sock.LowerPoints[8].Body.position.y > 1.15f,
                        "Foot spreads the closed rows");
                    foot.SetDragTarget(new Vector2(-3, 0));
                    Simulate(foot, sock, recovery, 500);
                    Require(body.position.x < -2.8f, "Foot pulls out");
                    foot.EndDrag();
                    Simulate(foot, sock, recovery, 500);
                    CheckClosed(sock);
                }
                foot.ResetPose();
                foreach (var p in sock.GetComponentsInChildren<SockPhysicsPoint>())
                    Require(Vector2.Distance(p.Body.position, p.InitialPosition) < 0.001f && p.Body.velocity == Vector2.zero, "Reset restores cloth");
            }
            finally
            {
                Physics2D.simulationMode = mode;
                if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            Debug.Log("STEP3_PHYSICS_VERIFICATION_PASSED");
        }

        internal static void CheckClosed(SockController sock)
        {
            foreach (var p in sock.GetComponentsInChildren<SockPhysicsPoint>())
            {
                Require(Vector2.Distance(p.Body.position, p.InitialPosition) < 0.08f, "Cloth returns to rest: " + p.name);
                Require(p.Body.velocity.magnitude < 0.1f, "Cloth settles");
            }
        }

        internal static void Simulate(FootController foot, SockController sock, SockRecovery recovery, int count)
        {
            var collider = foot.GetComponent<Collider2D>();
            for (int step = 0; step < count; step++)
            {
                foot.StepPhysics(0.02f);
                recovery.StepPhysics();
                Physics2D.Simulate(0.02f);
                CheckRow(sock.UpperPoints, collider, 1);
                CheckRow(sock.LowerPoints, collider, -1);
                for (int i = 0; i < sock.UpperPoints.Length; i++)
                    Require(sock.UpperPoints[i].Body.position.y > sock.LowerPoints[i].Body.position.y, "Rows do not invert");
            }
        }

        private static void CheckRow(SockPhysicsPoint[] row, Collider2D foot, int direction)
        {
            for (int i = 0; i < row.Length; i++)
            {
                Vector2 p = row[i].Body.position;
                Require(!float.IsNaN(p.x) && !float.IsInfinity(p.x) && !float.IsNaN(p.y) && !float.IsInfinity(p.y), "Finite position");
                Require(row[i].Body.velocity.magnitude < 20, "No explosive velocity");
                if (i > 0)
                {
                    Vector2 a = row[i - 1].Body.position;
                    Require(Vector2.Distance(p, a) < 0.38f, "Neighbor coverage maintained");
                    Vector2 center = foot.attachedRigidbody.position;
                    if (Mathf.Abs(p.x - a.x) > 0.0001f && center.x >= Mathf.Min(p.x, a.x) && center.x <= Mathf.Max(p.x, a.x))
                    {
                        float y = Mathf.Lerp(a.y, p.y, (center.x - a.x) / (p.x - a.x));
                        Require(direction * (center.y - y) < 0.05f, "Foot stays inside the cloth");
                    }
                }
                var separation = row[i].GetComponent<Collider2D>().Distance(foot);
                Require(!separation.isOverlapped || separation.distance > -0.06f, "No deep foot penetration");
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
            SockSkeletonSetup.VerifyPhysics();
            FootPrototypeSetup.VerifyPhysics();
        }

        public static void CapturePreview()
        {
            var mode = Physics2D.simulationMode;
            try
            {
                EditorSceneManager.OpenScene(ScenePath);
                Physics2D.simulationMode = SimulationMode2D.Script;
                var sock = UnityEngine.Object.FindObjectOfType<SockController>();
                var foot = UnityEngine.Object.FindObjectOfType<FootController>();
                sock.Initialize();
                Physics2D.SyncTransforms();
                SockSkeletonSetup.CaptureCurrentPreview("Logs/Step3Closed.png");
                foot.BeginDrag(foot.GetComponent<Rigidbody2D>().position);
                foot.SetDragTarget(new Vector2(1.5f, 0));
                Simulate(foot, sock, sock.GetComponent<SockRecovery>(), 500);
                sock.RefreshLines();
                SockSkeletonSetup.CaptureCurrentPreview("Logs/Step3Open.png");
            }
            finally { Physics2D.simulationMode = mode; }
        }
    }
}
