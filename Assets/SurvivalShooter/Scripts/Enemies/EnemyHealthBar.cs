using UnityEngine;
using DG.Tweening;

namespace SurvivalShooter.Enemies
{
    /// <summary>
    /// Tiny camera-facing health bar made of two quads (no Canvas per enemy = cheap on mobile).
    /// The fill pivot is at the left edge, so scaling X shrinks it from the right.
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Transform fillPivot;
        [SerializeField] private Transform lagPivot;

        private Camera cam;
        private float value = 1f;

        private void Awake() => cam = Camera.main;

        public void SetValue(float v, bool instant = false)
        {
            value = Mathf.Clamp01(v);
            gameObject.SetActive(true);
            if (fillPivot != null) fillPivot.localScale = new Vector3(value, 1f, 1f);
            if (lagPivot != null)
            {
                lagPivot.DOKill();
                if (instant) lagPivot.localScale = new Vector3(value, 1f, 1f);
                else lagPivot.DOScaleX(value, 0.35f).SetDelay(0.12f).SetEase(Ease.OutCubic);
            }
            if (!instant)
            {
                transform.DOKill(true);
                transform.DOPunchScale(Vector3.one * 0.25f, 0.18f, 5, 0.5f);
            }
        }

        public void Hide() => gameObject.SetActive(false);

        private void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
        }

        private void OnDestroy()
        {
            transform.DOKill();
            if (lagPivot != null) lagPivot.DOKill();
        }
    }
}
