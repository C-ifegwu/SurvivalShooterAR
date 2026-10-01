using UnityEngine;
using DG.Tweening;
using SurvivalShooter.Audio;
using SurvivalShooter.Core;

namespace SurvivalShooter.UI
{
    /// <summary>
    /// Maps game states to UI screens (Observer: listens to GameEvents.StateChanged).
    /// Gameplay code never references screens directly.
    /// </summary>
    [DefaultExecutionOrder(-70)]
    public class UIManager : Singleton<UIManager>
    {
        [SerializeField] private MainMenuScreen mainMenu;
        [SerializeField] private ScanningScreen scanning;
        [SerializeField] private HUDScreen hud;
        [SerializeField] private PauseScreen pause;
        [SerializeField] private GameOverScreen gameOver;
        [SerializeField] private LeaderboardScreen leaderboard;
        [SerializeField] private SettingsScreen settings;
        [SerializeField] private CanvasGroup menuBackdrop;

        private UIScreen[] all;

        protected override void OnSingletonAwake()
        {
            all = new UIScreen[] { mainMenu, scanning, hud, pause, gameOver, leaderboard, settings };
            foreach (var s in all) if (s != null) s.ShowImmediate(false);
        }

        private void OnEnable() => GameEvents.StateChanged += OnStateChanged;
        private void OnDisable() => GameEvents.StateChanged -= OnStateChanged;

        private void OnStateChanged(GameStateId previous, GameStateId next)
        {
            Toggle(mainMenu, next == GameStateId.MainMenu);
            Toggle(scanning, next == GameStateId.Scanning);
            Toggle(hud, next == GameStateId.Countdown || next == GameStateId.Playing || next == GameStateId.Paused);
            Toggle(pause, next == GameStateId.Paused);
            Toggle(gameOver, next == GameStateId.GameOver);
            if (leaderboard != null && leaderboard.IsVisible) leaderboard.Hide();
            if (settings != null && settings.IsVisible) settings.Hide();

            if (menuBackdrop != null)
            {
                bool backdrop = next == GameStateId.MainMenu || next == GameStateId.GameOver || next == GameStateId.Paused;
                menuBackdrop.gameObject.SetActive(true);
                menuBackdrop.DOKill();
                menuBackdrop.DOFade(backdrop ? 1f : 0f, 0.35f).SetUpdate(true);
                menuBackdrop.blocksRaycasts = false;
            }
        }

        private static void Toggle(UIScreen screen, bool visible)
        {
            if (screen == null) return;
            if (visible) screen.Show(); else screen.Hide();
        }

        public void ShowLeaderboard()
        {
            if (leaderboard != null) leaderboard.Show();
        }

        public void ShowSettings()
        {
            if (settings != null) settings.Show();
        }

        public void HideSettings()
        {
            if (settings != null) settings.Hide();
            if (AudioManager.HasInstance) AudioManager.Instance.Play(SoundId.UIBack);
        }

        public void HideLeaderboard()
        {
            if (leaderboard != null) leaderboard.Hide();
            if (AudioManager.HasInstance) AudioManager.Instance.Play(SoundId.UIBack);
        }
    }
}
