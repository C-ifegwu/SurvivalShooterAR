using System;
using UnityEngine;

namespace SurvivalShooter.Core
{
    /// <summary>
    /// Observer pattern hub. Gameplay systems raise events; UI, audio and the leaderboard subscribe.
    /// No system holds a hard reference to the UI, which keeps the architecture decoupled.
    /// </summary>
    public static class GameEvents
    {
        // Flow
        public static event Action<GameStateId, GameStateId> StateChanged;   // previous, next
        public static event Action<int> CountdownTick;                       // 3,2,1,0(=GO)
        public static event Action<DifficultyLevel> DifficultyChanged;

        // AR
        public static event Action<bool> PlaneStatusChanged;                  // plane under reticle?
        public static event Action<Transform> ArenaPlaced;

        // Player
        public static event Action<int, int> PlayerHealthChanged;             // current, max
        public static event Action<int, Vector3> PlayerDamaged;               // amount, source position
        public static event Action PlayerDied;
        public static event Action ShotFired;

        // Session
        public static event Action<int, int> ScoreChanged;                    // total, delta
        public static event Action<float> TimeRemainingChanged;
        public static event Action<int> KillCountChanged;

        // Enemies
        public static event Action<EnemyType, Transform> EnemySpawned;
        public static event Action<EnemyType, Vector3, bool> EnemyHit;        // type, point, killed
        public static event Action<EnemyType, Vector3, int> EnemyKilled;      // type, position, score awarded

        public static void RaiseStateChanged(GameStateId prev, GameStateId next) => StateChanged?.Invoke(prev, next);
        public static void RaiseCountdownTick(int value) => CountdownTick?.Invoke(value);
        public static void RaiseDifficultyChanged(DifficultyLevel level) => DifficultyChanged?.Invoke(level);
        public static void RaisePlaneStatusChanged(bool found) => PlaneStatusChanged?.Invoke(found);
        public static void RaiseArenaPlaced(Transform arena) => ArenaPlaced?.Invoke(arena);
        public static void RaisePlayerHealthChanged(int current, int max) => PlayerHealthChanged?.Invoke(current, max);
        public static void RaisePlayerDamaged(int amount, Vector3 source) => PlayerDamaged?.Invoke(amount, source);
        public static void RaisePlayerDied() => PlayerDied?.Invoke();
        public static void RaiseShotFired() => ShotFired?.Invoke();
        public static void RaiseScoreChanged(int total, int delta) => ScoreChanged?.Invoke(total, delta);
        public static void RaiseTimeRemainingChanged(float seconds) => TimeRemainingChanged?.Invoke(seconds);
        public static void RaiseKillCountChanged(int kills) => KillCountChanged?.Invoke(kills);
        public static void RaiseEnemySpawned(EnemyType type, Transform t) => EnemySpawned?.Invoke(type, t);
        public static void RaiseEnemyHit(EnemyType type, Vector3 point, bool killed) => EnemyHit?.Invoke(type, point, killed);
        public static void RaiseEnemyKilled(EnemyType type, Vector3 pos, int score) => EnemyKilled?.Invoke(type, pos, score);

        /// <summary>
        /// Domain reload is disabled in this project (fast Enter Play Mode), so static
        /// subscribers must be cleared manually at the start of every play session.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            StateChanged = null; CountdownTick = null; DifficultyChanged = null;
            PlaneStatusChanged = null; ArenaPlaced = null;
            PlayerHealthChanged = null; PlayerDamaged = null; PlayerDied = null; ShotFired = null;
            ScoreChanged = null; TimeRemainingChanged = null; KillCountChanged = null;
            EnemySpawned = null; EnemyHit = null; EnemyKilled = null;
        }
    }
}
