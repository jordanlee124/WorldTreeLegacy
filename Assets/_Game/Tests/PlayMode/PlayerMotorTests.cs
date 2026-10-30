using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Worldtree.Domain.Movement;
using Worldtree.Presentation;

namespace Worldtree.Tests
{
    public class PlayerMotorTests
    {
        static readonly MovementConfig C = MovementConfig.Default;
        const float SingleApex = 13f * 13f / (2f * 35f); // JumpVelocity^2 / 2g for the default config

        readonly List<GameObject> extras = new List<GameObject>(); // destroyed in TearDown so a failed assert cannot leak into the next test
        GameObject floor, wall, player;
        MovementConfigAsset config;
        PlayerMotor motor;
        Rigidbody body;
        float maxZDrift;

        [SetUp]
        public void SetUp()
        {
            floor = Block("Floor", new Vector3(0, -0.5f, 0), new Vector3(200, 1, 4));
            wall = Block("Wall", new Vector3(10, 5, 0), new Vector3(1, 10, 4));
            config = ScriptableObject.CreateInstance<MovementConfigAsset>();
            maxZDrift = 0;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in new[] { floor, wall, player }) if (go != null) Object.Destroy(go);
            foreach (var go in extras) if (go != null) Object.Destroy(go);
            extras.Clear();
            if (config != null) Object.Destroy(config);
        }

        static GameObject Block(string name, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            return go;
        }

