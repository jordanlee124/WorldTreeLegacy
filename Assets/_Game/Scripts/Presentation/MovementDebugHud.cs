using UnityEngine;
using UnityEngine.InputSystem;

namespace Worldtree.Presentation
{
    /// <summary>Debug-only on-screen readout (F3 toggles): movement state, positions, camera and frame rate.</summary>
    public sealed class MovementDebugHud : MonoBehaviour
    {
        [SerializeField] PlayerMotor motor;

        bool visible = true;
        float fps;

        public PlayerMotor Motor { set => motor = value; }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame) visible = !visible;
            fps = Mathf.Lerp(fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 1e-5f), 0.1f);
        }

        void OnGUI()
        {
            if (!visible || motor == null) return;
            var m = motor.Model;
            var p = motor.transform.position;
            var cam = Camera.main;
            var cp = cam != null ? cam.transform.position : Vector3.zero;
            var view = cam != null ? cam.WorldToViewportPoint(p) : Vector3.zero;
            GUI.Label(new Rect(10, 10, 520, 120),
                $"State: {m.State}   Grounded: {m.Grounded}   Jumps used: {m.JumpsUsed}\n" +
                $"Player: ({p.x:0.00}, {p.y:0.00}, {p.z:0.00})   Vel: ({m.VelocityX:0.0}, {m.VelocityY:0.0})   Fall resets: {motor.FallResets}\n" +
                $"Camera: ({cp.x:0.00}, {cp.y:0.00})   Player on screen: ({view.x:0.00}, {view.y:0.00})\n" +
                $"{fps:0} fps   fixed {1f / Time.fixedDeltaTime:0} Hz   A/D move, Space jump, F3 hide");
        }
    }
}
