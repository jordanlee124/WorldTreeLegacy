using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Worldtree.Domain;

namespace Worldtree.Tests
{
    public class BootstrapTests
    {
        [UnityTest]
        public IEnumerator Bootstrap_LoadsSandbox_AndSetsFixedStep()
        {
            SceneManager.LoadScene("Bootstrap");
            yield return null; // Bootstrap.Awake queues the Sandbox load
            yield return null;
            Assert.AreEqual("Sandbox", SceneManager.GetActiveScene().name);
            Assert.AreEqual(SimulationConfig.FixedStepSeconds, Time.fixedDeltaTime, 1e-6f);
            var cam = Camera.main;
            Assert.NotNull(cam);
            Assert.IsTrue(cam.orthographic);
        }
    }
}
