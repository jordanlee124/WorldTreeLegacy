using System;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Worldtree.Presentation;

namespace Worldtree.Editor
{
    /// <summary>Idempotent project setup and Windows build, run via -executeMethod.</summary>
    public static class FoundationSetup
    {
        const string Scenes = "Assets/_Game/Scenes";
        const string Settings = "Assets/_Game/Settings";
        const string BootstrapScene = Scenes + "/Bootstrap.unity";
        const string SandboxScene = Scenes + "/Sandbox.unity";

        [MenuItem("Worldtree/Apply Foundation Setup")]
        public static void Apply()
        {
            Directory.CreateDirectory(Scenes);
            Directory.CreateDirectory(Settings);
            SetupUrp();
            SetProjectSettings();
            CreateBootstrap(BootstrapScene);
            CreateSandbox(SandboxScene);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootstrapScene, true),
                new EditorBuildSettingsScene(SandboxScene, true),
            };
            AssetDatabase.SaveAssets();
        }

        static void SetupUrp()
        {
            var rendererPath = Settings + "/URP_Renderer.asset";
            var assetPath = Settings + "/URP_Asset.asset";
            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath) == null)
            {
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<UniversalRendererData>(), rendererPath);
                var asset = UniversalRenderPipelineAsset.Create(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath));
                AssetDatabase.CreateAsset(asset, assetPath);
            }
            var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(assetPath);
            GraphicsSettings.defaultRenderPipeline = urp;
            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
            }
        }

        static void SetProjectSettings()
        {
            Set("ProjectSettings/ProjectSettings.asset", "activeInputHandler", p => p.intValue = 1); // Input System only; needs editor restart
            Set("ProjectSettings/TimeManager.asset", "Fixed Timestep", p => p.floatValue = 1f / 60f);
            Set("ProjectSettings/EditorSettings.asset", "m_SerializationMode", p => p.intValue = 2); // Force Text
        }

        static void Set(string path, string property, Action<SerializedProperty> set)
        {
            var obj = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(path)[0]);
            var prop = obj.FindProperty(property);
            if (prop == null) { Debug.LogError($"{path}: property '{property}' not found"); return; }
            set(prop);
            obj.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateBootstrap(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Bootstrap").AddComponent<Bootstrap>();
            EditorSceneManager.SaveScene(scene, path);
        }

        static void CreateSandbox(string path)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cam = new GameObject("Main Camera") { tag = "MainCamera" };
            cam.AddComponent<Camera>().orthographic = true;
            cam.AddComponent<CinemachineBrain>();
            cam.transform.position = new Vector3(0, 2, -10); // looks along +Z; gameplay plane is Z = 0

            var vcam = new GameObject("CM Camera").AddComponent<CinemachineCamera>();
            vcam.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            vcam.Lens.OrthographicSize = 5;
            vcam.transform.position = cam.transform.position;

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(20, 1, 2);
            ground.transform.position = new Vector3(0, -0.5f, 0);
            EditorSceneManager.SaveScene(scene, path);
        }

        public static void BuildWindows()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { BootstrapScene, SandboxScene },
                locationPathName = "Builds/Windows/WorldtreeLegacy.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
            });
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
