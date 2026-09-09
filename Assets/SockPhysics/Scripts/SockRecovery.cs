using UnityEngine;

namespace SockPhysics
{
    [RequireComponent(typeof(SockController))]
    public sealed class SockRecovery : MonoBehaviour
    {
        [SerializeField, Min(0)] private float recoveryStrength = 4f;
        [SerializeField, Min(0)] private float recoveryDamping = 1.5f;
        [SerializeField, Range(0, 1)] private float horizontalRecovery = 0.3f;
        [SerializeField, Min(0)] private float maxRecoveryForce = 8f;
        private SockController sock;

        public void Configure(float strength, float damping, float horizontal, float forceLimit)
        {
            recoveryStrength = Mathf.Max(0, strength);
            recoveryDamping = Mathf.Max(0, damping);
            horizontalRecovery = Mathf.Clamp01(horizontal);
            maxRecoveryForce = Mathf.Max(0, forceLimit);
        }

        private void FixedUpdate() => StepPhysics();

        public void StepPhysics()
        {
            if (sock == null) sock = GetComponent<SockController>();
            Recover(sock.UpperPoints);
            Recover(sock.LowerPoints);
        }

        private void Recover(SockPhysicsPoint[] points)
        {
            if (points == null) return;
            foreach (var point in points)
            {
                Vector2 offset = point.InitialPosition - point.Body.position;
                offset.x *= horizontalRecovery;
                Vector2 force = recoveryStrength * offset - recoveryDamping * point.Body.velocity;
                point.Body.AddForce(Vector2.ClampMagnitude(force, maxRecoveryForce));
            }
        }
    }
}
