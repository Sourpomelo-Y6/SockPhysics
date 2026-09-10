using UnityEngine;

namespace SockPhysics
{
    public sealed class FitEvaluator : MonoBehaviour
    {
        [SerializeField] private FootController leg;
        [SerializeField] private AnkleController ankle;
        [SerializeField] private SockController sock;
        [SerializeField] private Vector2 toeTarget = new Vector2(4.2f, 0);
        [SerializeField] private Vector2 heelTarget = new Vector2(2.7f, -0.2f);
        [SerializeField] private Vector2 ankleTarget = new Vector2(2.75f, 0);
        [SerializeField] private Vector2 legTarget = new Vector2(2, 0);
        [SerializeField, Min(0.01f)] private float positionTolerance = 0.22f;
        [SerializeField, Min(0.01f)] private float holdDuration = 0.75f;
        [SerializeField] private float desiredFootAngle;
        public void ConfigureTargets(Vector2 toe, Vector2 heel, Vector2 anklePosition, Vector2 legPosition, float angle)
        {
            toeTarget = toe; heelTarget = heel; ankleTarget = anklePosition; legTarget = legPosition; desiredFootAngle = angle;
        }
        private bool initialized;
        public bool Cleared { get; private set; }
        public bool CompletionAllowed { get; set; } = true;
        public float HoldTime { get; private set; }
        public float FitScore { get; private set; }
        public bool ToeFits { get; private set; }
        public bool HeelFits { get; private set; }
        public bool AnkleFits { get; private set; }
        public bool LegFits { get; private set; }

        public void Configure(FootController owner, AnkleController foot, SockController cloth) { leg = owner; ankle = foot; sock = cloth; }
        private void Start() => Initialize();
        public void Initialize()
        {
            if (initialized) return;
            leg.PoseReset += ResetProgress;
            initialized = true;
        }
        private void FixedUpdate() => Evaluate(Time.fixedDeltaTime);
        public void Evaluate(float deltaTime)
        {
            Initialize();
            if (Cleared) return;
            var foot = ankle.GetComponent<Rigidbody2D>();
            var legBody = leg.GetComponent<Rigidbody2D>();
            float toeError = Vector2.Distance(foot.GetRelativePoint(new Vector2(0.8f, 0)), toeTarget);
            float heelError = Vector2.Distance(foot.GetRelativePoint(new Vector2(-0.7f, -0.2f)), heelTarget);
            float ankleError = Vector2.Distance(foot.GetRelativePoint(new Vector2(-0.65f, 0)), ankleTarget);
            float legError = Vector2.Distance(legBody.position, legTarget);
            ToeFits = toeError <= positionTolerance;
            HeelFits = heelError <= positionTolerance;
            AnkleFits = ankleError <= positionTolerance;
            LegFits = legError <= positionTolerance;
            FitScore = 25 * (Score(toeError) + Score(heelError) + Score(ankleError) + Score(legError));
            bool valid = CompletionAllowed && ToeFits && HeelFits && AnkleFits && LegFits && Mathf.Abs(Mathf.DeltaAngle(desiredFootAngle, foot.rotation)) < 10 &&
                foot.velocity.magnitude < 0.2f && legBody.velocity.magnitude < 0.2f && Mathf.Abs(foot.angularVelocity) < 5;
            foreach (var row in new[] { sock.UpperPoints, sock.LowerPoints })
                for (int i = 1; i < row.Length; i++)
                    valid &= Vector2.Distance(row[i].Body.position, row[i - 1].Body.position) <
                        row[i].GetComponent<DistanceJoint2D>().distance * 1.25f;
            HoldTime = valid ? HoldTime + Mathf.Max(0, deltaTime) : 0;
            if (HoldTime >= holdDuration)
            {
                Cleared = true;
                leg.EndDrag();
                leg.InputLocked = true;
                ankle.InputLocked = true;
            }
        }
        private float Score(float error) => Mathf.Clamp01(1 - error / (positionTolerance * 2));
        public void ResetProgress()
        {
            Cleared = false; HoldTime = 0; FitScore = 0;
            ToeFits = HeelFits = AnkleFits = LegFits = false;
            leg.InputLocked = false; ankle.InputLocked = false;
        }
        private void OnDestroy() { if (initialized && leg != null) leg.PoseReset -= ResetProgress; }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 190, 410, 190), GUI.skin.box);
            GUILayout.Label(Cleared ? "CLEAR! Sock fitted." : "Align toe, heel, ankle and leg, then hold still.");
            GUILayout.Label("Fit: " + FitScore.ToString("F0") + "%");
            GUILayout.Label("Toe: " + ToeFits + "   Heel: " + HeelFits);
            GUILayout.Label("Ankle: " + AnkleFits + "   Leg: " + LegFits);
            GUILayout.Label("Hold: " + Mathf.Min(HoldTime, holdDuration).ToString("F2") + " / " + holdDuration.ToString("F2") + " s");
            if (GUILayout.Button(Cleared ? "Try again [R]" : "Restart [R]")) leg.ResetPose();
            GUILayout.EndArea();
        }
    }
}
