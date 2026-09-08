using UnityEngine;

namespace SockPhysics
{
    public sealed class SockController : MonoBehaviour
    {
        [SerializeField] private FootController foot;
        [SerializeField] private SockPhysicsPoint[] upperPoints;
        [SerializeField] private SockPhysicsPoint[] lowerPoints;
        [SerializeField] private LineRenderer upperLine;
        [SerializeField] private LineRenderer lowerLine;
        private bool subscribed;

        public SockPhysicsPoint[] UpperPoints => upperPoints;
        public SockPhysicsPoint[] LowerPoints => lowerPoints;

        public void Configure(FootController controller, SockPhysicsPoint[] upper,
            SockPhysicsPoint[] lower, LineRenderer top, LineRenderer bottom)
        {
            foot = controller;
            upperPoints = upper;
            lowerPoints = lower;
            upperLine = top;
            lowerLine = bottom;
            Initialize();
        }

        private void OnEnable()
        {
            if (upperPoints != null && lowerPoints != null) Initialize();
        }

        public void Initialize()
        {
            foreach (var point in upperPoints) point.Initialize();
            foreach (var point in lowerPoints) point.Initialize();
            if (!subscribed && foot != null)
            {
                foot.PoseReset += ResetShape;
                subscribed = true;
            }
            RefreshLines();
        }

        private void OnDisable()
        {
            if (subscribed && foot != null) foot.PoseReset -= ResetShape;
            subscribed = false;
        }

        public void ResetShape()
        {
            foreach (var point in upperPoints) point.ResetPose();
            foreach (var point in lowerPoints) point.ResetPose();
            Physics2D.SyncTransforms();
            RefreshLines();
        }

        private void LateUpdate() => RefreshLines();

        public void RefreshLines()
        {
            RefreshLine(upperLine, upperPoints);
            RefreshLine(lowerLine, lowerPoints);
        }

        private static void RefreshLine(LineRenderer line, SockPhysicsPoint[] points)
        {
            if (line == null || points == null) return;
            line.positionCount = points.Length;
            for (int i = 0; i < points.Length; i++)
                line.SetPosition(i, points[i].transform.position);
        }
    }
}
