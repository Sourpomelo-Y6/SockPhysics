using UnityEngine;

namespace SockPhysics
{
    // Rest positions touch. Only collision forces from the foot open the cloth.
    [RequireComponent(typeof(SockController))]
    public sealed class ContactSock : MonoBehaviour
    {
        [SerializeField, Min(0)] private float closingStrength = 4;
        [SerializeField, Min(0)] private float shapeStrength = 60;
        [SerializeField, Min(0)] private float damping = 3;
        [SerializeField, Min(0)] private float forceLimit = 16;
        [SerializeField] private Vector2[] normals;
        private SockController sock;
        private bool initialized;
        private float resistance;

        public void Configure(Vector2[] directions) { normals = directions; }
        public void SetResistance(float fraction) { resistance = Mathf.Clamp01(fraction); }
        private void Start() => Initialize();
        public void Initialize()
        {
            if (initialized) return;
            sock = GetComponent<SockController>();
            sock.Initialize();
            // Closely sampled circles form one continuous surface. Nearby circles
            // on the SAME surface overlap intentionally; opposite surfaces collide.
            foreach (var row in new[] { sock.UpperPoints, sock.LowerPoints })
                for (int i = 0; i < row.Length; i++)
                    for (int j = i + 1; j <= Mathf.Min(i + 2, row.Length - 1); j++)
                        Physics2D.IgnoreCollision(row[i].GetComponent<Collider2D>(), row[j].GetComponent<Collider2D>());
            initialized = true;
        }
        private void FixedUpdate() => StepPhysics();
        public bool ContainsFoot(Rigidbody2D foot)
        {
            Initialize();
            // Check the rear, center and front of the foot against both surfaces.
            // A matching world-space goal alone must not clear an outside approach.
            for (int sampleIndex = -1; sampleIndex <= 1; sampleIndex++)
            {
                Vector2 sample = foot.GetRelativePoint(new Vector2(sampleIndex * 0.5f, 0));
                int nearest = 0; float best = float.MaxValue;
                for (int i = 0; i < normals.Length; i++)
                {
                    Vector2 center = (sock.UpperPoints[i].InitialPosition + sock.LowerPoints[i].InitialPosition) / 2;
                    float distance = (sample - center).sqrMagnitude;
                    if (distance < best) { best = distance; nearest = i; }
                }
                if (Vector2.Dot(sock.UpperPoints[nearest].Body.position - sample, normals[nearest]) <= 0 ||
                    Vector2.Dot(sock.LowerPoints[nearest].Body.position - sample, normals[nearest]) >= 0) return false;
            }
            return true;
        }
        public void StepPhysics()
        {
            Initialize();
            Recover(sock.UpperPoints);
            Recover(sock.LowerPoints);
        }
        private void Recover(SockPhysicsPoint[] row)
        {
            for (int i = 0; i < row.Length; i++)
            {
                Vector2 normal = normals[i];
                Vector2 offset = row[i].InitialPosition - row[i].Body.position;
                Vector2 opening = normal * Vector2.Dot(offset, normal);
                float bendMultiplier = normals[i].x > 0.3f ? 1 + resistance : 1;
                Vector2 force = closingStrength * bendMultiplier * opening + shapeStrength * (offset - opening) - damping * row[i].Body.velocity;
                row[i].Body.AddForce(Vector2.ClampMagnitude(force, forceLimit));
            }
        }
    }
}
