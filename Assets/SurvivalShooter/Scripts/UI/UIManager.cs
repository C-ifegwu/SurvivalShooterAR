using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SurvivalShooter.Core;
using SurvivalShooter.Audio;
using SurvivalShooter.Data;
using SurvivalShooter.Player;

namespace SurvivalShooter.UI
{
    /// <summary>
    /// Comprehensive UI Manager coordinating Start Menu, HUD, End-Game Summary, and Local Leaderboard.
    /// Operates via the Observer Pattern by subscribing to decoupled GameEvents.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Menu Panels")]
        [SerializeField] private GameObject startMenuPanel;
        [SerializeField] private GameObject inGamePanel;
        [SerializeField] private GameObject endGamePanel;
        [SerializeField] private GameObject leaderboardPanel;

        [Header("Start Menu Elements")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button difficultyNormalButton;
        [SerializeField] private Button difficultyHardButton;
        [SerializeField] private TextMeshProUGUI planeStatusText;

        [Header("In-Game HUD Elements")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timeRemainingText;
        [SerializeField] private Button shootButton;
        [SerializeField] private Image damageVignetteImage;

        [Header("End Game Summary Elements")]
        [SerializeField] private TextMeshProUGUI endTitleText;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI enemiesDefeatedText;
        [SerializeField] private TextMeshProUGUI timeSurvivedText;
        [SerializeField] private TextMeshProUGUI difficultyPlayedText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button endLeaderboardButton;

        [Header("Leaderboard Panel Elements")]
        [SerializeField] private Transform leaderboardContentContainer;
        [SerializeField] private GameObject leaderboardRowPrefab;
        [SerializeField] private TextMeshProUGUI leaderboardRawText;
        [SerializeField] private Button closeLeaderboardButton;

        private Coroutine damageFlashCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            SetupButtonListeners();
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
            GameEvents.OnPlayerHealthChanged += HandlePlayerHealthChanged;
            GameEvents.OnPlayerDamaged += HandlePlayerDamaged;
            GameEvents.OnScoreChanged += HandleScoreChanged;
            GameEvents.OnTimeRemainingUpdated += HandleTimeRemainingUpdated;
            GameEvents.OnPlaneDetectedStatusChanged += HandlePlaneStatusChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
            GameEvents.OnPlayerHealthChanged -= HandlePlayerHealthChanged;
            GameEvents.OnPlayerDamaged -= HandlePlayerDamaged;
            GameEvents.OnScoreChanged -= HandleScoreChanged;
            GameEvents.OnTimeRemainingUpdated -= HandleTimeRemainingUpdated;
            GameEvents.OnPlaneDetectedStatusChanged -= HandlePlaneStatusChanged;
        }

        private void SetupButtonListeners()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(() =>
                {
                    PlayButtonSound();
                    if (GameManager.Instance != null) GameManager.Instance.StartGamePlacementFlow();
                });
            }

            if (difficultyNormalButton != null)
            {
                difficultyNormalButton.onClick.AddListener(() =>
                {
                    PlayButtonSound();
                    if (GameManager.Instance != null) GameManager.Instance.SetDifficulty(DifficultyLevel.Normal);
                    HighlightDifficulty(DifficultyLevel.Normal);
                });
            }

            if (difficultyHardButton != null)
            {
                difficultyHardButton.onClick.AddListener(() =>
                {
                    PlayButtonSound();
                    if (GameManager.Instance != null) GameManager.Instance.SetDifficulty(DifficultyLevel.Hard);
                    HighlightDifficulty(DifficultyLevel.Hard);
                });
            }

            if (leaderboardButton != null)
            {
                leaderboardButton.onClick.AddListener(() =>
                {
                    PlayButtonSound();
                    OpenLeaderboard();
                });
            }

            if (endLeaderboardButton != null)
            {
                endLeaderboardButton.onClick.AddListener(() =>
                {
                    PlayButtonSound();
                    OpenLeaderboard();
                });
            }

            if (closeLeaderboardButton != null)
            {
                closeLeaderboardButton.onClick.AddListener(() =>
                {
                    PlayButtonSound();
                    CloseLeaderboard();
                });
            }

