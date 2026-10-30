using System;
using NUnit.Framework;
using Worldtree.Domain;
using Worldtree.Domain.Movement;

namespace Worldtree.Tests
{
    public class MovementModelTests
    {
        const float Dt = SimulationConfig.FixedStepSeconds;
        static readonly MovementConfig C = MovementConfig.Default;

        /// <summary>Tiny world: a floor at y = 0 that ends at LedgeX. Applies the model's velocity the way the rigidbody would.</summary>
        sealed class Sim
        {
            public readonly MovementModel Model = new MovementModel();
            public float X, Y, PeakY;
            public float LedgeX = float.PositiveInfinity;

            public void Step(float horizontal = 0f, bool jump = false)
            {
                var onFloor = Y <= 0f && X < LedgeX;
                Model.Step(new MovementInput(horizontal, jump), onFloor, Dt, C);
                X += Model.VelocityX * Dt;
                Y += Model.VelocityY * Dt;
                if (X < LedgeX && Y < 0f) { Y = 0f; Model.VelocityY = 0f; }
                PeakY = Math.Max(PeakY, Y);
            }

            public void Run(int steps, float horizontal = 0f) { for (var i = 0; i < steps; i++) Step(horizontal); }
            public void RunSeconds(float s, float horizontal = 0f) => Run((int)Math.Round(s / Dt), horizontal);
            public int RunUntilLanded() { var n = 0; do { Step(); n++; } while (Y > 0f && n < 600); return n; }
            public void WalkOffLedge(float ledgeX) { LedgeX = ledgeX; while (X < LedgeX) Step(1f); }
        }

        static Sim Jumped()
        {
            var s = new Sim();
            s.Step();
            s.Step(0f, true);
            return s;
        }

        /// <summary>Both jumps spent and still rising.</summary>
        static Sim BothJumpsSpent()
        {
            var s = Jumped();
            s.RunSeconds(0.2f);
            s.Step(0f, true);
            s.RunSeconds(0.05f);
            return s;
        }

        [Test]
        public void Runs_ToMaxSpeed_AndNoFurther()
        {
            var s = new Sim();
            s.RunSeconds(1f, 1f);
            Assert.AreEqual(C.MaxRunSpeed, s.Model.VelocityX, 1e-4f);
            Assert.AreEqual(MovementState.Run, s.Model.State);
        }

        [Test]
        public void RunsLeft_AndStopsOnRelease()
        {
            var s = new Sim();
            s.RunSeconds(1f, -1f);
            Assert.AreEqual(-C.MaxRunSpeed, s.Model.VelocityX, 1e-4f);
            s.RunSeconds(1f);
            Assert.AreEqual(0f, s.Model.VelocityX, 1e-4f);
            Assert.AreEqual(MovementState.Idle, s.Model.State);
        }

        [Test]
        public void Acceleration_IsGradual()
        {
            var s = new Sim();
            s.Step(1f);
            Assert.AreEqual(C.GroundAcceleration * Dt, s.Model.VelocityX, 1e-4f);
        }

        [Test]
        public void HorizontalInput_IsClamped()
        {
            var s = new Sim();
            s.RunSeconds(1f, 5f);
            Assert.AreEqual(C.MaxRunSpeed, s.Model.VelocityX, 1e-4f);
        }

        [Test]
        public void GroundJump_LaunchesAtJumpVelocity_AndReachesExpectedHeight()
        {
            var s = Jumped();
            Assert.AreEqual(C.JumpVelocity, s.Model.VelocityY, 1e-4f);
            Assert.AreEqual(1, s.Model.JumpsUsed);
            Assert.AreEqual(MovementState.Jump, s.Model.State);
            s.RunUntilLanded();
            var expected = C.JumpVelocity * C.JumpVelocity / (2f * C.Gravity);
            Assert.AreEqual(expected, s.PeakY, expected * 0.1f);
        }

        [Test]
        public void DoubleJump_WorksInTheAir_AndGoesHigherThanSingle()
        {
            var single = Jumped();
            single.RunUntilLanded();

            var s = Jumped();
            s.RunSeconds(0.3f);
            s.Step(0f, true);
            Assert.AreEqual(2, s.Model.JumpsUsed);
            Assert.AreEqual(C.JumpVelocity, s.Model.VelocityY, 1e-4f);
            Assert.AreEqual(MovementState.DoubleJump, s.Model.State);
            s.RunUntilLanded();
            Assert.Greater(s.PeakY, single.PeakY * 1.5f);
        }

