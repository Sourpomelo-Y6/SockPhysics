using UnityEngine;

namespace SockPhysics
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class FootController : MonoBehaviour
    {
        [SerializeField] private Camera inputCamera;
        [SerializeField, Min(0.1f)] private float followStrength = 45f;
        [SerializeField, Min(0.1f)] private float damping = 14f;
        [SerializeField, Min(0.1f)] private float maxForce = 80f;
        [SerializeField, Min(0.1f)] private float maxSpeed = 6f;

        private Rigidbody2D body;
        private Collider2D footCollider;
        private Vector2 initialPosition;
        private float initialRotation;
        private Vector2 target;
        private Vector2 grabOffset;
        private bool initialized;

        public bool IsDragging { get; private set; }
        public bool InputLocked { get; set; }
        public float MaxSpeed => maxSpeed;
        public event System.Action PoseReset;

        private void Awake() => Initialize();

        private void Initialize()
        {
            if (initialized) return;
            body = GetComponent<Rigidbody2D>();
            footCollider = GetComponent<Collider2D>();
            initialPosition = body.position;
            initialRotation = body.rotation;
            if (inputCamera == null) inputCamera = Camera.main;
            initialized = true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetPose();
                return;
            }

            if (InputLocked) { EndDrag(); return; }
            if (inputCamera == null) return;
            Vector3 screen = Input.mousePosition;
            screen.z = transform.position.z - inputCamera.transform.position.z;
            Vector2 pointer = inputCamera.ScreenToWorldPoint(screen);
            if (Input.GetMouseButtonDown(0)) BeginDrag(pointer);
            if (IsDragging) SetDragTarget(pointer);
            if (Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0)) EndDrag();
        }

        public bool BeginDrag(Vector2 pointer)
        {
            Initialize();
            if (InputLocked) return false;
            if (!footCollider.OverlapPoint(pointer)) return false;
            grabOffset = body.position - pointer;
            target = body.position;
            IsDragging = true;
            return true;
        }

        public void SetDragTarget(Vector2 pointer)
        {
            if (IsDragging) target = pointer + grabOffset;
        }

        public void EndDrag() => IsDragging = false;

        private void FixedUpdate() => StepPhysics(Time.fixedDeltaTime);

        // Also used by the editor's deterministic physics verification.
        public void StepPhysics(float deltaTime)
        {
            Initialize();
            if (deltaTime <= 0f) return;
            Vector2 force = -damping * body.velocity;
            if (IsDragging) force += followStrength * (target - body.position);
            force = Vector2.ClampMagnitude(force, maxForce);
            Vector2 nextVelocity = Vector2.ClampMagnitude(
                body.velocity + force / body.mass * deltaTime, maxSpeed);
            body.AddForce((nextVelocity - body.velocity) * body.mass / deltaTime);
        }

        public void ResetPose()
        {
            Initialize();
            EndDrag();
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.position = initialPosition;
            body.rotation = initialRotation;
            target = initialPosition;
            body.WakeUp();
            Physics2D.SyncTransforms();
            PoseReset?.Invoke();
        }

        private void OnDisable() => EndDrag();
        private void OnApplicationFocus(bool focused)
        {
            if (!focused) EndDrag();
        }
    }
}
