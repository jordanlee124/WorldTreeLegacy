using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Worldtree.Presentation;

namespace Worldtree.Tests
{
    /// <summary>Runs against the shipped Graybox scene (loaded additively) with a virtual keyboard through the real Input Actions.</summary>
    public class GrayboxSceneTests
    {
        PlayerMotor motor;
        Rigidbody body;
        Camera cam;
        CameraBounds bounds;
        Keyboard keyboard;
        InputSettings.BackgroundBehavior? savedBackground;
        int savedFrameRate, savedVSync;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode? savedEditorBehavior;
#endif

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("Graybox", LoadSceneMode.Additive);
            motor = Object.FindFirstObjectByType<PlayerMotor>();
            Assert.NotNull(motor, "Graybox scene has a PlayerMotor");
            body = motor.GetComponent<Rigidbody>();
            // An earlier test (Bootstrap) leaves the Sandbox scene loaded with its own camera and brain; ignore it.
            foreach (var brain in Object.FindObjectsByType<CinemachineBrain>(FindObjectsSortMode.None))
                if (brain.gameObject.scene.name != "Graybox") brain.gameObject.SetActive(false);
            cam = System.Array.Find(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None), c => c.gameObject.scene.name == "Graybox");
            Assert.NotNull(cam, "Graybox scene has a camera");
            bounds = Object.FindFirstObjectByType<CameraBounds>();

#if UNITY_EDITOR
            // Without these two overrides input is dropped while the Game view is unfocused (always the case in batch mode).
            savedEditorBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            savedBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard = InputSystem.AddDevice<Keyboard>();
            savedFrameRate = Application.targetFrameRate;
            savedVSync = QualitySettings.vSyncCount;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Application.targetFrameRate = savedFrameRate;
            QualitySettings.vSyncCount = savedVSync;
            InputSystem.RemoveDevice(keyboard);
            if (savedBackground.HasValue) InputSystem.settings.backgroundBehavior = savedBackground.Value;
#if UNITY_EDITOR
            if (savedEditorBehavior.HasValue) InputSystem.settings.editorInputBehaviorInPlayMode = savedEditorBehavior.Value;
#endif
            yield return SceneManager.UnloadSceneAsync("Graybox");
        }

        static IEnumerator Steps(int n) { for (var i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        void AssertPlayerVisible(string when)
        {
            var v = cam.WorldToViewportPoint(motor.transform.position);
            Assert.That(v.x, Is.InRange(0.05f, 0.95f), $"player x on screen ({when})");
            Assert.That(v.y, Is.InRange(0.05f, 0.95f), $"player y on screen ({when})");
        }

        void AssertViewInsideBounds(string when)
        {
            var halfH = cam.orthographicSize;
            var halfW = halfH * cam.aspect;
            var p = cam.transform.position;
            Assert.GreaterOrEqual(p.x - halfW, bounds.Bounds.xMin - 0.01f, $"view left edge ({when})");
            Assert.LessOrEqual(p.x + halfW, bounds.Bounds.xMax + 0.01f, $"view right edge ({when})");
            Assert.GreaterOrEqual(p.y - halfH, bounds.Bounds.yMin - 0.01f, $"view bottom edge ({when})");
            Assert.LessOrEqual(p.y + halfH, bounds.Bounds.yMax + 0.01f, $"view top edge ({when})");
        }

        [UnityTest]
        public IEnumerator Scene_IsWired_AndPlayerStartsVisible()
        {
            yield return Steps(60);
            Assert.IsTrue(cam.orthographic);
            Assert.AreEqual(-10f, cam.transform.position.z, 1e-3f, "camera stays on its Z so it looks along +Z at the plane");
            Assert.NotNull(cam.GetComponent<CinemachineBrain>());
            var vcam = Object.FindFirstObjectByType<CinemachineCamera>();
            Assert.AreSame(motor.transform, vcam.Follow);
            Assert.NotNull(vcam.GetComponent<CinemachinePositionComposer>());
            Assert.AreEqual(3, Object.FindObjectsByType<OneWayPlatform>(FindObjectsSortMode.None).Length);
            var zone = Object.FindFirstObjectByType<FallResetZone>();
            Assert.NotNull(zone);
            Assert.IsTrue(zone.GetComponent<Collider>().isTrigger);
            Assert.IsTrue(motor.Model.Grounded);
            AssertPlayerVisible("start");
            AssertViewInsideBounds("start");
        }

        /// <summary>
        /// Holds D until well past the camera's damping time, then samples camera-to-player offset every rendered frame.
        /// At constant speed that offset is constant, so its spread is the visible jitter.
        /// </summary>
        [UnityTest]
        public IEnumerator RunningRight_CameraHasNoJitter_AndPlayerStaysVisible([Values(144, 30)] int targetFps)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFps;
            yield return Steps(30);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));

            var samples = new List<float>();
            var t0 = Time.realtimeSinceStartup;
            var frames = 0;
            while (Time.realtimeSinceStartup - t0 < 6f)
            {
                yield return null; // resumes after the previous frame's LateUpdate, so camera and interpolated player are from the same frame
                frames++;
                var elapsed = Time.realtimeSinceStartup - t0;
                AssertPlayerVisible($"t={elapsed:0.0}s");
                AssertViewInsideBounds($"t={elapsed:0.0}s");
                if (elapsed > 4f) samples.Add(cam.transform.position.x - motor.transform.position.x);
            }
            Assert.AreEqual(0, motor.FallResets, "stayed on the flat run-up");
            Assert.Greater(body.linearVelocity.x, 7f, "still running at full speed");
            Assert.Greater(samples.Count, 10);
            var spread = Mathf.Max(samples.ToArray()) - Mathf.Min(samples.ToArray());
            Debug.Log($"[jitter] target={targetFps} frames={frames} avgFps={frames / 6f:0} samples={samples.Count} offsetSpread={spread * 1000f:0.0} mm");
            Assert.Less(spread, 0.05f, "camera-to-player offset must stay constant at constant speed");
        }

        [UnityTest]
        public IEnumerator Barrier_BlocksPlayer_OnThePlane_WithCameraFollowing()
        {
            body.position = new Vector3(90, 1.01f, 0); // ground B, 5 m before the barrier face at x = 95
            yield return Steps(30);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            var maxX = 0f;
            var maxZ = 0f;
            for (var i = 0; i < 240; i++)
            {
                yield return Steps(1);
                maxX = Mathf.Max(maxX, body.position.x);
                maxZ = Mathf.Max(maxZ, Mathf.Abs(body.position.z));
            }
            Assert.LessOrEqual(maxX, 94.7f, "barrier face at 95, capsule radius 0.5, plus at most one step (0.13 m) of impact penetration");
            Assert.Greater(maxX, 94.4f, "player reached the barrier");
            Assert.Less(maxZ, 1e-4f);
            Assert.AreEqual(0, motor.FallResets);
            AssertPlayerVisible("against the barrier");
        }

        [UnityTest]
        public IEnumerator FallingIntoThePit_ResetsToSpawn_AndCameraFollowsWithoutGliding()
        {
            body.position = new Vector3(62, 3f, 0); // above the 4 m pit
            var t0 = Time.time;
            while (motor.FallResets == 0 && Time.time - t0 < 5f) yield return null;
            Assert.AreEqual(1, motor.FallResets, "fall reset zone fired");
            // Only a few frames: a warp puts the camera back at once, a glide would take about a second over 60 m.
            yield return Steps(3); // let the interpolated transform catch up with the teleport
            for (var i = 0; i < 5; i++) yield return null;
            Assert.AreEqual(0f, body.position.x, 0.5f, "respawned at the spawn point");
            AssertPlayerVisible("after reset");
            AssertViewInsideBounds("after reset");
        }
    }
}
