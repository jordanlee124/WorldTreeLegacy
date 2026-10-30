using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Worldtree.Editor;

namespace Worldtree.Tests
{
    public class FoundationSetupTests
    {
        const string Dir = "Assets/_FoundationSetupTestTemp";
        const string Bootstrap = Dir + "/Bootstrap.unity";
        const string Sandbox = Dir + "/Sandbox.unity";
        const string Extra = Dir + "/Extra.unity";

        EditorBuildSettingsScene[] savedBuild;

        [SetUp]
        public void SetUp()
        {
            savedBuild = EditorBuildSettings.scenes;
            AssetDatabase.DeleteAsset(Dir);
            AssetDatabase.CreateFolder("Assets", "_FoundationSetupTestTemp");
            // A saved, clean active scene lets the helpers open scenes additively.
            EditorSceneManager.SaveScene(EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single), Dir + "/Anchor.unity");
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); // release the anchor before deleting it
            EditorBuildSettings.scenes = savedBuild;
            AssetDatabase.DeleteAsset(Dir);
        }

        static void SaveSceneWithSentinel(string path, string name)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.MoveGameObjectToScene(new GameObject(name), scene);
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.CloseScene(scene, true);
        }

        static bool SceneHasObject(string path, string name)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            var found = scene.GetRootGameObjects().Any(g => g.name == name);
            EditorSceneManager.CloseScene(scene, true);
            return found;
        }

        static int CountInBuild(string path) => EditorBuildSettings.scenes.Count(s => s.path == path);

        [Test]
        public void ExistingScene_KeepsContents()
        {
            SaveSceneWithSentinel(Sandbox, "Sentinel");
            var before = System.IO.File.ReadAllText(Sandbox);

            FoundationSetup.EnsureScenes(Bootstrap, Sandbox);

            Assert.AreEqual(before, System.IO.File.ReadAllText(Sandbox));
            Assert.IsTrue(SceneHasObject(Sandbox, "Sentinel"));
        }

        [Test]
        public void MissingScenes_AreCreated()
        {
            FoundationSetup.EnsureScenes(Bootstrap, Sandbox);

            Assert.IsTrue(SceneHasObject(Bootstrap, "Bootstrap"));
            Assert.IsTrue(SceneHasObject(Sandbox, "Main Camera"));
        }

        [Test]
        public void BuildList_PreservesExistingEntriesOrderAndState()
        {
            SaveSceneWithSentinel(Extra, "Extra");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(Extra, false),
                new EditorBuildSettingsScene(Bootstrap, false), // present but disabled: must stay disabled
            };

            FoundationSetup.EnsureScenes(Bootstrap, Sandbox);

            var scenes = EditorBuildSettings.scenes;
            Assert.AreEqual(new[] { Extra, Bootstrap, Sandbox }, scenes.Select(s => s.path).ToArray());
            Assert.AreEqual(new[] { false, false, true }, scenes.Select(s => s.enabled).ToArray());
        }

        [Test]
        public void RepeatedRun_CreatesNoDuplicatesOrChanges()
        {
            FoundationSetup.EnsureScenes(Bootstrap, Sandbox);
            var sandbox = System.IO.File.ReadAllText(Sandbox);
            var build = EditorBuildSettings.scenes.Select(s => s.path).ToArray();

            FoundationSetup.EnsureScenes(Bootstrap, Sandbox);

            Assert.AreEqual(1, CountInBuild(Bootstrap));
            Assert.AreEqual(1, CountInBuild(Sandbox));
            Assert.AreEqual(build, EditorBuildSettings.scenes.Select(s => s.path).ToArray());
            Assert.AreEqual(sandbox, System.IO.File.ReadAllText(Sandbox));
        }

        [Test]
        public void OpenUnsavedScene_IsNotDiscarded()
        {
            var active = SceneManager.GetActiveScene();
            var unsaved = new GameObject("UnsavedWork");
            EditorSceneManager.MarkSceneDirty(active);
            try
            {
                Assert.Throws<System.InvalidOperationException>(() => FoundationSetup.EnsureScenes(Bootstrap, Sandbox));

                Assert.IsTrue(unsaved != null, "unsaved object was destroyed");
                Assert.AreEqual(active, SceneManager.GetActiveScene());
                Assert.IsTrue(active.isDirty);
                Assert.IsFalse(System.IO.File.Exists(Bootstrap));
                Assert.AreEqual(0, CountInBuild(Bootstrap));
            }
            finally
            {
                Object.DestroyImmediate(unsaved);
            }
        }
    }
}
