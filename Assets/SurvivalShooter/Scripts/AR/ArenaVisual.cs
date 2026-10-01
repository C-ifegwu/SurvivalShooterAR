using UnityEngine;
using DG.Tweening;

namespace SurvivalShooter.AR
{
    /// <summary>Visual for the placed combat zone: a ring that expands in and slowly rotates.</summary>
    public class ArenaVisual : MonoBehaviour
    {
        [SerializeField] private Transform ring;
        [SerializeField] private Transform pulse;
        [SerializeField] private float spinSpeed = 8f;
        private Material pulseMaterial;

        private void Start()
        {
            if (ring != null)
            {
                Vector3 s = ring.localScale;
                ring.localScale = Vector3.zero;
                ring.DOScale(s, 0.7f).SetEase(Ease.OutBack);
            }
            if (pulse != null)
            {
                Vector3 s = pulse.localScale;
                pulse.localScale = s * 0.2f;
                pulse.DOScale(s * 3.2f, 0.9f).SetEase(Ease.OutCubic);
                var r = pulse.GetComponent<Renderer>();
                if (r != null)
                {
                    pulseMaterial = r.material;
                    pulseMaterial.DOFade(0f, 0.9f).SetEase(Ease.OutCubic).OnComplete(() => pulse.gameObject.SetActive(false));
                }
            }
        }

        private void Update()
        {
            if (ring != null) ring.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        }

        private void OnDestroy()
        {
            if (ring != null) ring.DOKill();
            if (pulse != null) pulse.DOKill();
            if (pulseMaterial != null) { pulseMaterial.DOKill(); Destroy(pulseMaterial); }
        }
    }
}
