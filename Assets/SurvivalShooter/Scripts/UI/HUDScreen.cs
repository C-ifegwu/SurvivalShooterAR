using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using SurvivalShooter.Core;
using SurvivalShooter.Enemies;

namespace SurvivalShooter.UI
{
    /// <summary>
    /// In-game HUD: health, score, time remaining, kills, crosshair + hit-marker, damage vignette,
    /// countdown and score pop-ups. Driven entirely by GameEvents (Observer pattern).
    /// </summary>
    public class HUDScreen : UIScreen
    {
        [Header("Health")]
        [SerializeField] private Image healthFill;
        [SerializeField] private Image healthLag;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private RectTransform healthBlock;
        [SerializeField] private Image heartIcon;

        [Header("Score / Time / Kills")]
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private Image timePill;
        [SerializeField] private Image timeProgress;
        [SerializeField] private TMP_Text killsText;
        [SerializeField] private TMP_Text hostilesText;

        [Header("Combat Feedback")]
        [SerializeField] private RectTransform crosshair;
        [SerializeField] private Image hitMarker;
        [SerializeField] private Image damageVignette;
        [SerializeField] private Image lowHealthVignette;
        [SerializeField] private TMP_Text scorePopup;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private RectTransform shakeRoot;
        [SerializeField] private GameObject combatControls;

        [Header("Buttons")]
        [SerializeField] private Button pauseButton;

        [Header("Palette")]
        [SerializeField] private Color healthHigh = new Color(0.24f, 1f, 0.63f);
        [SerializeField] private Color healthMid = new Color(1f, 0.7f, 0.22f);
        [SerializeField] private Color healthLow = new Color(1f, 0.25f, 0.38f);
        [SerializeField] private Color pillNormal = new Color(0.04f, 0.08f, 0.13f, 0.78f);
        [SerializeField] private Color pillDanger = new Color(0.55f, 0.06f, 0.12f, 0.85f);

        private int displayedScore;
        private Tween scoreCount, vignetteTween, lowHpTween, hitTween, popupTween, shakeTween, timeWarnTween;
        private float totalTime = 90f;
        private bool lowHealth;
        private Vector2 popupBasePos;

        protected override void Awake()
        {
            base.Awake();
            pauseButton.onClick.AddListener(() => GameManager.Instance.RequestPause());
            if (scorePopup != null) popupBasePos = scorePopup.rectTransform.anchoredPosition;
        }

        private void OnEnable()
        {
            GameEvents.PlayerHealthChanged += OnHealth;
            GameEvents.PlayerDamaged += OnDamaged;
            GameEvents.ScoreChanged += OnScore;
            GameEvents.TimeRemainingChanged += OnTime;
            GameEvents.KillCountChanged += OnKills;
            GameEvents.EnemyHit += OnEnemyHit;
            GameEvents.CountdownTick += OnCountdown;
            GameEvents.StateChanged += OnState;
        }

        private void OnDisable()
        {
            GameEvents.PlayerHealthChanged -= OnHealth;
            GameEvents.PlayerDamaged -= OnDamaged;
            GameEvents.ScoreChanged -= OnScore;
            GameEvents.TimeRemainingChanged -= OnTime;
            GameEvents.KillCountChanged -= OnKills;
            GameEvents.EnemyHit -= OnEnemyHit;
            GameEvents.CountdownTick -= OnCountdown;
            GameEvents.StateChanged -= OnState;
        }

        protected override void OnShown()
        {
            if (GameManager.HasInstance && GameManager.Instance.Session != null)
            {
                var s = GameManager.Instance.Session;
                totalTime = s.Config.survivalTime;
                SetScoreImmediate(s.Score);
                OnTime(s.TimeRemaining);
                OnKills(s.Kills);
            }
            if (Player.PlayerHealth.HasInstance) OnHealth(Player.PlayerHealth.Instance.CurrentHealth, Player.PlayerHealth.Instance.MaxHealth);
            if (hitMarker != null) hitMarker.color = new Color(1, 1, 1, 0);
            if (damageVignette != null) damageVignette.color = new Color(1, 0.1f, 0.15f, 0);
            if (scorePopup != null) scorePopup.alpha = 0f;
            RefreshCombatControls();
        }

        private void OnState(GameStateId prev, GameStateId next) => RefreshCombatControls();

        private void RefreshCombatControls()
        {
            bool playing = GameManager.HasInstance && GameManager.Instance.CurrentState == GameStateId.Playing;
            bool countdown = GameManager.HasInstance && GameManager.Instance.CurrentState == GameStateId.Countdown;
            if (combatControls != null) combatControls.SetActive(playing);
            if (pauseButton != null) pauseButton.interactable = playing;
            if (countdownText != null && !countdown) countdownText.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (hostilesText != null && EnemySpawner.HasInstance)
                hostilesText.text = EnemySpawner.Instance.AliveCount.ToString();
        }

        // ---------------------------------------------------------------- Health

