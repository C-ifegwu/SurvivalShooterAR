using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SurvivalShooter.DebugTools;

namespace SurvivalShooter.EditorTools
{
    /// <summary>Menu entry that opens the game scene, enters Play Mode and runs AutoPlayTest.</summary>
    [InitializeOnLoad]
    public static class PlayTestLauncher
    {
        private const string Flag = "SSAR_RunPlayTest";

        static PlayTestLauncher()
        {
            EditorApplication.playModeStateChanged -= OnPlayMode;
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        [MenuItem("Survival Shooter AR/Run Automated Play Test", priority = 20)]
        public static void Run()
        {
            if (EditorApplication.isPlaying) return;
            if (File.Exists(AutoPlayTest.ReportPath)) File.Delete(AutoPlayTest.ReportPath);
            if (EditorSceneManager.GetActiveScene().path != BuilderUtil.ScenePath)
                EditorSceneManager.OpenScene(BuilderUtil.ScenePath);
            SessionState.SetBool(Flag, true);
            EditorApplication.isPlaying = true;
        }

        [MenuItem("Survival Shooter AR/Open Game Scene", priority = 2)]
        public static void OpenScene()
        {
            EditorSceneManager.OpenScene(BuilderUtil.ScenePath);
        }

        [MenuItem("Survival Shooter AR/Clear Leaderboard Data", priority = 40)]
        public static void ClearPrefs()
        {
            PlayerPrefs.DeleteKey("SSAR_LEADERBOARD_V2");
            PlayerPrefs.Save();
            Debug.Log("[SSAR] Leaderboard cleared");
        }

        private static void OnPlayMode(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Flag, false)) return;
            SessionState.SetBool(Flag, false);
            new GameObject("[AutoPlayTest]").AddComponent<AutoPlayTest>();
        }
    }
}
