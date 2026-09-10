using UnityEngine;

namespace SockPhysics
{
    [RequireComponent(typeof(Rigidbody2D), typeof(HingeJoint2D))]
    public sealed class AnkleController : MonoBehaviour
    {
        [SerializeField] private FootController leg;
        [SerializeField] private float targetAngle = -50;
        [SerializeField, Min(1)] private float turnSpeed = 60;
        [SerializeField, Min(1)] private float motorTorque = 30;
        private Rigidbody2D body;
        private HingeJoint2D hinge;
        private Vector2 startPosition;
        private float startRotation;
        private bool initialized;
        public float TargetAngle => targetAngle;
        public bool InputLocked { get; set; }
        [SerializeField] private bool fitStage;
        [SerializeField] private bool bendStage;
        [SerializeField] private bool resistanceStage;
        public void ShowResistanceInstructions() => resistanceStage = true;
        public void SetBendFreedom(float freedom)
        {
            if (!bendStage) return;
            minimumAngle = Mathf.Lerp(-10, -100, Mathf.Clamp01(freedom));
            targetAngle = Mathf.Clamp(targetAngle, minimumAngle, maximumAngle);
            var joint = GetComponent<HingeJoint2D>();
            joint.limits = new JointAngleLimits2D { min = -maximumAngle, max = -minimumAngle };
        }
        [SerializeField] private float minimumAngle = -70;
        [SerializeField] private float maximumAngle = 30;
        [SerializeField] private float resetAngle = -50;
        public void ConfigureBend()
        {
            bendStage = true; minimumAngle = -100; maximumAngle = 20;
            resetAngle = targetAngle = 0;
            motorTorque = 120;
        }
        public void ShowFitInstructions() => fitStage = true;

        public void Configure(FootController controller) => leg = controller;
        private void Start() => Initialize();
        public void Initialize()
        {
            if (initialized) return;
            body = GetComponent<Rigidbody2D>();
            hinge = GetComponent<HingeJoint2D>();
            startPosition = body.position;
            startRotation = body.rotation;
            leg.PoseReset += ResetPose;
            initialized = true;
        }
        private void Update()
        {
            if (InputLocked) return;
            float input = (Input.GetKey(KeyCode.E) ? 1 : 0) - (Input.GetKey(KeyCode.Q) ? 1 : 0);
            SetTargetAngle(targetAngle + input * turnSpeed * Time.deltaTime);
        }
        public void SetTargetAngle(float angle) => targetAngle = Mathf.Clamp(angle, minimumAngle, maximumAngle);
        private void FixedUpdate() => StepPhysics();
        public void StepPhysics()
        {
            Initialize();
            float relative = Mathf.DeltaAngle(hinge.connectedBody.rotation, body.rotation);
            float speed = Mathf.Clamp(Mathf.DeltaAngle(relative, targetAngle) * 8, -turnSpeed, turnSpeed);
            hinge.motor = new JointMotor2D { motorSpeed = -speed, maxMotorTorque = motorTorque };
            hinge.useMotor = true;
        }
        public void ResetPose()
        {
            body.position = startPosition;
            body.rotation = startRotation;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0;
            targetAngle = resetAngle;
            hinge.motor = new JointMotor2D { motorSpeed = 0, maxMotorTorque = motorTorque };
            Physics2D.SyncTransforms();
        }
        private void OnDestroy()
        {
            if (initialized && leg != null) leg.PoseReset -= ResetPose;
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 410, 170), GUI.skin.box);
            GUILayout.Label(resistanceStage ? "STEP 8 / Push and pull back" : bendStage ? "STEP 7 / Turn the corner" : fitStage ? "STEP 6 / Fit the sock" : "STEP 5 / Ankle and narrow section");
            GUILayout.Label("Drag the light green LEG. Q / E: bend ankle.");
            GUILayout.Label(resistanceStage ? "Push at bend, pull left, repeat. Then use Q to bend." : bendStage ? "At the bend, pull back and use Q toward -90 degrees." : "Push, pull back, straighten with E, then push again.");
            GUILayout.Label("Target ankle: " + targetAngle.ToString("F0") + " degrees");
            if (GUILayout.Button("Reset leg, foot and sock [R]")) leg.ResetPose();
            GUILayout.EndArea();
        }
    }
}
