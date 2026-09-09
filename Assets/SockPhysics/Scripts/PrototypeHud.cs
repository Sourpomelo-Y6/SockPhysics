using UnityEngine;

namespace SockPhysics
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private FootController foot;
        [SerializeField] private bool sockPrototype;
        [SerializeField] private bool closedSockPrototype;
        [SerializeField] private string titleOverride = "";

        public void Configure(FootController controller) => foot = controller;
        public void ShowSockInstructions() => sockPrototype = true;
        public void ShowClosedSockInstructions() { sockPrototype = true; closedSockPrototype = true; }
        public void ShowTuningInstructions() => titleOverride = "SOCK PHYSICS / Step 4 - Tuning";

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 370, 160), GUI.skin.box);
            GUILayout.Label(!string.IsNullOrEmpty(titleOverride) ? titleOverride : closedSockPrototype ? "SOCK PHYSICS / Step 3 - Open and close" :
                sockPrototype ? "SOCK PHYSICS / Step 2 - Sock skeleton" : "SOCK PHYSICS / Step 1 - Foot movement");
            GUILayout.Label("Drag the green foot with the left mouse button.");
            GUILayout.Label(closedSockPrototype ? "Push in to open the sock. Pull out to close it." :
                sockPrototype ? "Enter between the two rows. Push up or down." : "Push against the blue wall, then pull back.");
            GUILayout.Label("Release to stop. Press R to reset.");
            if (foot != null)
            {
                GUILayout.Label(foot.IsDragging ? "Dragging" : "Ready");
                if (GUILayout.Button(sockPrototype ? "Reset foot + sock [R]" : "Reset foot [R]")) foot.ResetPose();
            }
            GUILayout.EndArea();
        }
    }
}
