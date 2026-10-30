using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Worldtree.Domain;

namespace Worldtree.Tests
{
    /// <summary>Checks the saved project settings, which apply before Bootstrap runs (e.g. when opening Sandbox directly).</summary>
    public class ProjectSettingsTests
    {
        static long Field(string yaml, string name) => long.Parse(Regex.Match(yaml, name + @": (\d+)").Groups[1].Value);

        [Test]
        public void SavedFixedTimestep_Is60Hz()
        {
            var yaml = File.ReadAllText("ProjectSettings/TimeManager.asset");
            var seconds = (double)Field(yaml, "m_Count") * Field(yaml, "m_Denominator") / Field(yaml, "m_Numerator");
            Assert.AreEqual(SimulationConfig.FixedStepSeconds, seconds, 1e-9);
        }

        [Test]
        public void LoadedFixedTimestep_Is60Hz() => Assert.AreEqual(SimulationConfig.FixedStepSeconds, Time.fixedDeltaTime, 1e-6f);

        [Test]
        public void Identity_IsSiregiWorldtreeLegacy()
        {
            Assert.AreEqual("Worldtree Legacy", PlayerSettings.productName);
            Assert.AreEqual("Siregi", PlayerSettings.companyName);
        }
    }
}
