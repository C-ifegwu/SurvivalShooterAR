using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SurvivalShooter.EditorTools
{
    /// <summary>Android build helpers (APK for submission, or Build &amp; Run on a USB device).</summary>
    public static class BuildTools
    {
        public const string ApkPath = "Builds/SurvivalShooterAR.apk";

        [MenuItem("Survival Shooter AR/Build Android APK", priority = 30)]
        public static void BuildApk() => Build(false);

        [MenuItem("Survival Shooter AR/Build And Run On Android Device", priority = 31)]
        public static void BuildAndRun() => Build(true);

        private static void Build(bool run)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory("Builds");
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = run ? BuildOptions.AutoRunPlayer : BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            string summary = $"{report.summary.result} | {report.summary.totalSize / (1024f * 1024f):F1} MB | {report.summary.totalTime} | errors {report.summary.totalErrors}";
            File.WriteAllText("Logs/ssar_build.log", summary + "\n");
            Debug.Log("[SSAR Build] " + summary);
        }
    }
}
