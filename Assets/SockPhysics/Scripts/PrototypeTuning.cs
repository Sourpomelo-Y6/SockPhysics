using UnityEngine;

namespace SockPhysics
{
    [RequireComponent(typeof(SockController), typeof(SockRecovery))]
    public sealed class PrototypeTuning : MonoBehaviour
    {
        public enum Preset { Standard, Slippery, Firm, Soft }
        [SerializeField] private FootController foot;
        [SerializeField, Range(0, 1)] private float friction = 0.2f;
        [SerializeField, Range(1, 8)] private float recoveryStrength = 4;
        [SerializeField, Range(0.5f, 4)] private float recoveryDamping = 1.5f;
        [SerializeField, Range(0.1f, 1)] private float horizontalRecovery = 0.3f;
        [SerializeField, Range(2, 12)] private float maxRecoveryForce = 8;
        [SerializeField, Range(2, 10)] private float pointDrag = 5;
        private string currentName = "Standard";
        private PhysicsMaterial2D runtimeMaterial;
        private Collider2D[] colliders;
        private PhysicsMaterial2D[] originalMaterials;

        public void Configure(FootController controller) => foot = controller;
        private void OnEnable() { if (Application.isPlaying) Apply(); }

        private void OnValidate()
        {
            friction = Mathf.Clamp01(friction);
            recoveryStrength = Mathf.Clamp(recoveryStrength, 1, 8);
            recoveryDamping = Mathf.Clamp(recoveryDamping, 0.5f, 4);
            horizontalRecovery = Mathf.Clamp(horizontalRecovery, 0.1f, 1);
            maxRecoveryForce = Mathf.Clamp(maxRecoveryForce, 2, 12);
            pointDrag = Mathf.Clamp(pointDrag, 2, 10);
        }

        public void SelectPreset(Preset preset)
        {
            friction = preset == Preset.Slippery ? 0.02f : 0.2f;
            recoveryStrength = preset == Preset.Firm ? 6 : preset == Preset.Soft ? 2 : 4;
            recoveryDamping = 1.5f;
            horizontalRecovery = 0.3f;
            maxRecoveryForce = 8;
            pointDrag = 5;
            currentName = preset.ToString();
            Apply();
            foot.ResetPose();
        }

        public void Apply()
        {
            if (foot == null) return;
            GetComponent<SockRecovery>().Configure(recoveryStrength, recoveryDamping, horizontalRecovery, maxRecoveryForce);
            foreach (var point in GetComponentsInChildren<SockPhysicsPoint>()) point.Body.drag = pointDrag;
            if (runtimeMaterial == null)
            {
                runtimeMaterial = new PhysicsMaterial2D("Prototype tuning (runtime)") { hideFlags = HideFlags.DontSave };
                var sockColliders = GetComponentsInChildren<Collider2D>();
                colliders = new Collider2D[sockColliders.Length + 1];
                sockColliders.CopyTo(colliders, 0);
                colliders[colliders.Length - 1] = foot.GetComponent<Collider2D>();
                originalMaterials = new PhysicsMaterial2D[colliders.Length];
                for (int i = 0; i < colliders.Length; i++) originalMaterials[i] = colliders[i].sharedMaterial;
            }
            runtimeMaterial.friction = friction;
            runtimeMaterial.bounciness = 0;
            foreach (var collider in colliders) collider.sharedMaterial = runtimeMaterial;
        }

        private void OnDestroy()
        {
            if (runtimeMaterial == null) return;
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].sharedMaterial = originalMaterials[i];
            if (Application.isPlaying) Destroy(runtimeMaterial);
            else DestroyImmediate(runtimeMaterial);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 180, 370, 170), GUI.skin.box);
            GUILayout.Label("STEP 4 / Compare sock feel");
            GUILayout.Label("Setting: " + currentName);
            GUILayout.BeginHorizontal();
            foreach (Preset preset in System.Enum.GetValues(typeof(Preset)))
                if (GUILayout.Button(preset.ToString())) SelectPreset(preset);
            GUILayout.EndHorizontal();
            GUILayout.Label("Friction: " + friction.ToString("0.00") + "   Recovery: " + recoveryStrength.ToString("0.0"));
            GUILayout.Label("Edit PrototypeTuning in Inspector, then apply.");
            if (GUILayout.Button("Apply Inspector values + reset"))
            {
                currentName = "Custom";
                Apply();
                foot.ResetPose();
            }
            GUILayout.EndArea();
        }
    }
}
