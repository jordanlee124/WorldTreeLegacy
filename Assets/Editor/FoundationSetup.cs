using System;
using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
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
            Directory.CreateDirectory(Settings);
            SetupUrp();
            SetProjectSettings();
            EnsureScenes(BootstrapScene, SandboxScene);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Creates the foundation scenes only if missing and appends them to the build list only if absent.</summary>
        public static void EnsureScenes(string bootstrapPath, string sandboxPath)
        {
            CreateIfMissing(bootstrapPath, CreateBootstrap);
            CreateIfMissing(sandboxPath, CreateSandbox);

            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var path in new[] { bootstrapPath, sandboxPath })
                if (!list.Exists(s => string.Equals(s.path, path, StringComparison.OrdinalIgnoreCase)))
                    list.Add(new EditorBuildSettingsScene(path, true));
            if (list.Count != EditorBuildSettings.scenes.Length)
                EditorBuildSettings.scenes = list.ToArray();
        }

        static void CreateIfMissing(string path, Action build)
        {
            if (File.Exists(path)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // NewScene(Single) would drop open scenes; Unity won't add a scene beside an untitled one, so refuse rather than lose work.
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var open = SceneManager.GetSceneAt(i);
                if (open.isDirty)
                    throw new InvalidOperationException($"Foundation setup aborted: scene '{(open.name == "" ? "Untitled" : open.name)}' has unsaved changes. Save or close it, then run setup again.");
            }
            var openScenes = EditorSceneManager.GetSceneManagerSetup();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            build();
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.RestoreSceneManagerSetup(openScenes);
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
            // Fixed Timestep is a rational (ticks / ticks-per-second), not a float: 141120000 ticks/s divides 1/60 s exactly.
            Set("ProjectSettings/TimeManager.asset", "Fixed Timestep.m_Count", p => p.longValue = 141120000L / 60);
            Set("ProjectSettings/TimeManager.asset", "Fixed Timestep.m_Rate.m_Numerator", p => p.longValue = 141120000L);
            Set("ProjectSettings/TimeManager.asset", "Fixed Timestep.m_Rate.m_Denominator", p => p.longValue = 1);
            PlayerSettings.productName = "Worldtree Legacy";
            PlayerSettings.companyName = "Siregi"; // chosen by the project owner
            Set("ProjectSettings/EditorSettings.asset", "m_SerializationMode", p => p.intValue = 2); // Force Text
        }

        static void Set(string path, string property, Action<SerializedProperty> set)
        {
            var obj = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath(path)[0]);
            var prop = obj.FindProperty(property);
            if (prop == null) { Debug.LogError($"{path}: property '{property}' not found"); return; }
            set(prop);
            obj.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(obj.targetObject);
        }

        static void CreateBootstrap() => new GameObject("Bootstrap").AddComponent<Bootstrap>();

        static void CreateSandbox()
        {
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
