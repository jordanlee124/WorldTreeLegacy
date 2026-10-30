using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Worldtree.Domain;
using Worldtree.Presentation;

namespace Worldtree.Tests
{
    public class BootstrapTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Do not leave the playable course's colliders/player in later physics tests.
            var loaded = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("BootstrapTestCleanup"));
            yield return SceneManager.UnloadSceneAsync(loaded);
        }

        [UnityTest]
        public IEnumerator Bootstrap_LoadsPlayableGraybox_AndSetsFixedStep()
        {
            SceneManager.LoadScene("Bootstrap");
            yield return null; // Bootstrap.Awake queues the playable scene load
            yield return null;
            Assert.AreEqual("Graybox", SceneManager.GetActiveScene().name);
            Assert.AreEqual(SimulationConfig.FixedStepSeconds, Time.fixedDeltaTime, 1e-6f);
            var cam = Camera.main;
            Assert.NotNull(cam);
            Assert.IsTrue(cam.orthographic);
            Assert.NotNull(Object.FindFirstObjectByType<PlayerMotor>(), "entry scene must contain the playable character");
        }
    }
}