        private void OnHealth(int current, int max)
        {
            float v = max > 0 ? (float)current / max : 0f;
            Color c = v > 0.6f ? healthHigh : (v > 0.3f ? healthMid : healthLow);
            if (healthFill != null)
            {
                healthFill.DOKill();
                healthFill.DOFillAmount(v, 0.15f).SetUpdate(true);
                healthFill.DOColor(c, 0.2f).SetUpdate(true);
            }
            if (healthLag != null)
            {
                healthLag.DOKill();
                if (v >= healthLag.fillAmount) healthLag.fillAmount = v;
                else healthLag.DOFillAmount(v, 0.45f).SetDelay(0.25f).SetEase(Ease.OutCubic).SetUpdate(true);
            }
            if (heartIcon != null) heartIcon.color = c;
            if (healthText != null) healthText.text = current.ToString();

            bool low = v <= 0.3f && current > 0;
            if (low != lowHealth)
            {
                lowHealth = low;
                lowHpTween?.Kill();
                if (lowHealthVignette != null)
                {
                    if (low)
                    {
                        lowHealthVignette.color = new Color(1f, 0.1f, 0.15f, 0.15f);
                        lowHpTween = lowHealthVignette.DOFade(0.45f, 0.55f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
                    }
                    else lowHealthVignette.DOFade(0f, 0.3f).SetUpdate(true);
                }
            }
        }

        private void OnDamaged(int amount, Vector3 source)
        {
            vignetteTween?.Kill();
            if (damageVignette != null)
            {
                damageVignette.color = new Color(1f, 0.1f, 0.15f, Mathf.Clamp01(0.45f + amount * 0.02f));
                vignetteTween = damageVignette.DOFade(0f, 0.5f).SetEase(Ease.OutQuad);
            }
            shakeTween?.Kill(true);
            if (shakeRoot != null) shakeTween = shakeRoot.DOShakeAnchorPos(0.25f, 18f, 22, 90f, false, true);
            if (healthBlock != null)
            {
                healthBlock.DOKill(true);
                healthBlock.DOPunchScale(Vector3.one * 0.08f, 0.25f, 8, 0.6f);
            }
        }

        // ---------------------------------------------------------------- Score / time / kills

        private void SetScoreImmediate(int value)
        {
            scoreCount?.Kill();
            displayedScore = value;
            if (scoreText != null) scoreText.text = value.ToString("N0");
        }

        private void OnScore(int total, int delta)
        {
            if (delta <= 0) { SetScoreImmediate(total); return; }

            scoreCount?.Kill();
            scoreCount = DOTween.To(() => displayedScore, x => { displayedScore = x; scoreText.text = x.ToString("N0"); }, total, 0.35f);
            scoreText.rectTransform.DOKill(true);
            scoreText.rectTransform.DOPunchScale(Vector3.one * 0.18f, 0.25f, 6, 0.6f);

            if (scorePopup != null)
            {
                popupTween?.Kill();
                scorePopup.text = $"+{delta}";
                scorePopup.alpha = 1f;
                scorePopup.rectTransform.anchoredPosition = popupBasePos;
                scorePopup.rectTransform.localScale = Vector3.one * 0.6f;
                Sequence s = DOTween.Sequence();
                s.Append(scorePopup.rectTransform.DOScale(1.1f, 0.18f).SetEase(Ease.OutBack));
                s.Join(scorePopup.rectTransform.DOAnchorPosY(popupBasePos.y + 70f, 0.8f).SetEase(Ease.OutCubic));
                s.Insert(0.45f, scorePopup.DOFade(0f, 0.35f));
                popupTween = s;
            }
        }

        private void OnTime(float remaining)
        {
            if (timeText != null) timeText.text = FormatTime(Mathf.Ceil(remaining));
            if (timeProgress != null) timeProgress.fillAmount = totalTime > 0 ? remaining / totalTime : 0f;

            bool danger = remaining <= 10f && remaining > 0f;
            if (timePill != null) timePill.color = danger ? pillDanger : pillNormal;
            if (danger && timeText != null && (timeWarnTween == null || !timeWarnTween.IsActive()))
            {
                timeWarnTween = timeText.rectTransform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 4, 0.5f);
            }
        }

        private void OnKills(int kills)
        {
            if (killsText != null) killsText.text = kills.ToString();
        }

        // ---------------------------------------------------------------- Combat feedback

        private void OnEnemyHit(EnemyType type, Vector3 point, bool killed)
        {
            if (hitMarker == null) return;
            hitTween?.Kill();
            hitMarker.color = killed ? new Color(1f, 0.3f, 0.35f, 1f) : Color.white;
            hitMarker.rectTransform.localScale = Vector3.one * (killed ? 1.5f : 1.1f);
            Sequence s = DOTween.Sequence();
            s.Append(hitMarker.rectTransform.DOScale(killed ? 1.1f : 0.85f, 0.12f));
            s.Append(hitMarker.DOFade(0f, 0.18f));
            hitTween = s;

            if (crosshair != null)
            {
                crosshair.DOKill(true);
                crosshair.DOPunchScale(Vector3.one * 0.15f, 0.15f, 4, 0.5f);
            }
        }

        private void OnCountdown(int value)
        {
            if (countdownText == null) return;
            countdownText.gameObject.SetActive(true);
            countdownText.text = value > 0 ? value.ToString() : "SURVIVE!";
            countdownText.color = value > 0 ? Color.white : new Color(0.25f, 0.91f, 1f);
            RectTransform rt = countdownText.rectTransform;
            rt.DOKill();
            countdownText.DOKill();
            rt.localScale = Vector3.one * 1.8f;
            countdownText.alpha = 0f;
            Sequence s = DOTween.Sequence();
            s.Append(rt.DOScale(1f, 0.35f).SetEase(Ease.OutBack));
            s.Join(countdownText.DOFade(1f, 0.2f));
            s.AppendInterval(value > 0 ? 0.25f : 0.5f);
            s.Append(countdownText.DOFade(0f, 0.2f));
            if (value <= 0) s.OnComplete(() => countdownText.gameObject.SetActive(false));
        }
    }
}
