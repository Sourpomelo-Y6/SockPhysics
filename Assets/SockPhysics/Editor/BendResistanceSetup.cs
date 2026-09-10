using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SockPhysics.Editor
{
    public static class BendResistanceSetup
    {
        public const string ScenePath = "Assets/Scenes/BendResistance.unity";
        [MenuItem("Sock Physics/Open Step 8 Scene")]
        public static void OpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }
        public static void CreateAndVerify()
        {
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.OpenScene(BentStageSetup.ScenePath);
                var leg = UnityEngine.Object.FindObjectOfType<FootController>();
                var ankle = UnityEngine.Object.FindObjectOfType<AnkleController>();
                var fit = UnityEngine.Object.FindObjectOfType<FitEvaluator>();
                var sock = UnityEngine.Object.FindObjectOfType<SockController>();
                var contacts = new List<Collider2D>(); var holds = new List<SpringJoint2D>();
                foreach (var point in sock.UpperPoints)
                    if (point.transform.position.x > 1.3f && point.transform.position.y > -1.5f)
                    {
                        contacts.Add(point.GetComponent<Collider2D>());
                        holds.Add(point.GetComponent<SpringJoint2D>());
                    }
                ankle.ShowResistanceInstructions();
                new GameObject("Bend Resistance").AddComponent<BendController>().Configure(leg, ankle, fit, contacts.ToArray(), holds.ToArray());
                EditorSceneManager.SaveScene(scene, ScenePath); AssetDatabase.SaveAssets();
            }
            VerifyPhysics();
            BentStageSetup.VerifyPhysics();
            StraightStageSetup.VerifyPhysics();
            AnklePrototypeSetup.VerifyPhysics();
            PrototypeTuningSetup.VerifyPhysics();
            ClosedSockSetup.VerifyPhysics();
            SockSkeletonSetup.VerifyPhysics();
            FootPrototypeSetup.VerifyPhysics();
        }
        [MenuItem("Sock Physics/Verify Step 8 Physics")]
        public static void VerifyPhysics()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup(); var mode = Physics2D.simulationMode;
            int positions = Physics2D.positionIterations, velocities = Physics2D.velocityIterations;
            try
            {
                EditorSceneManager.OpenScene(ScenePath); Physics2D.simulationMode = SimulationMode2D.Script;
                UnityEngine.Object.FindObjectOfType<StagePhysicsSettings>().Apply();
                var leg = UnityEngine.Object.FindObjectOfType<FootController>();
                var ankle = UnityEngine.Object.FindObjectOfType<AnkleController>();
                var sock = UnityEngine.Object.FindObjectOfType<SockController>();
                var fit = UnityEngine.Object.FindObjectOfType<FitEvaluator>();
                var bend = UnityEngine.Object.FindObjectOfType<BendController>();
                sock.Initialize(); ankle.Initialize(); fit.Initialize();
                UnityEngine.Object.FindObjectOfType<SockToeClosure>().Initialize();
                bend.Initialize(); leg.ResetPose(); Physics2D.SyncTransforms();
                var body = leg.GetComponent<Rigidbody2D>(); var foot = ankle.GetComponent<Rigidbody2D>();
                ankle.SetTargetAngle(-90);
                Require(ankle.TargetAngle == -10, "Resistance restricts ankle input");
                body.position = new Vector2(0.75f, -0.5f); foot.position = new Vector2(1.5f, -1.15f); foot.rotation = -90;
                fit.Evaluate(2);
                Require(!fit.Cleared && !fit.CompletionAllowed, "Resistance blocks premature clear even at fit pose");
                leg.ResetPose();
                Simulate(leg, ankle, sock, fit, bend, 200);
                Require(bend.BendResistance == 100, "Idle does not reduce resistance");
                for (int run = 0; run < 2; run++)
                {
                    leg.BeginDrag(body.position);
                    for (int push = 0; push < 4; push++)
                    {
                        leg.SetDragTarget(new Vector2(3, 0));
                        Simulate(leg, ankle, sock, fit, bend, 600);
                        float expected = 75 - 25 * push;
                        Debug.Log("STEP8_PUSH run=" + run + " push=" + push + " resistance=" + bend.BendResistance + " leg=" + body.position);
                        Require(bend.BendResistance == expected, "Exactly one reduction per push");
                        Simulate(leg, ankle, sock, fit, bend, 400);
                        Require(bend.BendResistance == expected, "Holding cannot reduce again");
                        if (push < 3)
                        {
                            for (int j = 0; j < 10; j++)
                            {
                                leg.SetDragTarget(body.position + Vector2.right * (j % 2 == 0 ? 0.03f : -0.03f));
                                Simulate(leg, ankle, sock, fit, bend, 5);
                            }
                            Require(bend.NeedsPullBack && bend.BendResistance == expected, "Small jitter cannot rearm");
                            leg.EndDrag(); Simulate(leg, ankle, sock, fit, bend, 100);
                            Require(bend.NeedsPullBack, "Mouse release alone cannot rearm");
                            leg.BeginDrag(body.position); leg.SetDragTarget(new Vector2(-1.2f, 0));
                            Simulate(leg, ankle, sock, fit, bend, 300);
                            Require(!bend.NeedsPullBack, "Real pullback rearms next push");
                        }
                    }
                    leg.SetDragTarget(new Vector2(-0.7f, 0)); Simulate(leg, ankle, sock, fit, bend, 300);
                    ankle.SetTargetAngle(-90); Simulate(leg, ankle, sock, fit, bend, 300);
                    leg.SetDragTarget(new Vector2(0.75f, -0.5f)); Simulate(leg, ankle, sock, fit, bend, 700);
                    Require(fit.Cleared, "Push/pull cycles unlock the bent-stage clear");
                    leg.ResetPose();
                    Require(bend.BendResistance == 100 && !bend.NeedsPullBack && !fit.Cleared && !fit.CompletionAllowed && ankle.TargetAngle == 0,
                        "Retry resets resistance, state, fit and ankle");
                }
            }
            finally
            {
                Physics2D.simulationMode = mode;
                if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup); else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Physics2D.positionIterations = positions; Physics2D.velocityIterations = velocities;
            }
            Debug.Log("STEP8_PHYSICS_VERIFICATION_PASSED");
        }
        private static void Simulate(FootController leg, AnkleController ankle, SockController sock, FitEvaluator fit, BendController bend, int count)
        {
            for (int i = 0; i < count; i++)
            {
                BentStageSetup.Simulate(leg, ankle, sock, fit, 1);
                bend.StepPhysics(0.02f);
            }
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        public static void CapturePreview()
        {
            var mode = Physics2D.simulationMode;
            int positions = Physics2D.positionIterations, velocities = Physics2D.velocityIterations;
            try
            {
                EditorSceneManager.OpenScene(ScenePath); Physics2D.simulationMode = SimulationMode2D.Script;
                UnityEngine.Object.FindObjectOfType<StagePhysicsSettings>().Apply();
                var leg = UnityEngine.Object.FindObjectOfType<FootController>();
                var ankle = UnityEngine.Object.FindObjectOfType<AnkleController>();
                var sock = UnityEngine.Object.FindObjectOfType<SockController>();
                var fit = UnityEngine.Object.FindObjectOfType<FitEvaluator>();
                var bend = UnityEngine.Object.FindObjectOfType<BendController>();
                var cap = UnityEngine.Object.FindObjectOfType<SockToeClosure>();
                sock.Initialize(); ankle.Initialize(); fit.Initialize(); cap.Initialize(); bend.Initialize(); Physics2D.SyncTransforms();
                SockSkeletonSetup.CaptureCurrentPreview("Logs/Step8Initial.png");
                leg.BeginDrag(leg.GetComponent<Rigidbody2D>().position);
                for (int i = 0; i < 4; i++)
                {
                    leg.SetDragTarget(new Vector2(3, 0)); Simulate(leg, ankle, sock, fit, bend, 600);
                    Require(bend.BendResistance == 75 - i * 25, "Preview counts pushes correctly");
                    sock.RefreshLines(); cap.RefreshLine();
                    if (i == 0) SockSkeletonSetup.CaptureCurrentPreview("Logs/Step8Push.png");
                    if (i < 3)
                    {
                        leg.SetDragTarget(new Vector2(-1.2f, 0)); Simulate(leg, ankle, sock, fit, bend, 300);
                    }
                }
                leg.SetDragTarget(new Vector2(-0.7f, 0)); Simulate(leg, ankle, sock, fit, bend, 300);
                ankle.SetTargetAngle(-90); Simulate(leg, ankle, sock, fit, bend, 300);
                leg.SetDragTarget(new Vector2(0.75f, -0.5f)); Simulate(leg, ankle, sock, fit, bend, 700);
                Require(fit.Cleared, "Preview reaches clear");
                sock.RefreshLines(); cap.RefreshLine();
                SockSkeletonSetup.CaptureCurrentPreview("Logs/Step8Clear.png");
            }
            finally
            {
                Physics2D.simulationMode = mode;
                Physics2D.positionIterations = positions; Physics2D.velocityIterations = velocities;
            }
        }
    }
}
