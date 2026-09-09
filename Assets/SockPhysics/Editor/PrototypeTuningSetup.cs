using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SockPhysics.Editor
{
    public static class PrototypeTuningSetup
    {
        public const string ScenePath = "Assets/Scenes/PrototypeTuning.unity";

        [MenuItem("Sock Physics/Open Step 4 Scene")]
        public static void OpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        public static void CreateAndVerify()
        {
            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.OpenScene(ClosedSockSetup.ScenePath);
                var foot = UnityEngine.Object.FindObjectOfType<FootController>();
                UnityEngine.Object.FindObjectOfType<PrototypeHud>().ShowTuningInstructions();
                UnityEngine.Object.FindObjectOfType<SockController>().gameObject.AddComponent<PrototypeTuning>().Configure(foot);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
            }
            VerifyPhysics();
            ClosedSockSetup.VerifyPhysics();
            SockSkeletonSetup.VerifyPhysics();
            FootPrototypeSetup.VerifyPhysics();
        }

        [MenuItem("Sock Physics/Verify Step 4 Physics")]
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
                var tuning = sock.GetComponent<PrototypeTuning>();
                var recovery = sock.GetComponent<SockRecovery>();
                var body = foot.GetComponent<Rigidbody2D>();
                sock.Initialize();
                Physics2D.SyncTransforms();
                var insertion = new float[4];
                foreach (PrototypeTuning.Preset preset in Enum.GetValues(typeof(PrototypeTuning.Preset)))
                {
                    tuning.SelectPreset(preset);
                    for (int pass = 0; pass < 3; pass++)
                    {
                        if (!foot.BeginDrag(body.position)) throw new InvalidOperationException("Cannot grab foot");
                        // Sudden target jumps test maximum allowed movement speed and diagonal entry.
                        foot.SetDragTarget(new Vector2(2.5f, pass == 1 ? 0.25f : pass == 2 ? -0.25f : 0));
                        ClosedSockSetup.Simulate(foot, sock, recovery, 600);
                        if (body.position.x < 1) throw new InvalidOperationException(preset + " failed insertion: " + body.position);
                        Debug.Log("STEP4_METRIC " + preset + " pass=" + pass + " insertionX=" + body.position.x.ToString("F3"));
                        if (pass == 0) insertion[(int)preset] = body.position.x;
                        foot.SetDragTarget(new Vector2(-3, 0));
                        ClosedSockSetup.Simulate(foot, sock, recovery, 500);
                        if (body.position.x > -2.8f) throw new InvalidOperationException(preset + " failed withdrawal");
                        foot.EndDrag();
                        ClosedSockSetup.Simulate(foot, sock, recovery, 750);
                        ClosedSockSetup.CheckClosed(sock);
                    }
                }
                if (!(insertion[1] > insertion[0] + 0.05f && insertion[3] > insertion[0] + 0.02f && insertion[2] < insertion[0] - 0.02f))
                    throw new InvalidOperationException("Preset differences must affect insertion resistance");
                tuning.SelectPreset(PrototypeTuning.Preset.Standard);
            }
            finally
            {
                Physics2D.simulationMode = mode;
                if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
            Debug.Log("STEP4_PHYSICS_VERIFICATION_PASSED");
        }
    }
}
