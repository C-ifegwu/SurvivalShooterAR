using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using SurvivalShooter.Audio;

namespace SurvivalShooter.UI
{
    /// <summary>
    /// Interactive button polish component providing DOTween micro-animations and responsive sound feedback.
    /// Operates with unscaled time so UI animations remain fluid even during pause or game-over states.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonPolish : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Animation Settings")]
        [SerializeField] private float hoverScaleMultiplier = 1.08f;
        [SerializeField] private float pressScaleMultiplier = 0.92f;
        [SerializeField] private float animDuration = 0.18f;

        [Header("Sound Options")]
        [SerializeField] private bool playSoundOnClick = true;
        [SerializeField] private bool playSoundOnHover = true;

        private Vector3 originalScale;
        private Tween currentTween;
        private Button targetButton;

        private void Awake()
        {
            originalScale = transform.localScale;
            if (originalScale == Vector3.zero) originalScale = Vector3.one;
            targetButton = GetComponent<Button>();
        }

        private void OnEnable()
        {
            transform.localScale = originalScale;
        }

        private void OnDisable()
        {
            currentTween?.Kill();
            transform.localScale = originalScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (targetButton != null && !targetButton.interactable) return;

            currentTween?.Kill();
            currentTween = transform.DOScale(originalScale * hoverScaleMultiplier, animDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);

            if (playSoundOnHover && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonHover();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            currentTween?.Kill();
            currentTween = transform.DOScale(originalScale, animDuration * 0.85f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (targetButton != null && !targetButton.interactable) return;

            currentTween?.Kill();
            currentTween = transform.DOScale(originalScale * pressScaleMultiplier, animDuration * 0.6f)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (targetButton != null && !targetButton.interactable) return;

            currentTween?.Kill();
            currentTween = transform.DOScale(originalScale * hoverScaleMultiplier, animDuration * 0.8f)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (targetButton != null && !targetButton.interactable) return;

            if (playSoundOnClick && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayButtonClick();
            }
        }
    }
}