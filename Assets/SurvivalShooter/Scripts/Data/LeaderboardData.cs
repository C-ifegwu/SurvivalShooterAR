using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalShooter.Data
{
    [Serializable]
    public class ScoreEntry
    {
        public int score;
        public int enemiesDefeated;
        public float timeSurvived;
        public string difficulty;
        public string dateString;

        public ScoreEntry() { }

        public ScoreEntry(int score, int enemiesDefeated, float timeSurvived, string difficulty)
        {
            this.score = score;
            this.enemiesDefeated = enemiesDefeated;
            this.timeSurvived = timeSurvived;
            this.difficulty = difficulty;
            this.dateString = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        }
    }

    [Serializable]
    public class LeaderboardSaveData
    {
        public List<ScoreEntry> entries = new List<ScoreEntry>();
    }

    /// <summary>
    /// Singleton manager for persistent local leaderboard storage.
    /// Retains strictly the latest 5 sessions as required by technical specification.
    /// </summary>
    public class LeaderboardManager : MonoBehaviour
    {
        private const string PREF_KEY = "SURVIVAL_SHOOTER_LEADERBOARD";
        private const int MAX_ENTRIES = 5;

        public static LeaderboardManager Instance { get; private set; }

        private LeaderboardSaveData currentData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
        }

        public void SaveSession(int score, int enemiesDefeated, float timeSurvived, string difficulty)
        {
            if (currentData == null)
            {
                currentData = new LeaderboardSaveData();
            }

            ScoreEntry entry = new ScoreEntry(score, enemiesDefeated, timeSurvived, difficulty);
            currentData.entries.Insert(0, entry); // Insert latest session at top

            // Clamp to latest 5 sessions
            while (currentData.entries.Count > MAX_ENTRIES)
            {
                currentData.entries.RemoveAt(currentData.entries.Count - 1);
            }

            string json = JsonUtility.ToJson(currentData, true);
            PlayerPrefs.SetString(PREF_KEY, json);
            PlayerPrefs.Save();
            Debug.Log($"[LeaderboardManager] Saved session: Score={score}, Time={timeSurvived:F1}s, Enemies={enemiesDefeated}");
        }

        public List<ScoreEntry> GetLatestSessions()
        {
            if (currentData == null)
            {
                LoadData();
            }
            return new List<ScoreEntry>(currentData.entries);
        }

        private void LoadData()
        {
            if (PlayerPrefs.HasKey(PREF_KEY))
            {
                try
                {
                    string json = PlayerPrefs.GetString(PREF_KEY);
                    currentData = JsonUtility.FromJson<LeaderboardSaveData>(json);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[LeaderboardManager] Failed to load data, resetting: {e.Message}");
                    currentData = new LeaderboardSaveData();
                }
            }
            else
            {
                currentData = new LeaderboardSaveData();
            }
        }

        public void ClearLeaderboard()
        {
            PlayerPrefs.DeleteKey(PREF_KEY);
            PlayerPrefs.Save();
            currentData = new LeaderboardSaveData();
        }
    }
}
