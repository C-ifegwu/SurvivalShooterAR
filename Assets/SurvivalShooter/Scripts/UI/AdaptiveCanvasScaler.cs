using UnityEngine;
using UnityEngine.UI;

namespace SurvivalShooter.UI
{
    /// <summary>
    /// Portrait phones match width; wider screens (tablets, landscape Game view in the Editor)
    /// match height so the portrait layout always fits.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    public class AdaptiveCanvasScaler : MonoBehaviour
    {
        private CanvasScaler scaler;

        private void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        private void Update() => Apply();

        private void Apply()
        {
            if (Screen.height <= 0) return;
            float aspect = (float)Screen.width / Screen.height;
            float reference = scaler.referenceResolution.x / scaler.referenceResolution.y;
            scaler.matchWidthOrHeight = aspect > reference ? 1f : 0f;
        }
    }
}
