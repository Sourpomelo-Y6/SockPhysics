using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SockPhysics.Editor
{
    public static class ContactSockSetup
    {
        public const string InsertionPath = "Assets/Scenes/ContactInsertion.unity";
        public const string StraightPath = "Assets/Scenes/ContactStraight.unity";
        public const string BendPath = "Assets/Scenes/ContactBend.unity";
        private static bool capture;
        public static void CaptureAndVerify()
        {
            EditorSceneManager.OpenScene(InsertionPath);
            capture = true;
            try { VerifyPhysics(); } finally { capture = false; }
            BendResistanceSetup.VerifyPhysics();
            BentStageSetup.VerifyPhysics();
            StraightStageSetup.VerifyPhysics();
            AnklePrototypeSetup.VerifyPhysics();
            PrototypeTuningSetup.VerifyPhysics();
            ClosedSockSetup.VerifyPhysics();
            SockSkeletonSetup.VerifyPhysics();
            FootPrototypeSetup.VerifyPhysics();
        }
        [MenuItem("Sock Physics/Open Contact Insertion Scene")]
        public static void OpenInsertion() => Open(InsertionPath);
        [MenuItem("Sock Physics/Open Contact Straight Scene")]
        public static void OpenStraight() => Open(StraightPath);
        [MenuItem("Sock Physics/Open Contact Bend Scene")]
        public static void OpenBend() => Open(BendPath);
        private static void Open(string path)
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(path);
        }
        public static void CreateAndVerify()
        {
            Create(InsertionPath, false, true);
            Create(StraightPath, false, false);
            Create(BendPath, true, false);
            VerifyPhysics();
        }
        private static void Create(string path, bool bent, bool shortSock)
        {
            const string materialPath = "Assets/SockPhysics/Art/ContactCloth.physicsMaterial2D";
            if (AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(materialPath) == null)
                AssetDatabase.CreateAsset(new PhysicsMaterial2D("Contact Cloth") { friction = 0.02f, bounciness = 0 }, materialPath);
            var scene = EditorSceneManager.OpenScene(BentStageSetup.ScenePath);
            UnityEngine.Object.DestroyImmediate(UnityEngine.Object.FindObjectOfType<SockController>().gameObject);
            UnityEngine.Object.DestroyImmediate(UnityEngine.Object.FindObjectOfType<SockToeClosure>().gameObject);
            var leg = UnityEngine.Object.FindObjectOfType<FootController>();
            leg.ConfigureContactMotion();
            var ankle = UnityEngine.Object.FindObjectOfType<AnkleController>();
            ankle.ConfigureContact(bent);
            if (bent)
            {
                leg.transform.position += Vector3.down * 0.25f;
                leg.GetComponent<Rigidbody2D>().position = leg.transform.position;
                ankle.transform.position += Vector3.down * 0.25f;
                ankle.GetComponent<Rigidbody2D>().position = ankle.transform.position;
            }
            var fit = UnityEngine.Object.FindObjectOfType<FitEvaluator>();
            var centers = new List<Vector2>(); var normals = new List<Vector2>();
            float end = shortSock ? 1.4f : bent ? 0.5f : 4.6f;
            int straightSteps = Mathf.CeilToInt((end + 1.5f) / 0.18f);
            for (int i = 0; i <= straightSteps; i++)
            { centers.Add(new Vector2(Mathf.Lerp(-1.5f, end, (float)i / straightSteps), bent ? -0.25f : 0)); normals.Add(Vector2.up); }
            if (bent)
            {
                for (int i = 1; i <= 10; i++)
                {
                    float a = Mathf.PI / 2 * (1 - i / 10f);
                    var normal = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    centers.Add(new Vector2(0.5f, -1.25f) + normal); normals.Add(normal);
                }
                for (int i = 1; i <= 6; i++)
                { centers.Add(new Vector2(1.5f, -1.25f - i / 6f)); normals.Add(Vector2.right); }
            }
            var root = new GameObject("Contact Sock");
            var upper = MakeRow(root.transform, "Outer", centers, normals, 1, new Color(0.3f, 0.7f, 1), out var top);
            var lower = MakeRow(root.transform, "Inner", centers, normals, -1, new Color(1, 0.72f, 0.25f), out var bottom);
            var sock = root.AddComponent<SockController>(); sock.Configure(leg, upper, lower, top, bottom);
            var cloth = root.AddComponent<ContactSock>(); cloth.Configure(normals.ToArray());
            var cap = new GameObject("Contact Toe");
            Vector2 tip = centers[centers.Count - 1] + (bent ? Vector2.down : Vector2.right) * 0.18f;
            var node = Point(cap.transform, "Toe", tip);
            Connect(node.Body, upper[upper.Length - 1].Body); Connect(node.Body, lower[lower.Length - 1].Body);
            var hold = node.gameObject.AddComponent<SpringJoint2D>();
            hold.autoConfigureConnectedAnchor = hold.autoConfigureDistance = false;
            hold.connectedAnchor = tip; hold.distance = 0.001f; hold.frequency = 6; hold.dampingRatio = 1;
            cap.AddComponent<SockToeClosure>().Configure(leg, upper[upper.Length - 1], lower[lower.Length - 1], new[] { node }, Line(cap, new Color(0.65f, 0.65f, 1)));
            fit.Configure(leg, ankle, sock);
            if (shortSock) fit.ConfigureInsertionPractice();
            if (!bent)
            {
                float goal = shortSock ? -1.2f : 2;
                fit.ConfigureTargets(new Vector2(goal + 2.2f, 0), new Vector2(goal + 0.7f, -0.2f), new Vector2(goal + 0.75f, 0), new Vector2(goal, 0), 0);
            }
            else
            {
                var contacts = new List<Collider2D>();
                for (int i = 0; i < upper.Length; i++)
                    if (normals[i].x > 0.7f && centers[i].y > -1.5f) contacts.Add(upper[i].GetComponent<Collider2D>());
                var bend = new GameObject("Bend Resistance").AddComponent<BendController>();
                bend.Configure(leg, ankle, fit, contacts.ToArray(), new SpringJoint2D[0]); bend.ConfigureContactSock(cloth);
                ankle.ShowResistanceInstructions();
            }
            sock.RefreshLines(); cap.GetComponent<SockToeClosure>().Initialize();
            EditorSceneManager.SaveScene(scene, path); AssetDatabase.SaveAssets();
        }
        private static SockPhysicsPoint[] MakeRow(Transform parent, string name, List<Vector2> centers, List<Vector2> normals, float side, Color color, out LineRenderer line)
        {
            var root = new GameObject(name); root.transform.SetParent(parent); line = Line(root, color);
            var row = new SockPhysicsPoint[centers.Count];
            for (int i = 0; i < row.Length; i++)
            {
                row[i] = Point(root.transform, name + " " + i, centers[i] + normals[i] * (0.18f * side));
                if (i > 0) Connect(row[i].Body, row[i - 1].Body);
            }
            return row;
        }
        private static SockPhysicsPoint Point(Transform parent, string name, Vector2 position)
        {
            var obj = new GameObject(name); obj.transform.SetParent(parent); obj.transform.position = position;
            var body = obj.AddComponent<Rigidbody2D>(); body.mass = 0.15f; body.gravityScale = 0; body.drag = 2;
            body.constraints = RigidbodyConstraints2D.FreezeRotation; body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = obj.AddComponent<CircleCollider2D>(); collider.radius = 0.18f;
            collider.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/SockPhysics/Art/ContactCloth.physicsMaterial2D");
            return obj.AddComponent<SockPhysicsPoint>();
        }
        private static void Connect(Rigidbody2D body, Rigidbody2D previous)
        {
            var joint = body.gameObject.AddComponent<DistanceJoint2D>(); joint.connectedBody = previous;
            joint.autoConfigureConnectedAnchor = joint.autoConfigureDistance = false;
            joint.distance = 0.28f; joint.maxDistanceOnly = true; joint.enableCollision = false;
        }
        private static LineRenderer Line(GameObject obj, Color color)
        {
            var line = obj.AddComponent<LineRenderer>();
            line.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/SockPhysics/Art/PrototypeUnlit.mat");
            line.widthMultiplier = 0.36f; line.numCapVertices = line.numCornerVertices = 6;
            line.startColor = line.endColor = color; return line;
        }
        [MenuItem("Sock Physics/Verify Contact Sock Physics")]
        public static void VerifyPhysics()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup(); var mode = Physics2D.simulationMode;
            int positions = Physics2D.positionIterations, velocities = Physics2D.velocityIterations;
            try
            {
                Physics2D.simulationMode = SimulationMode2D.Script;
                VerifyStraight(InsertionPath, -1.2f);
                VerifyStraight(StraightPath, 2);
                VerifyBend();
            }
            finally
            {
                if (setup.Length > 0 && Array.TrueForAll(setup, item => !string.IsNullOrEmpty(item.path)))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Physics2D.simulationMode = mode;
                Physics2D.positionIterations = positions; Physics2D.velocityIterations = velocities;
            }
            Debug.Log("CONTACT_SOCK_VERIFICATION_PASSED");
        }
        private static void VerifyStraight(string path, float goal)
        {
            var run = new Run(path);
            if (path == StraightPath)
            {
                // Same target pose, but both cloth surfaces are above the foot.
                // This is an evaluator-only check, not an insertion simulation.
                run.leg.GetComponent<Rigidbody2D>().position = new Vector2(goal, 0);
                run.ankle.GetComponent<Rigidbody2D>().position = new Vector2(goal + 1.4f, 0);
                foreach (var row in new[] { run.sock.UpperPoints, run.sock.LowerPoints })
                    foreach (var point in row) point.Body.position += Vector2.up * 2;
                Physics2D.SyncTransforms(); run.fit.Evaluate(2);
                Require(run.fit.FitScore > 99 && !run.fit.Cleared, "Outside pose cannot clear even when target positions match");
                run.leg.ResetPose();
            }
            for (int retry = 0; retry < 2; retry++)
            {
                run.fit.CompletionAllowed = false;
                run.Simulate(250); run.CheckClosed();
                run.Capture(path == InsertionPath ? "InsertionClosed" : "StraightClosed");
                run.Drag(new Vector2(goal, 0)); run.Simulate(900);
                Debug.Log("CONTACT_INSERT " + path + " leg=" + run.leg.GetComponent<Rigidbody2D>().position);
                Require(Vector2.Distance(run.leg.GetComponent<Rigidbody2D>().position, new Vector2(goal, 0)) < 0.22f, "Foot enters closed sock");
                Require(run.MaximumGap() > 0.65f, "Foot physically opens cloth");
                run.CheckEnclosed(); run.Capture(path == InsertionPath ? "InsertionOpen" : "StraightOpen");
                run.Drag(new Vector2(-4, 0)); run.Simulate(900); run.leg.EndDrag(); run.Simulate(400); run.CheckClosed();
                run.Capture(path == InsertionPath ? "InsertionWithdrawn" : "StraightWithdrawn");
                run.fit.CompletionAllowed = true; run.Drag(new Vector2(goal, 0)); run.Simulate(900);
                Require(path == InsertionPath ? !run.fit.Cleared : run.fit.Cleared, "Practice stays unlocked; full straight clears");
                run.leg.ResetPose(); Require(!run.fit.Cleared, "Retry clears progress"); run.CheckClosed();
            }
            Debug.Log("CONTACT_STRAIGHT_PASSED " + path);
        }
        private static void VerifyBend()
        {
            var run = new Run(BendPath);
            for (int retry = 0; retry < 2; retry++)
            {
                run.Simulate(250); run.CheckClosed();
                run.Capture("BendClosed");
                for (int push = 0; push < 4; push++)
                {
                    run.Drag(new Vector2(3, -0.25f)); run.Simulate(900);
                    Debug.Log("CONTACT_PUSH " + push + " resistance=" + run.bend.BendResistance + " leg=" + run.leg.GetComponent<Rigidbody2D>().position);
                    run.Capture("BendPush" + push);
                    Require(run.bend.BendResistance == 75 - push * 25, "One reduction per push");
                    run.Simulate(400); Require(run.bend.BendResistance == 75 - push * 25, "Holding cannot repeat");
                    if (push < 3) { run.Drag(new Vector2(-1.7f, -0.25f)); run.Simulate(400); Require(!run.bend.NeedsPullBack, "Pullback rearms"); }
                }
                run.Drag(new Vector2(-0.9f, -0.25f)); run.Simulate(300);
                for (int angle = 15; angle <= 90; angle += 15)
                {
                    float a = angle * Mathf.Deg2Rad;
                    Vector2 center = new Vector2(0.5f + Mathf.Sin(a), -1.25f + Mathf.Cos(a));
                    Vector2 direction = new Vector2(Mathf.Cos(a), -Mathf.Sin(a));
                    run.ankle.SetTargetAngle(-angle);
                    run.Drag(center - direction * 0.65f - Vector2.right * 0.75f); run.Simulate(300);
                }
                run.Drag(new Vector2(0.75f, -0.5f)); run.Simulate(1000);
                Debug.Log("CONTACT_BEND_FIT " + run.fit.FitScore + " leg=" + run.leg.GetComponent<Rigidbody2D>().position + " foot=" + run.ankle.GetComponent<Rigidbody2D>().position + " angle=" + run.ankle.GetComponent<Rigidbody2D>().rotation);
                Require(run.fit.Cleared, "Closed bend can clear");
                run.CheckEnclosed(); run.Capture("BendClear");
                Require(run.fit.ToeFits && run.fit.HeelFits && run.fit.AnkleFits && run.fit.LegFits, "Foot remains fitted after clear");
                run.fit.ResetProgress(); run.fit.CompletionAllowed = false;
                for (int angle = 90; angle >= 0; angle -= 15)
                {
                    float a = angle * Mathf.Deg2Rad;
                    Vector2 center = new Vector2(0.5f + Mathf.Sin(a), -1.25f + Mathf.Cos(a));
                    Vector2 direction = new Vector2(Mathf.Cos(a), -Mathf.Sin(a));
                    run.ankle.SetTargetAngle(-angle);
                    run.Drag(center - direction * 0.65f - Vector2.right * 0.75f); run.Simulate(300);
                }
                run.Drag(new Vector2(-4, -0.25f)); run.Simulate(900); run.leg.EndDrag(); run.Simulate(500);
                run.CheckClosed(); run.Capture("BendWithdrawn");
                run.leg.ResetPose(); Require(run.bend.BendResistance == 100 && !run.fit.Cleared, "Bend retry resets"); run.CheckClosed();
            }
            Debug.Log("CONTACT_BEND_PASSED");
        }
        private sealed class Run
        {
            public readonly FootController leg; public readonly AnkleController ankle; public readonly SockController sock;
            public readonly FitEvaluator fit; public readonly BendController bend;
            private readonly ContactSock cloth; private readonly SockPhysicsPoint[] points; private readonly Collider2D[] parts;
            public Run(string path)
            {
                EditorSceneManager.OpenScene(path); UnityEngine.Object.FindObjectOfType<StagePhysicsSettings>().Apply();
                leg = UnityEngine.Object.FindObjectOfType<FootController>(); ankle = UnityEngine.Object.FindObjectOfType<AnkleController>();
                sock = UnityEngine.Object.FindObjectOfType<SockController>(); fit = UnityEngine.Object.FindObjectOfType<FitEvaluator>();
                bend = UnityEngine.Object.FindObjectOfType<BendController>(); cloth = sock.GetComponent<ContactSock>();
                sock.Initialize(); ankle.Initialize(); fit.Initialize(); cloth.Initialize();
                UnityEngine.Object.FindObjectOfType<SockToeClosure>().Initialize(); if (bend != null) bend.Initialize();
                leg.ResetPose(); Physics2D.SyncTransforms();
                points = UnityEngine.Object.FindObjectsOfType<SockPhysicsPoint>();
                parts = new[] { leg.GetComponent<Collider2D>(), ankle.GetComponent<Collider2D>() };
            }
            public void Drag(Vector2 target) { if (!leg.IsDragging) leg.BeginDrag(leg.GetComponent<Rigidbody2D>().position); leg.SetDragTarget(target); }
            public void Simulate(int frames)
            {
                for (int i = 0; i < frames; i++)
                {
                    leg.StepPhysics(0.02f); ankle.StepPhysics(); cloth.StepPhysics();
                    Physics2D.Simulate(0.02f); if (bend != null) bend.StepPhysics(0.02f); fit.Evaluate(0.02f);
                    foreach (var point in points)
                    {
                        Require(point.Body.position.sqrMagnitude < 1000 && point.Body.velocity.magnitude < 20, "Stable contact cloth");
                        foreach (var joint in point.GetComponents<DistanceJoint2D>())
                            if (Vector2.Distance(point.Body.position, joint.connectedBody.position) >= 0.36f)
                                throw new InvalidOperationException("Cloth gap at " + point.name + ": " + Vector2.Distance(point.Body.position, joint.connectedBody.position));
                        foreach (var part in parts)
                            Require(point.GetComponent<Collider2D>().Distance(part).distance > -0.06f, "No deep foot penetration");
                    }
                }
            }
            public float MaximumGap()
            {
                float max = 0;
                for (int i = 0; i < sock.UpperPoints.Length; i++) max = Mathf.Max(max, Vector2.Distance(sock.UpperPoints[i].Body.position, sock.LowerPoints[i].Body.position));
                return max;
            }
            public void Capture(string name)
            {
                if (!capture) return;
                sock.RefreshLines(); UnityEngine.Object.FindObjectOfType<SockToeClosure>().RefreshLine();
                SockSkeletonSetup.CaptureCurrentPreview("Logs/Contact" + name + ".png");
            }
            public void CheckEnclosed()
            {
                var foot = ankle.GetComponent<Rigidbody2D>();
                foreach (float x in new[] { -0.5f, 0, 0.5f })
                {
                    Vector2 sample = foot.GetRelativePoint(new Vector2(x, 0));
                    int nearest = 0; float best = float.MaxValue;
                    for (int i = 0; i < sock.UpperPoints.Length; i++)
                    {
                        Vector2 center = (sock.UpperPoints[i].InitialPosition + sock.LowerPoints[i].InitialPosition) / 2;
                        float distance = (sample - center).sqrMagnitude;
                        if (distance < best) { best = distance; nearest = i; }
                    }
                    var upper = sock.UpperPoints[nearest]; var lower = sock.LowerPoints[nearest];
                    Vector2 normal = (upper.InitialPosition - lower.InitialPosition).normalized;
                    Require(Vector2.Dot(upper.Body.position - sample, normal) > 0 && Vector2.Dot(lower.Body.position - sample, normal) < 0,
                        "Foot is between the two surfaces, not outside the sock");
                }
            }
            public void CheckClosed()
            {
                Debug.Log("CONTACT_CLOSED maxCenterGap=" + MaximumGap());
                Require(MaximumGap() <= 0.365f, "Opposite surfaces close after withdrawal");
                for (int i = 0; i < sock.UpperPoints.Length; i++)
                {
                    float gap = sock.UpperPoints[i].GetComponent<Collider2D>().Distance(sock.LowerPoints[i].GetComponent<Collider2D>()).distance;
                    Require(gap >= -0.025f && gap <= 0.005f, "Closed surfaces touch without deep overlap");
                }
            }
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
