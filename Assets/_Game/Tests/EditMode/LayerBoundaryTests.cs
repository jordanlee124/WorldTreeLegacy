using System;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Worldtree.Tests
{
    /// <summary>
    /// Guards the dependency direction from ARCHITECTURE.md by reading the assembly definitions:
    /// Domain and Application are engine-free; nothing below Presentation references a layer above it.
    /// </summary>
    public class LayerBoundaryTests
    {
        [Serializable]
        sealed class Asmdef
        {
            public string name;
            public string[] references;
            public bool noEngineReferences;
        }

        static Asmdef Read(string layer) =>
            JsonUtility.FromJson<Asmdef>(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "_Game/Scripts", layer, "Worldtree." + layer + ".asmdef")));

        [Test]
        public void Domain_HasNoReferences_AndNoEngine()
        {
            var a = Read("Domain");
            Assert.IsEmpty(a.references);
            Assert.IsTrue(a.noEngineReferences);
        }

        [Test]
        public void Application_ReferencesOnlyDomain_AndNoEngine()
        {
            var a = Read("Application");
            CollectionAssert.AreEquivalent(new[] { "Worldtree.Domain" }, a.references);
            Assert.IsTrue(a.noEngineReferences);
        }

        [Test]
        public void Infrastructure_ReferencesOnlyDomain() =>
            CollectionAssert.AreEquivalent(new[] { "Worldtree.Domain" }, Read("Infrastructure").references);
    }
}
