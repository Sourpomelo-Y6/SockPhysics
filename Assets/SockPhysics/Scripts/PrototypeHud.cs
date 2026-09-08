using UnityEngine;

namespace SockPhysics
{
    public sealed class PrototypeHud : MonoBehaviour
    {
        [SerializeField] private FootController foot;
        [SerializeField] private bool sockPrototype;

        public void Configure(FootController controller) => foot = controller;
        public void ShowSockInstructions() => sockPrototype = true;

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(16, 16, 370, 160), GUI.skin.box);
            GUILayout.Label(sockPrototype ? "SOCK PHYSICS / Step 2 - Sock skeleton" : "SOCK PHYSICS / Step 1 - Foot movement");
            GUILayout.Label("Drag the green foot with the left mouse button.");
            GUILayout.Label(sockPrototype ? "Enter between the two rows. Push up or down." : "Push against the blue wall, then pull back.");
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
