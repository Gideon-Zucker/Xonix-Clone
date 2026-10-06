#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace AirXonix.EditorTools
{
    /// <summary>
    /// On first import, creates Assets/AirXonix/Scenes/Game.unity (a camera + a
    /// GameManager), adds it to Build Settings and applies mobile-friendly player
    /// settings. Everything else the game needs is created at runtime.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneBuilder
    {
        const string SceneDir = "Assets/AirXonix/Scenes";
        const string ScenePath = SceneDir + "/Game.unity";
        const string BundleId = "com.airxonix.game";

        static SceneBuilder()
        {
            EditorApplication.delayCall += EnsureSetup;
        }

        [MenuItem("AirXonix/Rebuild Game Scene")]
        public static void Rebuild() => CreateScene();

        [MenuItem("AirXonix/Apply Mobile Player Settings")]
        public static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "AirXonix";
            PlayerSettings.productName = "AirXonix";

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            TrySet(() =>
            {
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, BundleId);
                PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, BundleId);
            });
            TrySet(() => PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24);
            TrySet(() => PlayerSettings.iOS.targetOSVersionString = "13.0");
            TrySet(() => PlayerSettings.iOS.appleEnableAutomaticSigning = true);

            EnsureLegacyInput();
            Debug.Log("[AirXonix] Applied mobile player settings.");
        }

        static void EnsureSetup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;

            if (!File.Exists(ScenePath))
                CreateScene();
            else
                EnsureInBuildSettings();
        }

        static void CreateScene()
        {
            try
            {
                Directory.CreateDirectory(SceneDir);
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.02f, 0.05f, 0.12f);
                cam.nearClipPlane = 0.3f;
                cam.farClipPlane = 300f;
                camGo.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
                camGo.transform.position = new Vector3(0f, 40f, -40f);
                camGo.AddComponent<AudioListener>();

                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                light.shadows = LightShadows.Soft;
                light.intensity = 1.1f;

                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.18f, 0.18f, 0.22f);

                new GameObject("AirXonix").AddComponent<GameManager>();

                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
                EnsureInBuildSettings();
                ApplyPlayerSettings();

                Debug.Log("[AirXonix] Created " + ScenePath + " — open it and press Play.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[AirXonix] Could not create the game scene automatically: " + e.Message +
                               "\nUse the menu  AirXonix > Rebuild Game Scene.");
            }
        }

        static void EnsureInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == ScenePath)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        static void EnsureLegacyInput()
        {
            // Make sure the classic Input Manager is available (value 2 == "Both").
            TrySet(() =>
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
                if (assets == null || assets.Length == 0) return;
                var so = new SerializedObject(assets[0]);
                var prop = so.FindProperty("activeInputHandler");
                if (prop != null && prop.intValue == 1) // "Input System Package (New)" only
                {
                    prop.intValue = 2;
                    so.ApplyModifiedProperties();
                    Debug.LogWarning("[AirXonix] Set Active Input Handling to 'Both'. Restart the editor if input feels dead.");
                }
            });
        }

        static void TrySet(System.Action a)
        {
            try { a(); } catch { /* version differences — safe to ignore */ }
        }
    }
}
#endif
