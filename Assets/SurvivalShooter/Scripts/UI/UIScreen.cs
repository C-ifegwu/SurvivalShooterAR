using UnityEngine;
using DG.Tweening;

namespace SurvivalShooter.UI
{
    /// <summary>
    /// Base class for every full-screen UI panel. Handles the shared show/hide transition
    /// (fade + slight scale) so every screen feels consistent. Subclasses override the
    /// OnShown / OnHidden hooks (polymorphism) for their own content.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIScreen : MonoBehaviour
    {
        [SerializeField] protected RectTransform content;
        [SerializeField] protected float showDuration = 0.35f;
        [SerializeField] protected float hideDuration = 0.2f;
        [SerializeField] protected float hiddenScale = 0.94f;

        protected CanvasGroup group;
        private Sequence transition;
        public bool IsVisible { get; private set; }

        protected virtual void Awake()
        {
            group = GetComponent<CanvasGroup>();
        }

        public void ShowImmediate(bool visible)
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            transition?.Kill();
            IsVisible = visible;
            gameObject.SetActive(visible);
            group.alpha = visible ? 1f : 0f;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            if (content != null) content.localScale = Vector3.one;
            if (visible) OnShown();
        }

        public void Show()
        {
            if (group == null) group = GetComponent<CanvasGroup>();
            if (IsVisible && gameObject.activeSelf) { OnShown(); return; }
            IsVisible = true;
            gameObject.SetActive(true);
            transition?.Kill();
            group.interactable = true;
            group.blocksRaycasts = true;
            group.alpha = 0f;
            transition = DOTween.Sequence().SetUpdate(true);
            transition.Append(group.DOFade(1f, showDuration).SetEase(Ease.OutCubic));
            if (content != null)
            {
                content.localScale = Vector3.one * hiddenScale;
                transition.Join(content.DOScale(1f, showDuration).SetEase(Ease.OutBack, 1.2f));
            }
            OnShown();
        }

        public void Hide()
        {
            if (!IsVisible) return;
            IsVisible = false;
            transition?.Kill();
            group.interactable = false;
            group.blocksRaycasts = false;
            transition = DOTween.Sequence().SetUpdate(true);
            transition.Append(group.DOFade(0f, hideDuration).SetEase(Ease.InCubic));
            if (content != null) transition.Join(content.DOScale(hiddenScale, hideDuration).SetEase(Ease.InCubic));
            transition.OnComplete(() => gameObject.SetActive(false));
            OnHidden();
        }

        protected virtual void OnShown() { }
        protected virtual void OnHidden() { }

        protected virtual void OnDestroy() => transition?.Kill();

        protected static string FormatTime(float seconds)
        {
            seconds = Mathf.Max(0f, seconds);
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m:00}:{s:00}";
        }
    }
}
