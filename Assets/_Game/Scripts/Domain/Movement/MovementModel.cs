using System;

namespace Worldtree.Domain.Movement
{
    public enum MovementState { Idle, Run, Jump, DoubleJump, Fall }

    public readonly struct MovementInput
    {
        public readonly float Horizontal; // -1..1
        public readonly bool JumpPressed; // true for the fixed step(s) since the button went down

        public MovementInput(float horizontal, bool jumpPressed)
        {
            Horizontal = horizontal;
            JumpPressed = jumpPressed;
        }
    }

    /// <summary>
    /// Pure XY movement: acceleration, gravity, jump / double jump, coyote time and jump buffering.
    /// The host sets <see cref="VelocityX"/>/<see cref="VelocityY"/> from the physics body before each
    /// <see cref="Step"/> (so collisions are respected), then applies them back after.
    /// </summary>
    public sealed class MovementModel
    {
        // A jump launches at JumpVelocity, so anything slower than this while touching ground is a landing, not a launch.
        const float LandedMaxRiseSpeed = 0.1f;
        const float IdleSpeed = 0.05f;

        float coyoteLeft;
        float bufferLeft;

        public float VelocityX;
        public float VelocityY;
        public int JumpsUsed { get; private set; }
        public bool Grounded { get; private set; }
        public MovementState State { get; private set; } = MovementState.Idle;

        public void Step(in MovementInput input, bool grounded, float dt, in MovementConfig c)
        {
            if (dt <= 0) throw new ArgumentOutOfRangeException(nameof(dt));

            var landed = grounded && VelocityY <= LandedMaxRiseSpeed;
            Grounded = landed;
            if (landed)
            {
                JumpsUsed = 0;
                coyoteLeft = c.CoyoteTime;
                VelocityY = 0;
            }
            else
            {
                coyoteLeft = Math.Max(0, coyoteLeft - dt);
                // Walked off a ledge and the coyote window closed: the missing ground jump is spent.
                if (JumpsUsed == 0 && coyoteLeft == 0) JumpsUsed = 1;
            }

            bufferLeft = input.JumpPressed ? c.JumpBufferTime : Math.Max(0, bufferLeft - dt);

            var target = Math.Max(-1f, Math.Min(1f, input.Horizontal)) * c.MaxRunSpeed;
            var holding = Math.Abs(target) > 0f;
            var rate = landed ? (holding ? c.GroundAcceleration : c.GroundDeceleration)
                              : (holding ? c.AirAcceleration : c.AirDeceleration);
            VelocityX = MoveTowards(VelocityX, target, rate * dt);

            if (!landed)
            {
                var g = c.Gravity * (VelocityY < 0 ? c.FallGravityMultiplier : 1f);
                VelocityY = Math.Max(-c.MaxFallSpeed, VelocityY - g * dt);
            }

            // A refused press stays buffered, so it fires on landing (a fresh ground jump), never as an extra air jump.
            if (bufferLeft > 0 && JumpsUsed < c.MaxJumps)
            {
                VelocityY = c.JumpVelocity;
                JumpsUsed++;
                coyoteLeft = 0;
                bufferLeft = 0;
            }

            State = ComputeState(landed && VelocityY <= 0); // a launch step already counts as airborne
        }

        MovementState ComputeState(bool landed)
        {
            if (landed) return Math.Abs(VelocityX) > IdleSpeed ? MovementState.Run : MovementState.Idle;
            if (VelocityY > 0) return JumpsUsed >= 2 ? MovementState.DoubleJump : MovementState.Jump;
            return MovementState.Fall;
        }

        /// <summary>Clears all motion state (respawn).</summary>
        public void Reset()
        {
            VelocityX = VelocityY = 0;
            JumpsUsed = 0;
            coyoteLeft = bufferLeft = 0;
            Grounded = false;
            State = MovementState.Idle;
        }

        static float MoveTowards(float current, float target, float maxDelta) =>
            Math.Abs(target - current) <= maxDelta ? target : current + Math.Sign(target - current) * maxDelta;
    }
}
