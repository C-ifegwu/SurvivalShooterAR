using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using SurvivalShooter.Core;
using SurvivalShooter.Data;

namespace SurvivalShooter.UI
{
    /// <summary>Start menu: title, difficulty selector, Start and Leaderboard buttons.</summary>
    public class MainMenuScreen : UIScreen
    {
        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button normalButton;
        [SerializeField] private Button hardButton;

        [Header("Difficulty Visuals")]
        [SerializeField] private Image normalBg;
        [SerializeField] private Image hardBg;
        [SerializeField] private Image normalOutline;
        [SerializeField] private Image hardOutline;
        [SerializeField] private TMP_Text normalTitle;
        [SerializeField] private TMP_Text hardTitle;
        [SerializeField] private TMP_Text normalDesc;
        [SerializeField] private TMP_Text hardDesc;
        [SerializeField] private Image normalIcon;
        [SerializeField] private Image hardIcon;

        [Header("Info")]
        [SerializeField] private TMP_Text bestScoreText;
        [SerializeField] private RectTransform titleBlock;
        [SerializeField] private RectTransform startGlow;
        [SerializeField] private RectTransform decoRing;

        [Header("Palette")]
        [SerializeField] private Color accent = new Color(0.25f, 0.91f, 1f);
        [SerializeField] private Color danger = new Color(1f, 0.25f, 0.38f);
        [SerializeField] private Color idleCard = new Color(1f, 1f, 1f, 0.06f);
        [SerializeField] private Color idleText = new Color(0.62f, 0.7f, 0.78f);

        private Tween glowTween;
        private Tween ringTween;

        protected override void Awake()
        {
            base.Awake();
            startButton.onClick.AddListener(() => GameManager.Instance.RequestStart());
            leaderboardButton.onClick.AddListener(() => UIManager.Instance.ShowLeaderboard());
            if (settingsButton != null) settingsButton.onClick.AddListener(() => UIManager.Instance.ShowSettings());
            normalButton.onClick.AddListener(() => GameManager.Instance.SetDifficulty(DifficultyLevel.Normal));
            hardButton.onClick.AddListener(() => GameManager.Instance.SetDifficulty(DifficultyLevel.Hard));
        }

        private void OnEnable() => GameEvents.DifficultyChanged += Refresh;
        private void OnDisable() => GameEvents.DifficultyChanged -= Refresh;

        protected override void OnShown()
        {
            if (GameManager.HasInstance)
            {
                var gm = GameManager.Instance;
                if (normalDesc != null) normalDesc.text = gm.GetConfig(DifficultyLevel.Normal).description;
                if (hardDesc != null) hardDesc.text = gm.GetConfig(DifficultyLevel.Hard).description;
                Refresh(gm.SelectedDifficulty);
            }

            if (bestScoreText != null)
            {
                int best = LeaderboardManager.HasInstance ? LeaderboardManager.Instance.BestScore : 0;
                bestScoreText.text = best > 0 ? $"BEST SCORE  <color=#3FE8FF>{best:N0}</color>" : "NO MISSIONS YET — BE THE FIRST";
            }

            if (titleBlock != null)
            {
                titleBlock.DOKill(true);
                Vector2 p = titleBlock.anchoredPosition;
                titleBlock.anchoredPosition = p + new Vector2(0f, 60f);
                titleBlock.DOAnchorPos(p, 0.7f).SetEase(Ease.OutCubic).SetUpdate(true);
            }

            glowTween?.Kill();
            if (startGlow != null)
            {
                startGlow.localScale = Vector3.one;
                glowTween = startGlow.DOScale(1.08f, 1.1f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            }
            ringTween?.Kill();
            if (decoRing != null)
                ringTween = decoRing.DOLocalRotate(new Vector3(0, 0, -360f), 40f, RotateMode.FastBeyond360).SetLoops(-1).SetEase(Ease.Linear).SetUpdate(true);
        }

        protected override void OnHidden()
        {
            glowTween?.Kill();
            ringTween?.Kill();
        }

        private void Refresh(DifficultyLevel level)
        {
            bool normal = level == DifficultyLevel.Normal;
            Style(normalBg, normalOutline, normalTitle, normalIcon, normal, accent);
            Style(hardBg, hardOutline, hardTitle, hardIcon, !normal, danger);
        }

        private void Style(Image bg, Image outline, TMP_Text title, Image icon, bool selected, Color color)
        {
            if (bg != null) bg.DOColor(selected ? new Color(color.r, color.g, color.b, 0.16f) : idleCard, 0.2f).SetUpdate(true);
            if (outline != null) outline.DOColor(selected ? color : new Color(1f, 1f, 1f, 0.12f), 0.2f).SetUpdate(true);
            if (title != null) title.color = selected ? Color.white : idleText;
            if (icon != null) icon.color = selected ? color : idleText;
            if (bg != null && selected)
            {
                bg.rectTransform.DOKill(true);
                bg.rectTransform.DOPunchScale(Vector3.one * 0.04f, 0.25f, 6, 0.6f).SetUpdate(true);
            }
        }
    }
}
