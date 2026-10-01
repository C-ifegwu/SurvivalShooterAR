using UnityEngine;

namespace SurvivalShooter.UI
{
    /// <summary>Fits a RectTransform inside the device safe area (notches, rounded corners).</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        private RectTransform rect;
        private Rect lastSafe;
        private Vector2Int lastSize;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafe || lastSize.x != Screen.width || lastSize.y != Screen.height) Apply();
        }

        private void Apply()
        {
            lastSafe = Screen.safeArea;
            lastSize = new Vector2Int(Screen.width, Screen.height);
            if (Screen.width <= 0 || Screen.height <= 0) return;
            Vector2 min = lastSafe.position;
            Vector2 max = lastSafe.position + lastSafe.size;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
