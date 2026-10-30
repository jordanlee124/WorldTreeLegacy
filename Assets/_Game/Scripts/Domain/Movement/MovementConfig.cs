using System;

namespace Worldtree.Domain.Movement
{
    /// <summary>Tuning for the player movement model. Plain data so Unity can serialize it from a ScriptableObject.</summary>
    [Serializable]
    public struct MovementConfig
    {
        public float MaxRunSpeed;        // m/s
        public float GroundAcceleration; // m/s^2, while input is held
        public float GroundDeceleration; // m/s^2, while input is released
        public float AirAcceleration;
        public float AirDeceleration;
        public float JumpVelocity;       // m/s, set (not added) on every jump
        public float Gravity;            // m/s^2, positive = downward
        public float FallGravityMultiplier;
        public float MaxFallSpeed;       // m/s, positive
        public float CoyoteTime;         // s a ground jump stays allowed after leaving a ledge
        public float JumpBufferTime;     // s a jump press is remembered while it cannot be used
        public int MaxJumps;             // jumps per airtime including the first; 2 = double jump

        public static MovementConfig Default => new MovementConfig
        {
            MaxRunSpeed = 8f,
            GroundAcceleration = 60f,
            GroundDeceleration = 80f,
            AirAcceleration = 40f,
            AirDeceleration = 30f,
            JumpVelocity = 13f,
            Gravity = 35f,
            FallGravityMultiplier = 1.3f,
            MaxFallSpeed = 25f,
            CoyoteTime = 0.1f,
            JumpBufferTime = 0.1f,
            MaxJumps = 2,
        };

        /// <summary>Returns the first problem found, or null when the config is usable.</summary>
        public string Validate()
        {
            if (MaxRunSpeed <= 0) return nameof(MaxRunSpeed) + " must be > 0";
            if (GroundAcceleration <= 0) return nameof(GroundAcceleration) + " must be > 0";
            if (GroundDeceleration <= 0) return nameof(GroundDeceleration) + " must be > 0";
            if (AirAcceleration <= 0) return nameof(AirAcceleration) + " must be > 0";
            if (AirDeceleration <= 0) return nameof(AirDeceleration) + " must be > 0";
            if (JumpVelocity <= 0) return nameof(JumpVelocity) + " must be > 0";
            if (Gravity <= 0) return nameof(Gravity) + " must be > 0";
            if (FallGravityMultiplier < 1) return nameof(FallGravityMultiplier) + " must be >= 1";
            if (MaxFallSpeed <= 0) return nameof(MaxFallSpeed) + " must be > 0";
            if (CoyoteTime < 0) return nameof(CoyoteTime) + " must be >= 0";
            if (JumpBufferTime < 0) return nameof(JumpBufferTime) + " must be >= 0";
            if (MaxJumps < 1) return nameof(MaxJumps) + " must be >= 1";
            return null;
        }
    }
}
