using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using SurvivalShooter.Audio;

namespace SurvivalShooter.UI
{
    /// <summary>Micro-interaction for every button: press squash, release bounce and click sound.</summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIButtonFX : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler
    {
        [SerializeField] private float pressedScale = 0.93f;
        [SerializeField] private SoundId clickSound = SoundId.UIClick;
        [SerializeField] private bool playSound = true;

        private Vector3 baseScale;
        private Selectable selectable;

        private void Awake()
        {
            baseScale = transform.localScale;
            selectable = GetComponent<Selectable>();
        }

        private bool Interactable => selectable == null || selectable.IsInteractable();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!Interactable) return;
            transform.DOKill();
            transform.DOScale(baseScale * pressedScale, 0.08f).SetEase(Ease.OutQuad).SetUpdate(true);
            if (playSound && AudioManager.HasInstance) AudioManager.Instance.Play(clickSound);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.DOKill();
            transform.DOScale(baseScale, 0.25f).SetEase(Ease.OutBack, 2.5f).SetUpdate(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (Interactable && playSound && AudioManager.HasInstance) AudioManager.Instance.Play(SoundId.UIHover);
#endif
        }

        private void OnDisable()
        {
            transform.DOKill();
            transform.localScale = baseScale == Vector3.zero ? Vector3.one : baseScale;
        }
    }
}