        void SpawnPlayer(Vector3 pos)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.SetActive(false);
            player.transform.position = pos;
            body = player.AddComponent<Rigidbody>();
            motor = player.AddComponent<PlayerMotor>();
            motor.ConfigAsset = config;
            motor.FallResetY = -5f;
            player.SetActive(true);
        }

        /// <summary>Waits n fixed steps, recording the worst Z drift.</summary>
        IEnumerator Steps(int n)
        {
            for (var i = 0; i < n; i++)
            {
                yield return new WaitForFixedUpdate();
                maxZDrift = Mathf.Max(maxZDrift, Mathf.Abs(body.position.z), Mathf.Abs(body.linearVelocity.z));
            }
        }

        IEnumerator Settle()
        {
            yield return Steps(15);
            Assert.IsTrue(motor.Model.Grounded, "player should rest on the floor");
        }

        /// <summary>Waits n fixed steps and returns the highest Y reached through the out holder.</summary>
        IEnumerator Apex(int n, float[] peak)
        {
            for (var i = 0; i < n; i++)
            {
                yield return Steps(1);
                peak[0] = Mathf.Max(peak[0], body.position.y);
            }
        }

        [UnityTest]
        public IEnumerator Runs_ToMaxSpeed_OnThePlane()
        {
            SpawnPlayer(new Vector3(0, 1.01f, 0));
            yield return Settle();
            motor.SetMove(1f);
            yield return Steps(60);
            Assert.AreEqual(C.MaxRunSpeed, body.linearVelocity.x, 0.2f);
            Assert.Greater(body.position.x, 4f);
            Assert.AreEqual(MovementState.Run, motor.Model.State);
            motor.SetMove(0f);
            yield return Steps(30);
            Assert.AreEqual(0f, body.linearVelocity.x, 0.1f);
            Assert.Less(maxZDrift, 1e-4f);
        }

        [UnityTest]
        public IEnumerator SingleJump_ReachesExpectedHeight()
        {
            SpawnPlayer(new Vector3(0, 1.01f, 0));
            yield return Settle();
            var start = body.position.y;
            var peak = new[] { start };
            motor.QueueJump();
            yield return Apex(90, peak);
            Assert.AreEqual(SingleApex, peak[0] - start, SingleApex * 0.15f);
            Assert.IsTrue(motor.Model.Grounded, "landed again");
            Assert.AreEqual(0, motor.Model.JumpsUsed);
        }

        [UnityTest]
        public IEnumerator DoubleJump_GoesHigherThanSingle_AndThirdIsRejected()
        {
            SpawnPlayer(new Vector3(0, 1.01f, 0));
            yield return Settle();
            var start = body.position.y;
            var peak = new[] { start };
            motor.QueueJump();
            yield return Apex(18, peak);
            motor.QueueJump();
            yield return Apex(12, peak);
            Assert.AreEqual(2, motor.Model.JumpsUsed);

            var vyBefore = body.linearVelocity.y;
            motor.QueueJump(); // third
            yield return Apex(1, peak);
            Assert.AreEqual(2, motor.Model.JumpsUsed);
            Assert.Less(body.linearVelocity.y, vyBefore, "third jump must not launch");

            yield return Apex(120, peak);
            // Two jumps can reach at most about two single apexes; a working third would exceed this.
            Assert.Greater(peak[0] - start, SingleApex * 1.4f, "double jump should beat a single jump");
            Assert.Less(peak[0] - start, SingleApex * 2.15f, "no third jump");
            Assert.Less(maxZDrift, 1e-4f);
        }

        [UnityTest]
        public IEnumerator PressingIntoWall_WhileJumping_StaysOnPlane_AndBlocked()
        {
            SpawnPlayer(new Vector3(5, 1.01f, 0));
            yield return Settle();
            motor.SetMove(1f);
            var maxX = 0f;
            for (var i = 0; i < 240; i++)
            {
                if (i % 40 == 0) motor.QueueJump();
                yield return Steps(1);
                maxX = Mathf.Max(maxX, body.position.x);
            }
            // wall face at x = 9.5, capsule radius 0.5
            Assert.LessOrEqual(maxX, 9.06f, "wall must block the player");
            Assert.Greater(maxX, 8.9f, "player should reach the wall");
            Assert.Less(maxZDrift, 1e-4f);
        }

        [UnityTest]
        public IEnumerator SpawningOffThePlane_SnapsToPlane_AndFreezesZ()
        {
            SpawnPlayer(new Vector3(0, 1.01f, 0.7f));
            yield return Settle();
            Assert.AreEqual(0f, body.position.z, 1e-4f);
            Assert.IsTrue(body.constraints.HasFlag(RigidbodyConstraints.FreezePositionZ));
            Assert.IsTrue(body.constraints.HasFlag(RigidbodyConstraints.FreezeRotation));
        }

        GameObject OneWay(float topY)
        {
            var slab = Block("OneWay", new Vector3(0, topY - 0.25f, 0), new Vector3(6, 0.5f, 2));
            slab.AddComponent<OneWayPlatform>();
            extras.Add(slab);
            return slab;
        }

        [UnityTest]
        public IEnumerator OneWayPlatform_CanBeJumpedUpThrough_ThenStoodOn()
        {
            var slab = OneWay(1.8f);
            SpawnPlayer(new Vector3(0, 1.01f, 0)); // standing under it, head inside the slab
            yield return Settle();
            motor.QueueJump();
            yield return Steps(90);
            // Capsule centre rests half a height (1 m) above the slab top.
            Assert.AreEqual(1.8f + 1f, body.position.y, 0.05f, "should land on top of the platform");
            Assert.IsTrue(motor.Model.Grounded);
            yield return Steps(60);
            Assert.AreEqual(1.8f + 1f, body.position.y, 0.05f, "must not sink through while standing");
            Assert.Less(maxZDrift, 1e-4f);
        }

        [UnityTest]
        public IEnumerator OneWayPlatform_TooLowAJump_FallsBackThrough()
        {
            var slab = OneWay(2.8f); // feet apex is about 2.4 m, below the top
            SpawnPlayer(new Vector3(0, 1.01f, 0));
            yield return Settle();
            motor.QueueJump();
            yield return Steps(90);
            Assert.AreEqual(1f, body.position.y, 0.05f, "back on the floor");
        }

        [UnityTest]
        public IEnumerator OneWayPlatform_FallingOntoItFromAbove_Lands()
        {
            var slab = OneWay(1.8f);
            SpawnPlayer(new Vector3(0, 6f, 0));
            yield return Steps(90);
            Assert.AreEqual(1.8f + 1f, body.position.y, 0.05f);
        }

        [UnityTest]
        public IEnumerator SolidBlock_StillBlocksFromBelow()
        {
            var slab = Block("Solid", new Vector3(0, 3.05f, 0), new Vector3(6, 0.5f, 2)); // underside at 2.8
            extras.Add(slab);
            SpawnPlayer(new Vector3(0, 1.01f, 0));
            yield return Settle();
            var peak = new[] { body.position.y };
            motor.QueueJump();
            yield return Apex(60, peak);
            Assert.Less(peak[0], 1.9f, "head stops at the underside (centre <= 2.8 - 1); unobstructed it would reach 3.4");
        }

        [UnityTest]
        public IEnumerator FallingIntoThePit_ResetsToSpawn()
        {
            SpawnPlayer(new Vector3(150, 2f, 0)); // beyond the floor's end: nothing to land on
            var t = 0;
            while (motor.FallResets == 0 && t++ < 300) yield return Steps(1);
            Assert.AreEqual(1, motor.FallResets);
            Assert.AreEqual(150f, body.position.x, 0.05f);
            Assert.AreEqual(2f, body.position.y, 0.1f);
            Assert.AreEqual(0f, body.linearVelocity.y, 0.01f);
            Assert.AreEqual(0, motor.Model.JumpsUsed);
        }
    }
}
