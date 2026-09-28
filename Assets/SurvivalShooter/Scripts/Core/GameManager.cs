using System.Collections.Generic;
using UnityEngine;
using SurvivalShooter.Audio;
using SurvivalShooter.Data;
using SurvivalShooter.Enemies;
using SurvivalShooter.Player;
using SurvivalShooter.Pooling;
using SurvivalShooter.AR;

namespace SurvivalShooter.Core
{
    /// <summary>
    /// Master Game Manager implementing the State Pattern and Singleton Pattern.
    /// Governs complete game loop (Start -> Play -> End), score tallying, time limits, and session persistence.
    /// Coordinates seamless background music and state transitions.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("State & Difficulty")]
        [SerializeField] private GameState currentState = GameState.ScanningPlanes;
        [SerializeField] private DifficultyLevel selectedDifficulty = DifficultyLevel.Normal;

        [Header("Difficulty Presets")]
        [SerializeField] private DifficultyConfig normalConfig = DifficultyConfig.CreateDefaultNormal();
        [SerializeField] private DifficultyConfig hardConfig = DifficultyConfig.CreateDefaultHard();

        // Runtime Tracking
        private int currentScore;
        private int enemiesDefeated;
        private float timeRemaining;
        private float timeSurvived;
        private bool isTimerRunning;
        private DifficultyConfig activeConfig;

        // Public Properties
        public GameState CurrentState => currentState;
        public DifficultyLevel SelectedDifficulty => selectedDifficulty;
        public int CurrentScore => currentScore;
        public int EnemiesDefeated => enemiesDefeated;
        public float TimeRemaining => timeRemaining;
        public float TimeSurvived => timeSurvived;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnPlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnPlayerDied -= HandlePlayerDied;
        }

        private void Start()
        {
            SetDifficulty(DifficultyLevel.Normal);
            SetState(GameState.ScanningPlanes);

            // Start ambient menu music
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMenuBGM();
            }
        }

        public void SetDifficulty(DifficultyLevel level)
        {
            selectedDifficulty = level;
            activeConfig = (level == DifficultyLevel.Normal) ? normalConfig : hardConfig;
            Debug.Log($"[GameManager] Difficulty selected: {level}");
        }

        public void SetState(GameState newState)
        {
            currentState = newState;
            Debug.Log($"[GameManager] State changed to: {newState}");
            GameEvents.TriggerGameStateChanged(currentState);
        }

        public void StartGamePlacementFlow()
        {
            SetState(GameState.ScanningPlanes);
        }

        public void OnPlacementComplete()
        {
            StartCombat();
        }

        public void StartCombat()
        {
            currentScore = 0;
            enemiesDefeated = 0;
            timeSurvived = 0f;
            if (activeConfig == null) SetDifficulty(selectedDifficulty);
            timeRemaining = activeConfig.survivalTime;
            isTimerRunning = true;

            // Apply difficulty configurations
            if (EnemySpawner.Instance != null)
            {
                EnemySpawner.Instance.SetDifficultyParameters(
                    activeConfig.spawnInterval,
                    activeConfig.enemySpeedMultiplier,
                    activeConfig.enemyHealthMultiplier,
                    activeConfig.enemyDamageMultiplier
                );
            }

            if (PlayerShooter.Instance != null)
            {
                PlayerShooter.Instance.SetDamage(activeConfig.playerBulletDamage);
            }

            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.ResetHealth();
            }

            // Dispatch initial UI events
            GameEvents.TriggerScoreChanged(currentScore);
            GameEvents.TriggerTimeRemainingUpdated(timeRemaining);

            SetState(GameState.Playing);

            // Switch to high-energy combat action BGM
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGameStart();
                AudioManager.Instance.PlayCombatBGM();
            }
        }

        private void Update()
        {
            if (currentState == GameState.Playing && isTimerRunning)
            {
                timeRemaining -= Time.deltaTime;
                timeSurvived += Time.deltaTime;

                if (timeRemaining < 0) timeRemaining = 0;
                GameEvents.TriggerTimeRemainingUpdated(timeRemaining);

                if (timeRemaining <= 0)
                {
                    OnVictory();
                }
            }
        }

        private void HandleEnemyKilled(EnemyType type, int scoreAwarded)
        {
            if (currentState != GameState.Playing) return;

            enemiesDefeated++;
            currentScore += scoreAwarded;
            GameEvents.TriggerScoreChanged(currentScore);
        }

        private void HandlePlayerDied()
        {
            if (currentState != GameState.Playing) return;
            OnGameOver();
        }

        private void OnVictory()
        {
            isTimerRunning = false;
            SetState(GameState.Victory);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayVictory();
            }

            SaveSessionToLeaderboard();
        }

        private void OnGameOver()
        {
            isTimerRunning = false;
            SetState(GameState.GameOver);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayDefeat();
            }

            SaveSessionToLeaderboard();
        }

        private void SaveSessionToLeaderboard()
        {
            if (LeaderboardManager.Instance != null)
            {
                LeaderboardManager.Instance.SaveSession(
                    currentScore,
                    enemiesDefeated,
                    timeSurvived,
                    selectedDifficulty.ToString()
                );
            }
        }

        public void RestartGame()
        {
            // Wipe active enemies
            if (EnemySpawner.Instance != null)
            {
                EnemySpawner.Instance.WipeAllEnemies();
            }

            // Reset object pools
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ResetAllPools();
            }

            // Reset player
            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.ResetHealth();
            }

            // Directly begin combat if already placed, or re-trigger placement
            if (ARPlacementManager.Instance != null && ARPlacementManager.Instance.IsObjectPlaced)
            {
                StartCombat();
            }
            else
            {
                if (ARPlacementManager.Instance != null)
                {
                    ARPlacementManager.Instance.ResetPlacement();
                }
                SetState(GameState.ScanningPlanes);
            }
        }

        public void ReturnToMainMenu()
        {
            if (EnemySpawner.Instance != null)
            {
                EnemySpawner.Instance.WipeAllEnemies();
            }

            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ResetAllPools();
            }

            if (ARPlacementManager.Instance != null)
            {
                ARPlacementManager.Instance.ResetPlacement();
            }

            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.ResetHealth();
            }

            SetState(GameState.ScanningPlanes);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayMenuBGM();
            }
        }
    }
}