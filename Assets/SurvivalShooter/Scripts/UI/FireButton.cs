using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using SurvivalShooter.Core;
using SurvivalShooter.Player;

namespace SurvivalShooter.UI
{
    /// <summary>Hold-to-fire button with a pulsing ring and recoil kick on every shot.</summary>
    public class FireButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform visual;
        [SerializeField] private Image ring;

        private bool held;

        private void OnEnable() => GameEvents.ShotFired += OnShot;

        private void OnDisable()
        {
            GameEvents.ShotFired -= OnShot;
            Release();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            held = true;
            if (PlayerShooter.HasInstance) PlayerShooter.Instance.SetFireButtonHeld(true);
            if (visual != null)
            {
                visual.DOKill();
                visual.DOScale(0.9f, 0.08f).SetUpdate(true);
            }
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) { if (held) Release(); }

        private void Release()
        {
            held = false;
            if (PlayerShooter.HasInstance) PlayerShooter.Instance.SetFireButtonHeld(false);
            if (visual != null)
            {
                visual.DOKill();
                visual.DOScale(1f, 0.2f).SetEase(Ease.OutBack).SetUpdate(true);
            }
        }

        private void OnShot()
        {
            if (ring == null) return;
            ring.rectTransform.DOKill(true);
            ring.rectTransform.localScale = Vector3.one;
            ring.rectTransform.DOScale(1.18f, 0.12f).SetLoops(2, LoopType.Yoyo).SetUpdate(true);
        }
    }
}
