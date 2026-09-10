using UnityEngine;

namespace SockPhysics
{
    public sealed class BendController : MonoBehaviour
    {
        [SerializeField] private FootController leg;
        [SerializeField] private AnkleController ankle;
        [SerializeField] private FitEvaluator fit;
        [SerializeField] private Collider2D[] bendContacts;
        [SerializeField] private SpringJoint2D[] bendHolds;
        [SerializeField] private ContactSock contactSock;
        [SerializeField, Min(1)] private float initialResistance = 100;
        [SerializeField, Min(1)] private float reductionPerPush = 25;
        [SerializeField, Min(0.02f)] private float pushDuration = 0.12f;
        [SerializeField, Min(0.1f)] private float returnDistance = 0.35f;
        private bool initialized;
        private float contactTime;
        private float pushPosition;
        public float BendResistance { get; private set; }
        public bool NeedsPullBack { get; private set; }

        public void Configure(FootController owner, AnkleController foot, FitEvaluator evaluator, Collider2D[] contacts, SpringJoint2D[] holds)
        { leg = owner; ankle = foot; fit = evaluator; bendContacts = contacts; bendHolds = holds; }
        public void ConfigureContactSock(ContactSock cloth) => contactSock = cloth;
        private void Start() => Initialize();
        public void Initialize()
        {
            if (initialized) return;
            initialized = true;
            leg.PoseReset += ResetResistance;
            ResetResistance();
        }
        public void ResetResistance()
        {
            BendResistance = initialResistance;
            contactTime = 0; pushPosition = 0; NeedsPullBack = false;
            ApplyResistance();
        }
        private void FixedUpdate() => StepPhysics(Time.fixedDeltaTime);
        public void StepPhysics(float deltaTime)
        {
            Initialize();
            if (BendResistance <= 0 || fit.Cleared) return;
            var footCollider = ankle.GetComponent<Collider2D>();
            bool contact = false;
            foreach (var candidate in bendContacts)
                contact |= candidate.Distance(footCollider).distance <= 0.03f;
            float position = leg.GetComponent<Rigidbody2D>().position.x;
            if (NeedsPullBack)
            {
                if (!contact && leg.DragError.x < -0.1f && position <= pushPosition - returnDistance)
                    NeedsPullBack = false;
                return;
            }
            bool pushing = contact && leg.DragError.x > 0.2f && Mathf.Abs(Mathf.DeltaAngle(0, ankle.GetComponent<Rigidbody2D>().rotation)) < 15;
            contactTime = pushing ? contactTime + Mathf.Max(0, deltaTime) : 0;
            if (contactTime < pushDuration) return;
            BendResistance = Mathf.Max(0, BendResistance - reductionPerPush);
            contactTime = 0;
            pushPosition = position;
            NeedsPullBack = BendResistance > 0;
            ApplyResistance();
        }
        private void ApplyResistance()
        {
            float freedom = 1 - BendResistance / Mathf.Max(1, initialResistance);
            ankle.SetBendFreedom(freedom);
            foreach (var hold in bendHolds) hold.frequency = Mathf.Lerp(16, 12, freedom);
            if (contactSock != null) contactSock.SetResistance(1 - freedom);
            fit.CompletionAllowed = BendResistance <= 0;
        }
        private void OnDestroy() { if (initialized && leg != null) leg.PoseReset -= ResetResistance; }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(Mathf.Max(16, Screen.width - 335), 16, 319, 160), GUI.skin.box);
            GUILayout.Label("Bend resistance: " + BendResistance.ToString("F0"));
            GUILayout.Label(BendResistance <= 0 ? "READY: pull back, Q to -90, then move down." : NeedsPullBack ? "PULL LEFT until the next push is ready." : "PUSH RIGHT against the bend with a straight foot.");
            GUILayout.Label("Holding or tiny movements do not count again.");
            GUILayout.EndArea();
        }
    }
}