        [Test]
        public void ThirdJump_IsRejected()
        {
            var s = BothJumpsSpent();
            var vyBefore = s.Model.VelocityY;
            s.Step(0f, true);
            Assert.AreEqual(2, s.Model.JumpsUsed);
            Assert.Less(s.Model.VelocityY, vyBefore, "third press must not launch");
        }

        [Test]
        public void Landing_ResetsJumpCount()
        {
            var s = BothJumpsSpent();
            s.RunUntilLanded();
            s.Step(0f, true);
            Assert.AreEqual(1, s.Model.JumpsUsed);
            Assert.AreEqual(C.JumpVelocity, s.Model.VelocityY, 1e-4f);
        }

        [Test]
        public void Coyote_JumpInsideWindow_IsTheFirstGroundJump()
        {
            var s = new Sim();
            s.WalkOffLedge(2f);
            s.Run(2, 1f);
            s.Step(1f, true);
            Assert.AreEqual(1, s.Model.JumpsUsed);
            Assert.AreEqual(C.JumpVelocity, s.Model.VelocityY, 1e-4f);
            s.RunSeconds(0.2f, 1f);
            s.Step(1f, true);
            Assert.AreEqual(2, s.Model.JumpsUsed, "one air jump remains");
            Assert.AreEqual(C.JumpVelocity, s.Model.VelocityY, 1e-4f);
        }

        [Test]
        public void Coyote_JumpAfterWindow_LeavesOnlyOneJump()
        {
            var s = new Sim();
            s.WalkOffLedge(2f);
            s.RunSeconds(C.CoyoteTime + 0.1f, 1f);
            s.Step(1f, true);
            Assert.AreEqual(2, s.Model.JumpsUsed);
            Assert.AreEqual(C.JumpVelocity, s.Model.VelocityY, 1e-4f);
            s.RunSeconds(0.2f, 1f);
            var vy = s.Model.VelocityY;
            s.Step(1f, true);
            Assert.Less(s.Model.VelocityY, vy, "no third jump");
        }

        [Test]
        public void Buffer_PressJustBeforeLanding_JumpsOnLanding()
        {
            var steps = BothJumpsSpent().RunUntilLanded();
            var s = BothJumpsSpent();
            s.Run(steps - 3);
            s.Step(0f, true);       // refused mid-air, remembered
            s.Run(2);
            Assert.AreEqual(0f, s.Y);
            s.Step();               // landing step consumes the buffer
            Assert.AreEqual(1, s.Model.JumpsUsed);
            Assert.AreEqual(C.JumpVelocity, s.Model.VelocityY, 1e-4f);
        }

        [Test]
        public void Buffer_PressLongBeforeLanding_IsForgotten()
        {
            var steps = BothJumpsSpent().RunUntilLanded();
            var s = BothJumpsSpent();
            s.Run(steps - 12);      // 0.2 s before landing, buffer is 0.1 s
            s.Step(0f, true);
            s.Run(11);
            Assert.AreEqual(0f, s.Y);
            s.Step();
            Assert.AreEqual(0, s.Model.JumpsUsed);
            Assert.AreEqual(0f, s.Model.VelocityY, 1e-4f);
        }

        [Test]
        public void Falling_IsClampedToMaxFallSpeed()
        {
            var s = new Sim { LedgeX = -1f };
            s.RunSeconds(3f);
            Assert.AreEqual(-C.MaxFallSpeed, s.Model.VelocityY, 1e-4f);
            Assert.AreEqual(MovementState.Fall, s.Model.State);
        }

        [Test]
        public void Reset_ClearsMotion()
        {
            var s = BothJumpsSpent();
            s.Model.Reset();
            Assert.AreEqual(0, s.Model.JumpsUsed);
            Assert.AreEqual(0f, s.Model.VelocityY);
        }

        [Test]
        public void Step_RejectsNonPositiveDt() =>
            Assert.Throws<ArgumentOutOfRangeException>(() => new MovementModel().Step(new MovementInput(0, false), true, 0f, C));

        [Test]
        public void Config_Default_IsValid_AndBadValuesAreReported()
        {
            Assert.IsNull(C.Validate());
            var bad = C;
            bad.MaxJumps = 0;
            StringAssert.Contains("MaxJumps", bad.Validate());
            bad = C;
            bad.Gravity = 0;
            StringAssert.Contains("Gravity", bad.Validate());
        }
    }
}
