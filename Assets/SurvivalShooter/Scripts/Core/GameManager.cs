using System.Collections.Generic;
using UnityEngine;
using SurvivalShooter.AR;
using SurvivalShooter.Data;
using SurvivalShooter.Enemies;
using SurvivalShooter.Player;
using SurvivalShooter.Pooling;
using SurvivalShooter.States;

namespace SurvivalShooter.Core
{
    /// <summary>
    /// Context of the State pattern + Singleton. Owns the session model and the
    /// difficulty presets, and routes every flow request (start, pause, restart, menu).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : Singleton<GameManager>
    {
        [Header("Difficulty Presets (Bonus)")]
        [SerializeField] private DifficultyConfig normal = DifficultyConfig.Normal();
        [SerializeField] private DifficultyConfig hard = DifficultyConfig.Hard();

        private readonly Dictionary<GameStateId, GameStateBase> states = new Dictionary<GameStateId, GameStateBase>();
        private GameStateBase current;

        public GameStateId CurrentState => current != null ? current.Id : GameStateId.MainMenu;
        public DifficultyLevel SelectedDifficulty { get; private set; } = DifficultyLevel.Normal;
        public DifficultyConfig ActiveConfig => SelectedDifficulty == DifficultyLevel.Hard ? hard : normal;
        public GameSession Session { get; private set; }
        public SessionRecord LastRecord { get; private set; }
        public bool LastWasNewBest { get; private set; }

        public DifficultyConfig GetConfig(DifficultyLevel level) => level == DifficultyLevel.Hard ? hard : normal;

        protected override void OnSingletonAwake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Register(new MainMenuState(this));
            Register(new ScanningState(this));
            Register(new CountdownState(this));
            Register(new PlayingState(this));
            Register(new PausedState(this));
            Register(new GameOverState(this));
        }

        private void Register(GameStateBase state) => states[state.Id] = state;

        private void OnEnable()
        {
            GameEvents.EnemyKilled += HandleEnemyKilled;
            GameEvents.PlayerDied += HandlePlayerDied;
        }

        private void OnDisable()
        {
            GameEvents.EnemyKilled -= HandleEnemyKilled;
            GameEvents.PlayerDied -= HandlePlayerDied;
        }

        private void Start()
        {
            ChangeState(GameStateId.MainMenu);
        }

        private void Update()
        {
            current?.Tick(Time.deltaTime);
        }

        public void ChangeState(GameStateId next)
        {
            GameStateId previous = CurrentState;
            if (current != null && current.Id == next) return;

            current?.Exit(next);
            current = states[next];
            current.Enter(previous);
            GameEvents.RaiseStateChanged(previous, next);
        }

        // ------------------------------------------------------------------ UI requests

        public void SetDifficulty(DifficultyLevel level)
        {
            SelectedDifficulty = level;
            GameEvents.RaiseDifficultyChanged(level);
        }

        public void RequestStart()
        {
            if (CurrentState == GameStateId.MainMenu) ChangeState(GameStateId.Scanning);
        }

        public void RequestPause()
        {
            if (CurrentState == GameStateId.Playing) ChangeState(GameStateId.Paused);
        }

        public void RequestResume()
        {
            if (CurrentState == GameStateId.Paused) ChangeState(GameStateId.Playing);
        }

        public void RequestRestart()
        {
            CleanUpCombat();
            bool placed = ARPlacementManager.HasInstance && ARPlacementManager.Instance.IsArenaPlaced;
            ChangeState(placed ? GameStateId.Countdown : GameStateId.Scanning);
        }

        public void RequestMainMenu()
        {
            ChangeState(GameStateId.MainMenu);
        }

        // ------------------------------------------------------------------ Session

        public void BeginNewSession()
        {
            CleanUpCombat();
            Session = new GameSession(ActiveConfig);
            if (PlayerHealth.HasInstance) PlayerHealth.Instance.ResetHealth();
            if (EnemySpawner.HasInstance) EnemySpawner.Instance.Configure(ActiveConfig);

            GameEvents.RaiseScoreChanged(0, 0);
            GameEvents.RaiseKillCountChanged(0);
            GameEvents.RaiseTimeRemainingChanged(Session.TimeRemaining);
        }

        public void EndSession(GameResult result)
        {
            if (Session == null || Session.IsFinished) return;
            int hp = PlayerHealth.HasInstance ? PlayerHealth.Instance.CurrentHealth : 0;
            Session.Finish(result, hp);
            ChangeState(GameStateId.GameOver);
        }

        /// <summary>All enemies wiped + every pooled projectile/effect returned.</summary>
        public void CleanUpCombat()
        {
            if (EnemySpawner.HasInstance)
            {
                EnemySpawner.Instance.SetSpawning(false);
                EnemySpawner.Instance.WipeAllEnemies();
            }
            if (PoolManager.HasInstance) PoolManager.Instance.ReleaseAll();
            if (PlayerShooter.HasInstance) PlayerShooter.Instance.SetCombatEnabled(false);
        }

        public void SaveSessionToLeaderboard()
        {
            if (Session == null || !LeaderboardManager.HasInstance) return;
            LastRecord = LeaderboardManager.Instance.Record(Session, out bool newBest);
            LastWasNewBest = newBest;
        }

        private void HandleEnemyKilled(EnemyType type, Vector3 position, int baseScore)
        {
            if (CurrentState != GameStateId.Playing || Session == null) return;
            int awarded = Session.RegisterKill(type, baseScore);
            GameEvents.RaiseScoreChanged(Session.Score, awarded);
            GameEvents.RaiseKillCountChanged(Session.Kills);
        }

        private void HandlePlayerDied()
        {
            if (CurrentState != GameStateId.Playing) return;
            EndSession(GameResult.Defeated);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) RequestPause();
        }
    }
}
