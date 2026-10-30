using System.Collections.Generic;
using System.IO;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using Worldtree.Presentation;

namespace Worldtree.Editor
{
    /// <summary>Idempotent generator for the GameCamera prefab and the Graybox scene. Run via -executeMethod.</summary>
    public static class GrayboxSetup
    {
        public const string PrefabPath = "Assets/_Game/Prefabs/GameCamera.prefab";
        public const string ScenePath = "Assets/_Game/Scenes/Graybox.unity";
        public const float OrthoSize = 6f;

        // World-space limits of the course; the camera view never leaves this rectangle.
        public static readonly Rect CameraLimits = new Rect(-6, -6, 157, 20);

        [MenuItem("Worldtree/Apply Graybox Setup")]
        public static void Apply()
        {
            MovementSetup.Apply(); // input actions and tuning asset the player needs
            CreateCameraPrefab();
            FoundationSetup.CreateIfMissing(ScenePath, CreateScene);

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == ScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            AssetDatabase.SaveAssets();
        }

        static void CreateCameraPrefab()
        {
            if (File.Exists(PrefabPath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));

            var root = new GameObject("GameCamera");

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(root.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = OrthoSize;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
            camGo.AddComponent<CinemachineBrain>().UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate; // measured less jitter than SmartUpdate with the interpolated rigidbody
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0, 0, -10); // looks along +Z; gameplay plane is Z = 0

            var vcamGo = new GameObject("CM Follow");
            vcamGo.transform.SetParent(root.transform, false);
            vcamGo.transform.position = new Vector3(0, 0, -10);
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            vcam.Lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            vcam.Lens.OrthographicSize = OrthoSize;

            var composer = vcamGo.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 10f;
            composer.Damping = new Vector3(0.35f, 0.25f, 0f); // x follows firmly, y more slowly so hops do not bob the view
            composer.Composition.DeadZone.Enabled = true;
            composer.Composition.DeadZone.Size = new Vector2(0.08f, 0.2f);
            composer.Composition.HardLimits.Enabled = true; // damping may lag, but the player never leaves this screen area
            composer.Composition.HardLimits.Size = new Vector2(0.7f, 0.7f);

            vcamGo.AddComponent<CameraBounds>().Bounds = CameraLimits;
            vcamGo.AddComponent<CameraWarpOnRespawn>();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>Platform test course on the Z = 0 plane. Distances assume the default movement config (single jump ~2.4 m and ~5.6 m, double jump ~4.8 m).</summary>
        static void CreateScene()
        {
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            Span("Ground A (flat run-up)", -5, 60, 0, 1);
            // 4 m pit: one jump clears it
            Span("Ground B", 64, 100, 0, 1);
            Span("Step 1", 70, 72, 1, 1);
            Span("Step 2", 72, 74, 2, 2);
            Span("Ledge A (one-way)", 78, 83, 2, 0.4f, true);
            Span("Ledge B (one-way)", 85, 90, 4, 0.4f, true);
            Span("Barrier (double jump)", 95, 96, 3, 3);
            // 8 m pit: needs the double jump, or the one-way ledge in the middle
            Span("Ledge Pit (one-way)", 103, 105, 2, 0.4f, true);
            Span("Ground C", 108, 150, 0, 1);
            Span("Wall Left", -6, -5, 8, 9);
            Span("Wall Right", 150, 151, 8, 9);

            var motor = MovementSetup.CreatePlayer(new Vector3(0, 1.01f, 0), -50f); // the zone below resets first; -50 is only a safety net
            new GameObject("Debug HUD").AddComponent<MovementDebugHud>().Motor = motor;

            var zone = new GameObject("Fall Reset Zone");
            zone.transform.position = new Vector3(72, -6, 0);
            var box = zone.AddComponent<BoxCollider>();
            box.size = new Vector3(200, 4, 4);
            box.isTrigger = true;
            zone.AddComponent<FallResetZone>();

            var rig = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var vcam = rig.GetComponentInChildren<CinemachineCamera>();
            vcam.Follow = motor.transform;
            PrefabUtility.RecordPrefabInstancePropertyModifications(vcam);
            var warp = rig.GetComponentInChildren<CameraWarpOnRespawn>();
            warp.Motor = motor;
            PrefabUtility.RecordPrefabInstancePropertyModifications(warp);
        }

        static void Span(string name, float x0, float x1, float topY, float height, bool oneWay = false) =>
            MovementSetup.Block(name, (x0 + x1) * 0.5f, topY, x1 - x0, height, oneWay);
    }
}
