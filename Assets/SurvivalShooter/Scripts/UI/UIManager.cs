using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using SurvivalShooter.Core;
using SurvivalShooter.Audio;
using SurvivalShooter.Data;
using SurvivalShooter.Player;

namespace SurvivalShooter.UI
{
    /// <summary>
    /// Master UI Manager coordinating Start Menu, HUD, End-Game Summary, and Local Leaderboard.
    /// Fully animated using DOTween for premium visual polish, micro-interactions, and reactive gameplay feedback.
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
        [SerializeField] private TextMeshProUGUI gameTitleText;

        [Header("In-Game HUD Elements")]
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image healthFillImage;
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

        private Tween healthTween;
        private Tween scoreTween;
        private Tween timerPulseTween;
        private Tween startPulseTween;
        private Tween vignetteTween;
        private Tween endSummaryTween;
        private Tween leaderboardTween;

        private readonly Color fullHealthColor = new Color(0f, 1f, 0.55f, 1f);       // Neon emerald
        private readonly Color mediumHealthColor = new Color(1f, 0.72f, 0.1f, 1f);    // Amber gold
        private readonly Color criticalHealthColor = new Color(1f, 0.2f, 0.2f, 1f);   // Crimson red

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupButtonListeners();
            AttachButtonPolishComponents();
            ResolveHealthFillImage();
        }

        private void Start()
        {
            InitializeAnimations();
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

            KillAllTweens();
        }

        private void KillAllTweens()
        {
            healthTween?.Kill();
            scoreTween?.Kill();
            timerPulseTween?.Kill();
            startPulseTween?.Kill();
            vignetteTween?.Kill();
            endSummaryTween?.Kill();
            leaderboardTween?.Kill();
        }

        private void ResolveHealthFillImage()
        {
            if (healthFillImage == null && healthSlider != null && healthSlider.fillRect != null)
            {
                healthFillImage = healthSlider.fillRect.GetComponent<Image>();
            }
        }

        private void AttachButtonPolishComponents()
        {
            Button[] allButtons = { startButton, leaderboardButton, difficultyNormalButton, difficultyHardButton,
                                    shootButton, restartButton, mainMenuButton, endLeaderboardButton, closeLeaderboardButton };

            foreach (var btn in allButtons)
            {
                if (btn != null && btn.GetComponent<UIButtonPolish>() == null)
                {
                    btn.gameObject.AddComponent<UIButtonPolish>();
                }
            }
        }

        private void InitializeAnimations()
        {
            if (startButton != null)
            {
                startPulseTween?.Kill();
                startPulseTween = startButton.transform.DOScale(1.06f, 0.9f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }

            if (gameTitleText != null)
            {
                gameTitleText.transform.DOScale(1.03f, 1.4f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }
        }

        private void SetupButtonListeners()
        {
            if (startButton != null)
            {
                startButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayWhoosh();
                    if (GameManager.Instance != null) GameManager.Instance.StartGamePlacementFlow();
                });
            }

            if (leaderboardButton != null)
            {
                leaderboardButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayMechanical();
                    OpenLeaderboard();
                });
            }

            if (difficultyNormalButton != null)
            {
                difficultyNormalButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPunch();
                    if (GameManager.Instance != null) GameManager.Instance.SetDifficulty(DifficultyLevel.Normal);
                    UpdateDifficultyButtonVisuals(DifficultyLevel.Normal);
                });
            }

            if (difficultyHardButton != null)
            {
                difficultyHardButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPunch();
                    if (GameManager.Instance != null) GameManager.Instance.SetDifficulty(DifficultyLevel.Hard);
                    UpdateDifficultyButtonVisuals(DifficultyLevel.Hard);
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
                    AudioManager.Instance?.PlayWhoosh();
                    if (GameManager.Instance != null) GameManager.Instance.RestartGame();
                });
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayWhoosh();
                    if (GameManager.Instance != null) GameManager.Instance.ReturnToMainMenu();
                });
            }

            if (endLeaderboardButton != null)
            {
                endLeaderboardButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayMechanical();
                    OpenLeaderboard();
                });
            }

            if (closeLeaderboardButton != null)
            {
                closeLeaderboardButton.onClick.AddListener(() =>
                {
                    AudioManager.Instance?.PlayPunch();
                    CloseLeaderboard();
                });
            }

            UpdateDifficultyButtonVisuals(DifficultyLevel.Normal);
        }

        private void UpdateDifficultyButtonVisuals(DifficultyLevel level)
        {
            Color activeColor = new Color(0f, 0.95f, 1f, 1f);
            Color inactiveColor = new Color(0.55f, 0.55f, 0.6f, 1f);

            if (difficultyNormalButton != null)
            {
                difficultyNormalButton.image.color = (level == DifficultyLevel.Normal) ? activeColor : inactiveColor;
                difficultyNormalButton.transform.DOScale((level == DifficultyLevel.Normal) ? 1.05f : 1f, 0.2f).SetUpdate(true);
            }
            if (difficultyHardButton != null)
            {
                difficultyHardButton.image.color = (level == DifficultyLevel.Hard) ? activeColor : inactiveColor;
                difficultyHardButton.transform.DOScale((level == DifficultyLevel.Hard) ? 1.05f : 1f, 0.2f).SetUpdate(true);
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            bool showStart = (state == GameState.ScanningPlanes || state == GameState.PlacementReady);
            bool showInGame = (state == GameState.Playing);
            bool showEnd = (state == GameState.GameOver || state == GameState.Victory);

            if (startMenuPanel != null)
            {
                startMenuPanel.SetActive(showStart);
                if (showStart)
                {
                    startMenuPanel.transform.localScale = Vector3.one * 0.95f;
                    startMenuPanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
                }
            }

            if (inGamePanel != null)
            {
                inGamePanel.SetActive(showInGame);
                if (showInGame)
                {
                    inGamePanel.transform.localScale = Vector3.one;
                }
            }

            if (endGamePanel != null)
            {
                endGamePanel.SetActive(showEnd);
                if (showEnd)
                {
                    AnimateEndGameEntrance(state);
                }
            }

            if (leaderboardPanel != null)
            {
                leaderboardPanel.SetActive(false);
            }
        }

        private void AnimateEndGameEntrance(GameState state)
        {
            if (endGamePanel == null) return;

            endSummaryTween?.Kill();
            endGamePanel.transform.localScale = Vector3.zero;
            endSummaryTween = endGamePanel.transform.DOScale(1f, 0.45f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);

            PopulateEndGameSummary(state);
        }

        private void PopulateEndGameSummary(GameState state)
        {
            if (GameManager.Instance == null) return;

            if (endTitleText != null)
            {
                endTitleText.text = (state == GameState.Victory) ? "MISSION COMPLETE" : "SURVIVAL FAILED";
                endTitleText.color = (state == GameState.Victory) ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.25f, 0.25f);
                endTitleText.transform.DOPunchScale(Vector3.one * 0.25f, 0.4f, 4, 0.5f).SetUpdate(true);
            }

            int targetScore = GameManager.Instance.CurrentScore;
            if (finalScoreText != null)
            {
                int currentDisplay = 0;
                DOTween.To(() => currentDisplay, x =>
                {
                    currentDisplay = x;
                    finalScoreText.text = $"FINAL SCORE: {x:N0}";
                }, targetScore, 1.1f)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
            }

            if (enemiesDefeatedText != null) enemiesDefeatedText.text = $"ENEMIES ELIMINATED: {GameManager.Instance.EnemiesDefeated}";

            if (timeSurvivedText != null)
            {
                int minutes = Mathf.FloorToInt(GameManager.Instance.TimeSurvived / 60f);
                int seconds = Mathf.FloorToInt(GameManager.Instance.TimeSurvived % 60f);
                timeSurvivedText.text = $"TIME SURVIVED: {minutes:00}:{seconds:00}";
            }

            if (difficultyPlayedText != null)
            {
                difficultyPlayedText.text = $"DIFFICULTY: {GameManager.Instance.SelectedDifficulty.ToString().ToUpper()}";
            }
        }

        private void HandlePlayerHealthChanged(int current, int max)
        {
            float targetValue = Mathf.Clamp(current, 0, max);

            if (healthSlider != null)
            {
                healthSlider.maxValue = max;
                healthTween?.Kill();
                healthTween = healthSlider.DOValue(targetValue, 0.22f)
                    .SetEase(Ease.OutCubic);
            }

            if (healthFillImage != null && max > 0)
            {
                float ratio = (float)current / max;
                Color targetColor;
                if (ratio > 0.5f)
                {
                    targetColor = Color.Lerp(mediumHealthColor, fullHealthColor, (ratio - 0.5f) * 2f);
                }
                else
                {
                    targetColor = Color.Lerp(criticalHealthColor, mediumHealthColor, ratio * 2f);
                }
                healthFillImage.DOColor(targetColor, 0.25f);
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
                vignetteTween?.Kill();
                damageVignetteImage.color = new Color(1f, 0f, 0f, 0.65f);
                vignetteTween = damageVignetteImage.DOFade(0f, 0.35f).SetEase(Ease.OutQuad);
            }

            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.DOShakePosition(0.22f, 0.16f, 15, 90f);
            }
        }

        private void HandleScoreChanged(int totalScore)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE: {totalScore}";
                scoreTween?.Kill();
                scoreTween = scoreText.transform.DOPunchScale(Vector3.one * 0.32f, 0.2f, 4, 0.5f);
            }
        }

        private void HandleTimeRemainingUpdated(float timeRemaining)
        {
            if (timeRemainingText != null)
            {
                int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                timeRemainingText.text = $"TIME: {minutes:00}:{seconds:00}";

                if (timeRemaining <= 10f && timeRemaining > 0f)
                {
                    timeRemainingText.color = new Color(1f, 0.25f, 0.25f, 1f);
                    if (timerPulseTween == null || !timerPulseTween.IsActive())
                    {
                        timerPulseTween = timeRemainingText.transform.DOPunchScale(Vector3.one * 0.25f, 0.25f, 2, 0.5f);
                    }
                }
                else
                {
                    timeRemainingText.color = new Color(0.9f, 0.95f, 1f, 1f);
                }
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

                if (hasPlane)
                {
                    planeStatusText.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 3, 0.5f);
                }
            }
        }

        public void OpenLeaderboard()
        {
            if (leaderboardPanel != null)
            {
                leaderboardPanel.SetActive(true);
                leaderboardTween?.Kill();
                leaderboardPanel.transform.localScale = Vector3.zero;
                leaderboardTween = leaderboardPanel.transform.DOScale(1f, 0.35f)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);

                PopulateLeaderboard();
            }
        }

        public void CloseLeaderboard()
        {
            if (leaderboardPanel != null)
            {
                leaderboardTween?.Kill();
                leaderboardTween = leaderboardPanel.transform.DOScale(0f, 0.22f)
                    .SetEase(Ease.InBack)
                    .SetUpdate(true)
                    .OnComplete(() => leaderboardPanel.SetActive(false));
            }
        }

        private void PopulateLeaderboard()
        {
            if (LeaderboardManager.Instance == null) return;

            List<ScoreEntry> entries = LeaderboardManager.Instance.GetLatestSessions();

            if (leaderboardRawText != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("=== LATEST 5 BATTLE SESSIONS ===");
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
            Button endLeadBtn, TextMeshProUGUI rawRecordTxt, Button closeLeadBtn,
            TextMeshProUGUI titleTxt = null)
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
            gameTitleText = titleTxt;

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

            ResolveHealthFillImage();
            AttachButtonPolishComponents();
            SetupButtonListeners();
            InitializeAnimations();
        }
    }
}