using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Worldtree.Presentation;

namespace Worldtree.Tests
{
    /// <summary>Runs against the shipped MovementTest scene, loaded additively so it never mixes with other tests' objects.</summary>
    public class MovementSceneTests
    {
        PlayerMotor motor;
        Rigidbody body;
        Keyboard keyboard;
        InputSettings.BackgroundBehavior? savedBackground;
#if UNITY_EDITOR
        InputSettings.EditorInputBehaviorInPlayMode? savedBehavior;
#endif

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return SceneManager.LoadSceneAsync("MovementTest", LoadSceneMode.Additive);
            motor = Object.FindFirstObjectByType<PlayerMotor>();
            Assert.NotNull(motor, "MovementTest scene has a PlayerMotor");
            body = motor.GetComponent<Rigidbody>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (savedBackground.HasValue) InputSystem.settings.backgroundBehavior = savedBackground.Value;
#if UNITY_EDITOR
            if (savedBehavior.HasValue) InputSystem.settings.editorInputBehaviorInPlayMode = savedBehavior.Value;
#endif
            yield return SceneManager.UnloadSceneAsync("MovementTest");
        }

        static IEnumerator Steps(int n) { for (var i = 0; i < n; i++) yield return new WaitForFixedUpdate(); }

        [UnityTest]
        public IEnumerator Scene_IsWired_AndPlayerRestsOnThePlane()
        {
            Assert.NotNull(motor.ConfigAsset);
            Assert.NotNull(motor.InputActions);
            Assert.NotNull(motor.InputActions.FindAction("Gameplay/Move"));
            Assert.NotNull(motor.InputActions.FindAction("Gameplay/Jump"));
            Assert.IsTrue(Camera.main.orthographic);
            yield return Steps(60);
            Assert.IsTrue(motor.Model.Grounded);
            Assert.AreEqual(0, motor.FallResets);
            Assert.AreEqual(0f, body.position.z, 1e-4f);
        }

        [UnityTest]
        public IEnumerator RealInputActions_KeyboardMovesAndJumps()
        {
#if UNITY_EDITOR
            // Without this and IgnoreFocus below, input is dropped while the Game view is unfocused (always the case in batch mode).
            savedBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            savedBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return Steps(60);
            var x0 = body.position.x;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            yield return Steps(30);
            Assert.Greater(body.position.x, x0 + 1f, "D should run right");

            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.Space));
            yield return Steps(3);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            yield return Steps(10);
            Assert.Greater(motor.Model.JumpsUsed, 0, "Space should jump");
            Assert.IsFalse(motor.Model.Grounded);
            Assert.AreEqual(0f, body.position.z, 1e-4f);
        }
    }
}
