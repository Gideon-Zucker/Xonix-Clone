#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AirXonix.EditorTools
{
    /// <summary>One-click / CI builds. Output goes to &lt;project&gt;/Builds.</summary>
    public static class BuildScript
    {
        static string[] Scenes() =>
            EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        [MenuItem("AirXonix/Build/Android (.apk)")]
        public static void BuildAndroid()
        {
            Run(BuildTarget.Android, "Builds/Android/AirXonix.apk");
        }

        [MenuItem("AirXonix/Build/iOS (Xcode project)")]
        public static void BuildIOS()
        {
            Run(BuildTarget.iOS, "Builds/iOS");
        }

        static void Run(BuildTarget target, string outPath)
        {
            var scenes = Scenes();
            if (scenes.Length == 0)
            {
                Debug.LogError("[AirXonix] No scenes in Build Settings. Run  AirXonix > Rebuild Game Scene.");
                return;
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outPath,
                target = target,
                options = BuildOptions.None
            });

            Debug.Log($"[AirXonix] {target} build: {report.summary.result} ({report.summary.totalSize} bytes) -> {outPath}");
        }

        // Callable head-less:  -batchmode -quit -executeMethod AirXonix.EditorTools.BuildScript.CI_Android
        public static void CI_Android() => BuildAndroid();
        public static void CI_iOS() => BuildIOS();
    }
}
#endif
