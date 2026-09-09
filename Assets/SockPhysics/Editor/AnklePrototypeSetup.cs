using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SockPhysics.Editor
{
    public static class AnklePrototypeSetup
    {
        public const string ScenePath = "Assets/Scenes/AnklePrototype.unity";
        [MenuItem("Sock Physics/Open Step 5 Scene")]
        public static void OpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }
        public static void CreateAndVerify()
        {
            if (!File.Exists(ScenePath)) CreateScene();
            VerifyPhysics();
            PrototypeTuningSetup.VerifyPhysics();
            ClosedSockSetup.VerifyPhysics();
            SockSkeletonSetup.VerifyPhysics();
            FootPrototypeSetup.VerifyPhysics();
        }
        private static void CreateScene()
        {
            var scene = EditorSceneManager.OpenScene(ClosedSockSetup.ScenePath);
            UnityEngine.Object.DestroyImmediate(UnityEngine.Object.FindObjectOfType<PrototypeHud>());
            var leg = UnityEngine.Object.FindObjectOfType<FootController>();
            leg.name = "Leg (drag here)";
            leg.transform.position = new Vector3(-4, 0.5f, 0);
            leg.GetComponent<Rigidbody2D>().position = new Vector2(-4, 0.5f);
            leg.GetComponent<CapsuleCollider2D>().size = new Vector2(1.5f, 0.4f);
            var legRenderer = leg.GetComponent<SpriteRenderer>();
            legRenderer.drawMode = SpriteDrawMode.Sliced;
            legRenderer.size = new Vector2(1.5f, 0.4f);
            legRenderer.color = new Color(0.65f, 1, 0.65f);
            var foot = new GameObject("Foot (Q E ankle)");
            var renderer = foot.AddComponent<SpriteRenderer>();
            renderer.sprite = legRenderer.sprite;
            renderer.sharedMaterial = legRenderer.sharedMaterial;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(1.6f, 0.5f);
            renderer.color = new Color(0.2f, 0.8f, 0.4f);
            foot.transform.rotation = Quaternion.Euler(0, 0, -50);
            foot.transform.position = new Vector3(-3.25f, 0.5f, 0) + foot.transform.right * 0.65f;
            var body = foot.AddComponent<Rigidbody2D>();
            body.gravityScale = 0;
            body.mass = 0.7f;
            body.drag = 2;
            body.angularDrag = 3;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = foot.AddComponent<CapsuleCollider2D>();
            collider.direction = CapsuleDirection2D.Horizontal;
            collider.size = new Vector2(1.6f, 0.5f);
            var hinge = foot.AddComponent<HingeJoint2D>();
            hinge.connectedBody = leg.GetComponent<Rigidbody2D>();
            hinge.autoConfigureConnectedAnchor = false;
            hinge.anchor = new Vector2(-0.65f, 0);
            hinge.connectedAnchor = new Vector2(0.75f, 0);
            hinge.enableCollision = false;
            hinge.useLimits = true;
            // jointAngle = initial foot angle - current foot angle (leg rotation is fixed).
            hinge.limits = new JointAngleLimits2D { min = -80, max = 20 };
            foot.AddComponent<AnkleController>().Configure(leg);
            var sock = UnityEngine.Object.FindObjectOfType<SockController>();
            ShapeRow(sock.UpperPoints, 1);
            ShapeRow(sock.LowerPoints, -1);
            sock.RefreshLines();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }
        private static void ShapeRow(SockPhysicsPoint[] row, int direction)
        {
            for (int i = 0; i < row.Length; i++)
            {
                float height = i == 7 || i == 8 ? 0.46f : i == 6 || i == 9 ? 0.55f : 0.65f;
                Vector2 position = new Vector2(-1 + 0.3f * i, direction * height);
                row[i].transform.position = position;
                row[i].Body.position = position;
                if (i > 0) row[i].GetComponent<DistanceJoint2D>().distance = Vector2.Distance(position, row[i - 1].Body.position);
                if (i == 0) row[i].GetComponent<SpringJoint2D>().connectedAnchor = position;
                if (i == 7 || i == 8)
                {
                    var hold = row[i].gameObject.AddComponent<SpringJoint2D>();
                    hold.autoConfigureConnectedAnchor = false;
                    hold.connectedAnchor = position;
                    hold.autoConfigureDistance = false;
                    hold.distance = 0.001f;
                    hold.frequency = 18;
                    hold.dampingRatio = 1;
                }
            }
            Physics2D.SyncTransforms();
        }
        [MenuItem("Sock Physics/Verify Step 5 Physics")]
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
                var leg = UnityEngine.Object.FindObjectOfType<FootController>();
                var ankle = UnityEngine.Object.FindObjectOfType<AnkleController>();
                var sock = UnityEngine.Object.FindObjectOfType<SockController>();
                sock.Initialize();
                ankle.Initialize();
                Physics2D.SyncTransforms();
                var body = leg.GetComponent<Rigidbody2D>();
                for (int pass = 0; pass < 3; pass++)
                {
                    leg.ResetPose();
                    Require(leg.BeginDrag(body.position), "Leg can be grabbed");
                    leg.SetDragTarget(new Vector2(3, 0.5f));
                    Simulate(leg, ankle, sock, 600);
                    Debug.Log("STEP5_STUCK leg=" + body.position + " angle=" + ankle.GetComponent<Rigidbody2D>().rotation);
                    Require(body.position.x > -1.3f && body.position.x < 0.5f, "Bent foot enters sock but is blocked at neck");
                    leg.SetDragTarget(new Vector2(-2.5f, 0));
                    Simulate(leg, ankle, sock, 300);
                    Require(body.position.x < -2, "Can pull back");
                    ankle.SetTargetAngle(0);
                    Simulate(leg, ankle, sock, 200);
                    Debug.Log("STEP5_STRAIGHT angle=" + ankle.GetComponent<Rigidbody2D>().rotation + " joint=" + ankle.GetComponent<HingeJoint2D>().jointAngle);
                    Require(Mathf.Abs(Mathf.DeltaAngle(0, ankle.GetComponent<Rigidbody2D>().rotation)) < 5, "Ankle straightens");
                    leg.SetDragTarget(new Vector2(2, 0));
                    Simulate(leg, ankle, sock, 600);
                    Debug.Log("STEP5_PASSED leg=" + body.position);
                    Require(body.position.x > 1.5f, "Adjusted foot passes neck");
                }
                leg.ResetPose();
                Require(Mathf.Abs(ankle.GetComponent<Rigidbody2D>().rotation + 50) < 0.01f && ankle.TargetAngle == -50, "Ankle resets");
                Require(ankle.GetComponent<Rigidbody2D>().velocity == Vector2.zero && !leg.IsDragging, "Reset clears motion and dragging");
                foreach (var point in sock.GetComponentsInChildren<SockPhysicsPoint>())
                    Require(Vector2.Distance(point.Body.position, point.InitialPosition) < 0.001f, "Sock resets");
                ankle.SetTargetAngle(500);
                Require(ankle.TargetAngle == 30, "Upper input angle is clamped");
                Simulate(leg, ankle, sock, 250);
                Require(Mathf.Abs(Mathf.DeltaAngle(30, ankle.GetComponent<Rigidbody2D>().rotation)) < 3, "Upper joint limit is reachable");
                ankle.SetTargetAngle(-500);
                Require(ankle.TargetAngle == -70, "Lower input angle is clamped");
                Simulate(leg, ankle, sock, 250);
                Require(Mathf.Abs(Mathf.DeltaAngle(-70, ankle.GetComponent<Rigidbody2D>().rotation)) < 3, "Lower joint limit is reachable");
            }
            finally
            {
                Physics2D.simulationMode = mode;
                if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            Debug.Log("STEP5_PHYSICS_VERIFICATION_PASSED");
        }
        private static void Simulate(FootController leg, AnkleController ankle, SockController sock, int count)
        {
            var parts = new[] { leg.GetComponent<Collider2D>(), ankle.GetComponent<Collider2D>() };
            var points = sock.GetComponentsInChildren<SockPhysicsPoint>();
            for (int i = 0; i < count; i++)
            {
                leg.StepPhysics(0.02f);
                ankle.StepPhysics();
                sock.GetComponent<SockRecovery>().StepPhysics();
                Physics2D.Simulate(0.02f);
                foreach (var p in points)
                {
                    Require(!float.IsNaN(p.Body.position.x) && !float.IsNaN(p.Body.position.y) && p.Body.position.sqrMagnitude < 1000 && p.Body.velocity.magnitude < 20, "Cloth remains stable");
                    foreach (var part in parts)
                        Require(p.GetComponent<Collider2D>().Distance(part).distance > -0.08f, "Parts do not deeply penetrate cloth");
                }
                foreach (var row in new[] { sock.UpperPoints, sock.LowerPoints })
                    for (int j = 1; j < row.Length; j++)
                        Require(Vector2.Distance(row[j].Body.position, row[j - 1].Body.position) < 0.38f, "Cloth retains collider coverage");
                var hinge = ankle.GetComponent<HingeJoint2D>();
                Require(Vector2.Distance(hinge.transform.TransformPoint(hinge.anchor), hinge.connectedBody.transform.TransformPoint(hinge.connectedAnchor)) < 0.08f,
                    "Ankle joint stays connected");
                float angle = Mathf.DeltaAngle(leg.GetComponent<Rigidbody2D>().rotation, ankle.GetComponent<Rigidbody2D>().rotation);
                Require(angle > -74 && angle < 34, "Ankle stays within limits with solver tolerance");
            }
        }
        public static void CapturePreview()
        {
            var mode = Physics2D.simulationMode;
            try
            {
                EditorSceneManager.OpenScene(ScenePath);
                Physics2D.simulationMode = SimulationMode2D.Script;
                var leg = UnityEngine.Object.FindObjectOfType<FootController>();
                var ankle = UnityEngine.Object.FindObjectOfType<AnkleController>();
                var sock = UnityEngine.Object.FindObjectOfType<SockController>();
                sock.Initialize(); ankle.Initialize(); Physics2D.SyncTransforms();
                SockSkeletonSetup.CaptureCurrentPreview("Logs/Step5Initial.png");
                leg.BeginDrag(leg.GetComponent<Rigidbody2D>().position);
                leg.SetDragTarget(new Vector2(3, 0.5f));
                Simulate(leg, ankle, sock, 600); sock.RefreshLines();
                SockSkeletonSetup.CaptureCurrentPreview("Logs/Step5Stuck.png");
                leg.SetDragTarget(new Vector2(-2.5f, 0));
                Simulate(leg, ankle, sock, 300);
                ankle.SetTargetAngle(0); Simulate(leg, ankle, sock, 200);
                leg.SetDragTarget(new Vector2(2, 0));
                Simulate(leg, ankle, sock, 600); sock.RefreshLines();
                SockSkeletonSetup.CaptureCurrentPreview("Logs/Step5Passed.png");
            }
            finally { Physics2D.simulationMode = mode; }
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
