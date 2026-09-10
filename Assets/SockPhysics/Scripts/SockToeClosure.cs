using UnityEngine;

namespace SockPhysics
{
    public sealed class SockToeClosure : MonoBehaviour
    {
        [SerializeField] private FootController leg;
        [SerializeField] private SockPhysicsPoint upperEnd;
        [SerializeField] private SockPhysicsPoint lowerEnd;
        [SerializeField] private SockPhysicsPoint[] points;
        [SerializeField] private LineRenderer line;
        private bool initialized;
        public void Configure(FootController owner, SockPhysicsPoint upper, SockPhysicsPoint lower, SockPhysicsPoint[] nodes, LineRenderer renderer)
        { leg = owner; upperEnd = upper; lowerEnd = lower; points = nodes; line = renderer; }
        private void Start() => Initialize();
        public void Initialize()
        {
            if (initialized) return;
            foreach (var point in points) point.Initialize();
            leg.PoseReset += ResetShape;
            initialized = true;
            RefreshLine();
        }
        public void ResetShape()
        {
            foreach (var point in points) point.ResetPose();
            Physics2D.SyncTransforms();
            RefreshLine();
        }
        private void LateUpdate() => RefreshLine();
        public void RefreshLine()
        {
            if (line == null || points == null) return;
            line.positionCount = points.Length + 2;
            line.SetPosition(0, upperEnd.transform.position);
            for (int i = 0; i < points.Length; i++) line.SetPosition(i + 1, points[i].transform.position);
            line.SetPosition(points.Length + 1, lowerEnd.transform.position);
        }
        private void OnDestroy() { if (initialized && leg != null) leg.PoseReset -= ResetShape; }
    }
}
