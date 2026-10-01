using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using SurvivalShooter.Core;

namespace SurvivalShooter.UI
{
    /// <summary>End-of-round summary: final score, enemies defeated, time survived, restart/menu.</summary>
    public class GameOverScreen : UIScreen
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text subtitleText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text killsText;
        [SerializeField] private TMP_Text killsBreakdownText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text bonusText;
        [SerializeField] private Image resultIcon;
        [SerializeField] private Image resultGlow;
        [SerializeField] private Sprite survivedSprite;
        [SerializeField] private Sprite defeatedSprite;
        [SerializeField] private RectTransform newBestBadge;
        [SerializeField] private RectTransform[] staggerItems;

        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button leaderboardButton;

        [SerializeField] private Color winColor = new Color(0.24f, 1f, 0.63f);
        [SerializeField] private Color loseColor = new Color(1f, 0.25f, 0.38f);

        private Tween countTween;

        protected override void Awake()
        {
            base.Awake();
            restartButton.onClick.AddListener(() => GameManager.Instance.RequestRestart());
            menuButton.onClick.AddListener(() => GameManager.Instance.RequestMainMenu());
            leaderboardButton.onClick.AddListener(() => UIManager.Instance.ShowLeaderboard());
        }

        protected override void OnShown()
        {
            if (!GameManager.HasInstance || GameManager.Instance.Session == null) return;
            GameSession s = GameManager.Instance.Session;
            bool won = s.Result == GameResult.Survived;
            Color c = won ? winColor : loseColor;

            titleText.text = won ? "YOU SURVIVED" : "OVERRUN";
            titleText.color = c;
            subtitleText.text = won
                ? $"{s.Config.displayName} MISSION COMPLETE"
                : $"{s.Config.displayName} MISSION FAILED";
            if (resultIcon != null)
            {
                resultIcon.sprite = won ? survivedSprite : defeatedSprite;
                resultIcon.color = c;
            }
            if (resultGlow != null) resultGlow.color = new Color(c.r, c.g, c.b, 0.35f);

            killsText.text = s.Kills.ToString();
            if (killsBreakdownText != null) killsBreakdownText.text = $"{s.MeleeKills} ZOMBIES  ·  {s.ShooterKills} SOLDIERS";
            timeText.text = FormatTime(s.TimeSurvived);
            if (bonusText != null)
            {
                bonusText.gameObject.SetActive(s.SurvivalBonus > 0);
                bonusText.text = $"INCLUDES SURVIVAL BONUS +{s.SurvivalBonus:N0}";
            }

            // Animated count-up of the final score
            countTween?.Kill();
            int shown = 0;
            scoreText.text = "0";
            countTween = DOTween.To(() => shown, x => { shown = x; scoreText.text = x.ToString("N0"); }, s.Score, 1.1f)
                .SetDelay(0.3f).SetEase(Ease.OutCubic).SetUpdate(true);

            if (newBestBadge != null)
            {
                bool best = GameManager.Instance.LastWasNewBest;
                newBestBadge.gameObject.SetActive(best);
                if (best)
                {
                    newBestBadge.localScale = Vector3.zero;
                    newBestBadge.DOScale(1f, 0.45f).SetDelay(1.3f).SetEase(Ease.OutBack, 2f).SetUpdate(true);
                }
            }

            if (staggerItems != null)
            {
                for (int i = 0; i < staggerItems.Length; i++)
                {
                    var rt = staggerItems[i];
                    if (rt == null) continue;
                    rt.DOKill(true);
                    var cg = rt.GetComponent<CanvasGroup>();
                    if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = 0f;
                    Vector2 p = rt.anchoredPosition;
                    rt.anchoredPosition = p + new Vector2(0f, -40f);
                    float delay = 0.15f + i * 0.08f;
                    rt.DOAnchorPos(p, 0.45f).SetDelay(delay).SetEase(Ease.OutCubic).SetUpdate(true);
                    cg.DOFade(1f, 0.35f).SetDelay(delay).SetUpdate(true);
                }
            }
        }
    }
}