            if (shootButton != null)
            {
                shootButton.onClick.AddListener(() =>
                {
                    if (PlayerShooter.Instance != null) PlayerShooter.Instance.Shoot();
                });
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(() =>
                {
                    PlayButtonSound();
                    if (GameManager.Instance != null) GameManager.Instance.RestartGame();
                });
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(() =>
                {
                    PlayButtonSound();
                    if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
                });
            }
        }

        private void PlayButtonSound()
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayButtonClick();
        }

        private void HighlightDifficulty(DifficultyLevel level)
        {
            Color activeColor = new Color(0f, 0.9f, 1f, 1f);
            Color normalColor = new Color(0.7f, 0.7f, 0.7f, 1f);

            if (difficultyNormalButton != null)
            {
                difficultyNormalButton.image.color = (level == DifficultyLevel.Normal) ? activeColor : normalColor;
            }
            if (difficultyHardButton != null)
            {
                difficultyHardButton.image.color = (level == DifficultyLevel.Hard) ? activeColor : normalColor;
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (startMenuPanel != null) startMenuPanel.SetActive(state == GameState.ScanningPlanes || state == GameState.PlacementReady);
            if (inGamePanel != null) inGamePanel.SetActive(state == GameState.Playing);
            if (endGamePanel != null) endGamePanel.SetActive(state == GameState.GameOver || state == GameState.Victory);
            if (leaderboardPanel != null) leaderboardPanel.SetActive(false);

            if (state == GameState.GameOver || state == GameState.Victory)
            {
                PopulateEndGameSummary(state);
            }
        }

        private void PopulateEndGameSummary(GameState state)
        {
            if (GameManager.Instance == null) return;

            if (endTitleText != null)
            {
                endTitleText.text = (state == GameState.Victory) ? "MISSION COMPLETE" : "SURVIVAL FAILED";
                endTitleText.color = (state == GameState.Victory) ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.25f, 0.25f);
            }

            if (finalScoreText != null) finalScoreText.text = $"FINAL SCORE: {GameManager.Instance.CurrentScore}";
            if (enemiesDefeatedText != null) enemiesDefeatedText.text = $"ENEMIES ELIMINATED: {GameManager.Instance.EnemiesDefeated}";
            if (timeSurvivedText != null)
            {
                int minutes = Mathf.FloorToInt(GameManager.Instance.TimeSurvived / 60f);
                int seconds = Mathf.FloorToInt(GameManager.Instance.TimeSurvived % 60f);
                timeSurvivedText.text = $"TIME SURVIVED: {minutes:00}:{seconds:00}";
            }
            if (difficultyPlayedText != null) difficultyPlayedText.text = $"DIFFICULTY: {GameManager.Instance.SelectedDifficulty.ToString().ToUpper()}";
        }

        private void HandlePlayerHealthChanged(int current, int max)
        {
            if (healthSlider != null)
            {
                healthSlider.maxValue = max;
                healthSlider.value = current;
            }

            if (healthText != null)
            {
                healthText.text = $"HP: {current} / {max}";
            }
        }

        private void HandlePlayerDamaged(int damageAmount)
        {
            if (damageVignetteImage != null)
            {
                if (damageFlashCoroutine != null) StopCoroutine(damageFlashCoroutine);
                damageFlashCoroutine = StartCoroutine(DamageFlashRoutine());
            }
        }

        private IEnumerator DamageFlashRoutine()
        {
            if (damageVignetteImage != null) damageVignetteImage.color = new Color(1f, 0f, 0f, 0.85f);
            float duration = 0.35f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (damageVignetteImage != null) damageVignetteImage.color = new Color(1f, 0f, 0f, Mathf.Lerp(0.85f, 0f, elapsed / duration));
                yield return null;
            }

            if (damageVignetteImage != null) damageVignetteImage.color = new Color(1f, 0f, 0f, 0f);
            damageFlashCoroutine = null;
        }

        private void HandleScoreChanged(int totalScore)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE: {totalScore}";
            }
        }

        private void HandleTimeRemainingUpdated(float timeRemaining)
        {
            if (timeRemainingText != null)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                timeRemainingText.text = $"TIME: {minutes:00}:{seconds:00}";
            }
        }

        private void HandlePlaneStatusChanged(bool hasPlane)
        {
            if (planeStatusText != null)
            {
                planeStatusText.text = hasPlane 
                    ? "HORIZONTAL PLANE ACQUIRED\nTAP SCREEN TO ESTABLISH COMBAT ZONE"
                    : "SCANNING ENVIRONMENT...\nMOVE DEVICE OVER FLAT FLOOR OR TABLE";
                planeStatusText.color = hasPlane ? new Color(0f, 1f, 0.8f) : new Color(1f, 0.8f, 0.2f);
            }
        }

        public void OpenLeaderboard()
        {
            if (leaderboardPanel != null)
            {
                leaderboardPanel.SetActive(true);
                PopulateLeaderboard();
            }
        }

        public void CloseLeaderboard()
        {
            if (leaderboardPanel != null)
            {
                leaderboardPanel.SetActive(false);
            }
        }

        private void PopulateLeaderboard()
        {
            if (LeaderboardManager.Instance == null) return;

            List<ScoreEntry> entries = LeaderboardManager.Instance.GetLatestSessions();

            if (leaderboardRawText != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("=== LATEST 5 SESSIONS ===");
                sb.AppendLine();

                if (entries.Count == 0)
                {
                    sb.AppendLine("No battle sessions recorded yet.");
                    sb.AppendLine("Complete a combat round to record score!");
                }
                else
                {
                    for (int i = 0; i < entries.Count; i++)
                    {
                        var e = entries[i];
                        int min = Mathf.FloorToInt(e.timeSurvived / 60f);
                        int sec = Mathf.FloorToInt(e.timeSurvived % 60f);
                        sb.AppendLine($"#{i + 1} | Score: {e.score} | Kills: {e.enemiesDefeated} | Time: {min:00}:{sec:00} | {e.difficulty} | {e.dateString}");
                    }
                }

                leaderboardRawText.text = sb.ToString();
            }
        }

        public void ConfigureUIPanels(
            GameObject startPanel, GameObject inGame, GameObject endPanel, GameObject leadPanel,
            Button startBtn, Button leadBtn, Button normBtn, Button hardBtn, TextMeshProUGUI planeStatus,
            Slider hpSlider, TextMeshProUGUI hpTxt, TextMeshProUGUI scoreTxt, TextMeshProUGUI timeTxt,
            Button fireBtn, Image vignetteImage,
            TextMeshProUGUI endTitle, TextMeshProUGUI finalScore, TextMeshProUGUI enemiesKilled,
            TextMeshProUGUI timeSurv, TextMeshProUGUI diffPlayed, Button restartBtn, Button mainBtn,
            Button endLeadBtn, TextMeshProUGUI rawRecordTxt, Button closeLeadBtn)
        {
            startMenuPanel = startPanel;
            inGamePanel = inGame;
            endGamePanel = endPanel;
            leaderboardPanel = leadPanel;

            startButton = startBtn;
            leaderboardButton = leadBtn;
            difficultyNormalButton = normBtn;
            difficultyHardButton = hardBtn;
            planeStatusText = planeStatus;

            healthSlider = hpSlider;
            healthText = hpTxt;
            scoreText = scoreTxt;
            timeRemainingText = timeTxt;
            shootButton = fireBtn;
            damageVignetteImage = vignetteImage;

            endTitleText = endTitle;
            finalScoreText = finalScore;
            enemiesDefeatedText = enemiesKilled;
            timeSurvivedText = timeSurv;
            difficultyPlayedText = diffPlayed;
            restartButton = restartBtn;
            mainMenuButton = mainBtn;
            endLeaderboardButton = endLeadBtn;

            leaderboardRawText = rawRecordTxt;
            closeLeaderboardButton = closeLeadBtn;

            SetupButtonListeners();
        }
    }
}