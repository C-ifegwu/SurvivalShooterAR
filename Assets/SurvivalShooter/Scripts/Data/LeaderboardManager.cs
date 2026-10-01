using System;
using System.Collections.Generic;
using UnityEngine;
using SurvivalShooter.Core;

namespace SurvivalShooter.Data
{
    [Serializable]
    public class SessionRecord
    {
        public int score;
        public int kills;
        public float timeSurvived;
        public string difficulty;
        public bool survived;
        public long timestampTicks;

        public DateTime Timestamp => new DateTime(timestampTicks);
    }

    [Serializable]
    internal class LeaderboardSave
    {
        public int bestScore;
        public List<SessionRecord> sessions = new List<SessionRecord>();
    }

    /// <summary>
    /// Local, persistent leaderboard. Stores the latest 5 sessions (newest first) as JSON in
    /// PlayerPrefs, so data survives app restarts. Also tracks the all-time best score.
    /// </summary>
    [DefaultExecutionOrder(-95)]
    public class LeaderboardManager : Singleton<LeaderboardManager>
    {
        public const int MaxSessions = 5;
        private const string PrefsKey = "SSAR_LEADERBOARD_V2";

        private LeaderboardSave data;

        public IReadOnlyList<SessionRecord> Sessions => data.sessions;
        public int BestScore => data.bestScore;

        protected override void OnSingletonAwake() => Load();

        public SessionRecord Record(GameSession session, out bool isNewBest)
        {
            var record = new SessionRecord
            {
                score = session.Score,
                kills = session.Kills,
                timeSurvived = session.TimeSurvived,
                difficulty = session.Config.displayName,
                survived = session.Result == GameResult.Survived,
                timestampTicks = DateTime.Now.Ticks
            };

            data.sessions.Insert(0, record);
            if (data.sessions.Count > MaxSessions)
                data.sessions.RemoveRange(MaxSessions, data.sessions.Count - MaxSessions);

            isNewBest = record.score > data.bestScore && record.score > 0;
            if (isNewBest) data.bestScore = record.score;

            Save();
            return record;
        }

        public void Clear()
        {
            data = new LeaderboardSave();
            Save();
        }

        private void Load()
        {
            data = null;
            if (PlayerPrefs.HasKey(PrefsKey))
            {
                try { data = JsonUtility.FromJson<LeaderboardSave>(PlayerPrefs.GetString(PrefsKey)); }
                catch (Exception e) { Debug.LogWarning($"[Leaderboard] Corrupt save reset: {e.Message}"); }
            }
            if (data == null) data = new LeaderboardSave();
            if (data.sessions == null) data.sessions = new List<SessionRecord>();
        }

        private void Save()
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
