using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Worldtree.Presentation;

namespace Worldtree.Editor
{
    /// <summary>Idempotent generator for the movement Input Actions, config asset and MovementTest scene. Run via -executeMethod.</summary>
    public static class MovementSetup
    {
        public const string InputPath = "Assets/_Game/Settings/Player.inputactions";
        public const string ConfigPath = "Assets/_Game/Data/PlayerMovement.asset";
        public const string ScenePath = "Assets/_Game/Scenes/MovementTest.unity";

        [MenuItem("Worldtree/Apply Movement Setup")]
        public static void Apply()
        {
            CreateInputActions();
            CreateConfig();
            AssetDatabase.Refresh();
            FoundationSetup.CreateIfMissing(ScenePath, CreateScene);

            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == ScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true)); // PlayMode tests load it by name
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            AssetDatabase.SaveAssets();
        }

        static void CreateInputActions()
        {
            if (File.Exists(InputPath)) return;
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = asset.AddActionMap("Gameplay");

            var move = map.AddAction("Move", InputActionType.Value, expectedControlLayout: "Axis");
            move.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            move.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick/x");

            var jump = map.AddAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            Directory.CreateDirectory(Path.GetDirectoryName(InputPath));
            File.WriteAllText(InputPath, asset.ToJson());
            Object.DestroyImmediate(asset);
        }

        static void CreateConfig()
        {
            if (File.Exists(ConfigPath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<MovementConfigAsset>(), ConfigPath);
        }

        /// <summary>Flat course on the Z = 0 plane: ground, pit, wall, a single-jump ledge, a double-jump ledge and an unreachable ledge.</summary>
        static void CreateScene()
        {
            var cam = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cam.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8;
            cam.transform.position = new Vector3(3, 4, -10);

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            // (name, center x, top y, width, height) - all 2 deep, centered on Z = 0.
            Block("Ground Left", -4.5f, 0f, 15f, 1f);
            Block("Ground Right", 12f, 0f, 12f, 1f);
            Block("Wall", 15f, 3f, 1f, 3f);
            Block("Ledge Single Jump", -6f, 1.5f, 3f, 0.5f);
            Block("Ledge Double Jump", -1.5f, 3.8f, 3f, 0.5f);
            Block("Ledge Unreachable", 10.5f, 6.5f, 3f, 0.5f);

            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.SetActive(false); // so PlayerMotor.Awake sees its fields
            player.transform.position = new Vector3(-10.5f, 1.01f, 0);
            player.AddComponent<Rigidbody>();
            var motor = player.AddComponent<PlayerMotor>();
            motor.ConfigAsset = AssetDatabase.LoadAssetAtPath<MovementConfigAsset>(ConfigPath);
            motor.InputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            motor.FallResetY = -8f;
            player.SetActive(true);

            new GameObject("Debug HUD").AddComponent<MovementDebugHud>().Motor = motor;
        }

        static void Block(string name, float centerX, float topY, float width, float height)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.localScale = new Vector3(width, height, 2);
            go.transform.position = new Vector3(centerX, topY - height / 2f, 0);
        }
    }
}
