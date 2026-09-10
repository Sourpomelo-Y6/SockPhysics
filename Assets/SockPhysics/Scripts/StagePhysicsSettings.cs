using UnityEngine;

namespace SockPhysics
{
    public sealed class StagePhysicsSettings : MonoBehaviour
    {
        private int oldPositionIterations;
        private int oldVelocityIterations;
        private bool applied;
        private void OnEnable() { if (Application.isPlaying) Apply(); }
        public void Apply()
        {
            if (applied) return;
            oldPositionIterations = Physics2D.positionIterations;
            oldVelocityIterations = Physics2D.velocityIterations;
            Physics2D.positionIterations = 12;
            Physics2D.velocityIterations = 12;
            applied = true;
        }
        private void OnDisable()
        {
            if (!applied) return;
            Physics2D.positionIterations = oldPositionIterations;
            Physics2D.velocityIterations = oldVelocityIterations;
            applied = false;
        }
    }
}
