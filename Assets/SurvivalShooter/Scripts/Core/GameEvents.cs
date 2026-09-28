using System;
using UnityEngine;

namespace SurvivalShooter.Core
{
    /// <summary>
    /// Centralized Observer/Event hub for decoupled communication
    /// between gameplay systems and UI / Audio / Analytics.
    /// </summary>
    public static class GameEvents
    {
        // Game State Events
        public static event Action<GameState> OnGameStateChanged;
        public static void TriggerGameStateChanged(GameState state) => OnGameStateChanged?.Invoke(state);

        // Player Events
        public static event Action<int, int> OnPlayerHealthChanged; // current, max
        public static void TriggerPlayerHealthChanged(int current, int max) => OnPlayerHealthChanged?.Invoke(current, max);

        public static event Action<int> OnPlayerDamaged; // damage amount
        public static void TriggerPlayerDamaged(int damage) => OnPlayerDamaged?.Invoke(damage);

        public static event Action OnPlayerDied;
        public static void TriggerPlayerDied() => OnPlayerDied?.Invoke();

        // Score & Progress Events
        public static event Action<int> OnScoreChanged;
        public static void TriggerScoreChanged(int totalScore) => OnScoreChanged?.Invoke(totalScore);

        public static event Action<float> OnTimeRemainingUpdated;
        public static void TriggerTimeRemainingUpdated(float timeRemaining) => OnTimeRemainingUpdated?.Invoke(timeRemaining);

        // Enemy Events
        public static event Action<EnemyType, int> OnEnemyKilled; // type, scoreAwarded
        public static void TriggerEnemyKilled(EnemyType type, int score) => OnEnemyKilled?.Invoke(type, score);

        // Plane / AR Placement Events
        public static event Action<bool> OnPlaneDetectedStatusChanged;
        public static void TriggerPlaneDetectedStatusChanged(bool hasPlane) => OnPlaneDetectedStatusChanged?.Invoke(hasPlane);

        public static event Action<Vector3> OnGameWorldPlaced;
        public static void TriggerGameWorldPlaced(Vector3 position) => OnGameWorldPlaced?.Invoke(position);
    }
}
