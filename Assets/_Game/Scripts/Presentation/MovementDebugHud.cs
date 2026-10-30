using UnityEngine;

namespace Worldtree.Presentation
{
    /// <summary>Debug-only on-screen readout for the movement test scene.</summary>
    public sealed class MovementDebugHud : MonoBehaviour
    {
        [SerializeField] PlayerMotor motor;

        public PlayerMotor Motor { set => motor = value; }

        void OnGUI()
        {
            if (motor == null) return;
            var m = motor.Model;
            GUI.Label(new Rect(10, 10, 420, 80),
                $"State: {m.State}   Grounded: {m.Grounded}\nJumps used: {m.JumpsUsed}   Vel: ({m.VelocityX:0.0}, {m.VelocityY:0.0})\nFall resets: {motor.FallResets}\nA/D or stick = move, Space or (A) = jump");
        }
    }
}
