using UnityEngine;

namespace SockPhysics
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class SockPhysicsPoint : MonoBehaviour
    {
        private Rigidbody2D body;
        private Vector2 initialPosition;
        private float initialRotation;
        public Rigidbody2D Body { get { Initialize(); return body; } }
        public Vector2 InitialPosition { get { Initialize(); return initialPosition; } }

        public void Initialize()
        {
            if (body != null) return;
            body = GetComponent<Rigidbody2D>();
            initialPosition = body.position;
            initialRotation = body.rotation;
        }

        public void ResetPose()
        {
            Initialize();
            body.position = initialPosition;
            body.rotation = initialRotation;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0;
            body.WakeUp();
        }
    }
}
