using UnityEngine;
using DG.Tweening;

namespace SurvivalShooter.AR
{
    /// <summary>Animated placement reticle that smoothly follows the detected plane pose.</summary>
    public class PlacementReticle : MonoBehaviour
    {
        [SerializeField] private Transform spinner;
        [SerializeField] private float spinSpeed = 45f;
        [SerializeField] private float followSharpness = 18f;

        private bool visible = true;
        private bool snapNext = true;
        private Vector3 baseScale;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        public void SetVisible(bool show, bool instant = false)
        {
            if (visible == show && !instant) return;
            visible = show;
            transform.DOKill();
            if (instant)
            {
                transform.localScale = show ? baseScale : Vector3.zero;
                gameObject.SetActive(show);
                snapNext = true;
                return;
            }
            if (show)
            {
                gameObject.SetActive(true);
                snapNext = true;
                transform.localScale = baseScale * 0.4f;
                transform.DOScale(baseScale, 0.35f).SetEase(Ease.OutBack);
            }
            else
            {
                transform.DOScale(Vector3.zero, 0.18f).SetEase(Ease.InBack).OnComplete(() => gameObject.SetActive(false));
            }
        }

        public void Follow(Pose pose)
        {
            Vector3 target = pose.position + Vector3.up * 0.005f;
            if (snapNext)
            {
                transform.position = target;
                snapNext = false;
            }
            else
            {
                float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, target, t);
            }
            transform.rotation = Quaternion.Slerp(transform.rotation, pose.rotation, 0.3f);
        }

        private void Update()
        {
            if (spinner != null) spinner.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        }
    }
}
