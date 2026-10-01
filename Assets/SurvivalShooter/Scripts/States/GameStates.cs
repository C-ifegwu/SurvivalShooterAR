using UnityEngine;
using SurvivalShooter.AR;
using SurvivalShooter.Audio;
using SurvivalShooter.Core;
using SurvivalShooter.Enemies;
using SurvivalShooter.Player;
using SurvivalShooter.Pooling;

namespace SurvivalShooter.States
{
    /// <summary>
    /// State pattern: each game state encapsulates its own Enter / Tick / Exit behaviour.
    /// GameManager (the context) just delegates to the current state object.
    /// </summary>
    public abstract class GameStateBase
    {
        protected readonly GameManager Context;
        public abstract GameStateId Id { get; }

        protected GameStateBase(GameManager context) { Context = context; }

        public virtual void Enter(GameStateId previous) { }
        public virtual void Tick(float deltaTime) { }
        public virtual void Exit(GameStateId next) { }
    }

    /// <summary>Start menu: title, difficulty selection, leaderboard.</summary>
    public class MainMenuState : GameStateBase
    {
        public override GameStateId Id => GameStateId.MainMenu;
        public MainMenuState(GameManager ctx) : base(ctx) { }

        public override void Enter(GameStateId previous)
        {
            Time.timeScale = 1f;
            Context.CleanUpCombat();
            if (ARPlacementManager.HasInstance) ARPlacementManager.Instance.ResetPlacement();
            if (ARPlacementManager.HasInstance) ARPlacementManager.Instance.SetPlacementEnabled(false);
            if (AudioManager.HasInstance) AudioManager.Instance.PlayMusic(MusicTrack.Menu);
        }
    }

    /// <summary>Plane detection + tap-to-place. Only one arena can ever be placed.</summary>
    public class ScanningState : GameStateBase
    {
        public override GameStateId Id => GameStateId.Scanning;
        public ScanningState(GameManager ctx) : base(ctx) { }

        public override void Enter(GameStateId previous)
        {
            if (!ARPlacementManager.HasInstance) return;
            ARPlacementManager.Instance.ArenaPlacedCallback = OnArenaPlaced;
            ARPlacementManager.Instance.SetPlacementEnabled(true);
        }

        private void OnArenaPlaced()
        {
            Context.ChangeState(GameStateId.Countdown);
        }

        public override void Exit(GameStateId next)
        {
            if (!ARPlacementManager.HasInstance) return;
            ARPlacementManager.Instance.ArenaPlacedCallback = null;
            ARPlacementManager.Instance.SetPlacementEnabled(false);
        }
    }

    /// <summary>3-2-1 countdown that creates a fresh session before combat.</summary>
    public class CountdownState : GameStateBase
    {
        private const float StepDuration = 0.9f;
        private float timer;
        private int lastShown;

        public override GameStateId Id => GameStateId.Countdown;
        public CountdownState(GameManager ctx) : base(ctx) { }

        public override void Enter(GameStateId previous)
        {
            Time.timeScale = 1f;
            Context.BeginNewSession();
            timer = 0f;
            lastShown = 3;
            GameEvents.RaiseCountdownTick(3);
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.PlayMusic(MusicTrack.Combat);
                AudioManager.Instance.Play(SoundId.CountdownBeep);
            }
        }

        public override void Tick(float deltaTime)
        {
            timer += deltaTime;
            int value = 3 - Mathf.FloorToInt(timer / StepDuration);
            if (value != lastShown && value >= 0)
            {
                lastShown = value;
                GameEvents.RaiseCountdownTick(value);
                if (AudioManager.HasInstance) AudioManager.Instance.Play(value == 0 ? SoundId.CountdownGo : SoundId.CountdownBeep);
            }
            if (timer >= StepDuration * 3.6f)
            {
                Context.ChangeState(GameStateId.Playing);
            }
        }
    }

    /// <summary>Active combat: timer runs, enemies spawn, player can shoot.</summary>
    public class PlayingState : GameStateBase
    {
        private float lastBroadcastSecond = -1f;

        public override GameStateId Id => GameStateId.Playing;
        public PlayingState(GameManager ctx) : base(ctx) { }

        public override void Enter(GameStateId previous)
        {
            Time.timeScale = 1f;
            if (AudioManager.HasInstance) AudioManager.Instance.SetPaused(false);
            if (EnemySpawner.HasInstance) EnemySpawner.Instance.SetSpawning(true);
            if (PlayerShooter.HasInstance) PlayerShooter.Instance.SetCombatEnabled(true);
        }

        public override void Tick(float deltaTime)
        {
            GameSession session = Context.Session;
            if (session == null) return;

            session.Tick(deltaTime);

            float remaining = session.TimeRemaining;
            float second = Mathf.Ceil(remaining * 10f) / 10f;
            if (!Mathf.Approximately(second, lastBroadcastSecond))
            {
                lastBroadcastSecond = second;
                GameEvents.RaiseTimeRemainingChanged(remaining);
            }

            if (remaining <= 0f)
            {
                Context.EndSession(GameResult.Survived);
            }
        }

        public override void Exit(GameStateId next)
        {
            if (EnemySpawner.HasInstance) EnemySpawner.Instance.SetSpawning(false);
            if (PlayerShooter.HasInstance) PlayerShooter.Instance.SetCombatEnabled(false);
        }
    }

    /// <summary>Freezes the simulation (timeScale 0). UI keeps animating via unscaled tweens.</summary>
    public class PausedState : GameStateBase
    {
        public override GameStateId Id => GameStateId.Paused;
        public PausedState(GameManager ctx) : base(ctx) { }

        public override void Enter(GameStateId previous)
        {
            Time.timeScale = 0f;
            if (AudioManager.HasInstance) AudioManager.Instance.SetPaused(true);
        }

        public override void Exit(GameStateId next)
        {
            Time.timeScale = 1f;
            if (AudioManager.HasInstance) AudioManager.Instance.SetPaused(false);
        }
    }

    /// <summary>End of round: wipe enemies, save to leaderboard, show summary.</summary>
    public class GameOverState : GameStateBase
    {
        public override GameStateId Id => GameStateId.GameOver;
        public GameOverState(GameManager ctx) : base(ctx) { }

        public override void Enter(GameStateId previous)
        {
            Time.timeScale = 1f;
            Context.CleanUpCombat();
            Context.SaveSessionToLeaderboard();

            if (AudioManager.HasInstance)
            {
                bool won = Context.Session != null && Context.Session.Result == GameResult.Survived;
                AudioManager.Instance.PlayMusic(won ? MusicTrack.Victory : MusicTrack.None);
                if (!won) AudioManager.Instance.Play(SoundId.Defeat);
            }
        }
    }
}
