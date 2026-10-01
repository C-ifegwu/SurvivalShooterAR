using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using DG.Tweening;

namespace SurvivalShooter.AR
{
    /// <summary>
    /// Custom plane tracker that replaces Unity's default plane visual.
    /// Uses a custom tiled texture that displays the student's full name
    /// ("CHIBUEZE VICTOR IFEGWU") plus a glowing boundary line.
    /// The visual only appears while a horizontal plane is actually being tracked,
    /// fades in when the plane is first detected, and dims once the arena is placed.
    /// </summary>
    [RequireComponent(typeof(ARPlane))]
    [RequireComponent(typeof(MeshRenderer))]
    public class CustomPlaneVisualizer : MonoBehaviour
    {
        public const string StudentName = "CHIBUEZE VICTOR IFEGWU";

        [SerializeField] private float scanningAlpha = 0.85f;
        [SerializeField] private float arenaAlpha = 0.35f;
        [SerializeField] private float fadeDuration = 0.6f;

        private static readonly List<CustomPlaneVisualizer> All = new List<CustomPlaneVisualizer>();
        private static bool arenaMode;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private ARPlane plane;
        private MeshRenderer meshRenderer;
        private LineRenderer lineRenderer;
        private Material instanceMaterial;
        private Color baseColor;
        private Color lineBaseColor;
        private float alpha;
        private Tween fadeTween;
        private bool wasVisible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { All.Clear(); arenaMode = false; }

        private void Awake()
        {
            plane = GetComponent<ARPlane>();
            meshRenderer = GetComponent<MeshRenderer>();
            lineRenderer = GetComponent<LineRenderer>();
            instanceMaterial = meshRenderer.material; // per-plane instance for independent fading
            baseColor = instanceMaterial.HasProperty(BaseColorId) ? instanceMaterial.GetColor(BaseColorId) : Color.white;
            if (lineRenderer != null) lineBaseColor = lineRenderer.startColor;
            ApplyAlpha(0f);
        }

        private void OnEnable() => All.Add(this);

        private void OnDisable()
        {
            All.Remove(this);
            fadeTween?.Kill();
        }

        private void OnDestroy()
        {
            if (instanceMaterial != null) Destroy(instanceMaterial);
        }

        public static void SetArenaMode(bool enabled)
        {
            arenaMode = enabled;
            foreach (var v in All) v.FadeTo(v.TargetAlpha);
        }

        private float TargetAlpha => arenaMode ? arenaAlpha : scanningAlpha;

        private bool ShouldBeVisible =>
            (plane.trackingState == TrackingState.Tracking || (arenaMode && wasVisible)) &&
            plane.subsumedBy == null &&
            (plane.alignment == PlaneAlignment.HorizontalUp || plane.alignment == PlaneAlignment.HorizontalDown);

        private void LateUpdate()
        {
            bool visible = ShouldBeVisible;
            meshRenderer.enabled = visible;
            if (lineRenderer != null) lineRenderer.enabled = visible;

            if (visible && !wasVisible) FadeTo(TargetAlpha);   // plane just detected → fade in
            if (!visible && wasVisible) { fadeTween?.Kill(); ApplyAlpha(0f); }
            wasVisible = visible;
        }

        private void FadeTo(float target)
        {
            fadeTween?.Kill();
            fadeTween = DOTween.To(() => alpha, ApplyAlpha, target, fadeDuration).SetEase(Ease.OutCubic);
        }

        private void ApplyAlpha(float a)
        {
            alpha = a;
            if (instanceMaterial != null && instanceMaterial.HasProperty(BaseColorId))
            {
                Color c = baseColor;
                c.a = baseColor.a * a;
                instanceMaterial.SetColor(BaseColorId, c);
            }
            if (lineRenderer != null)
            {
                Color lc = lineBaseColor;
                lc.a = lineBaseColor.a * a;
                lineRenderer.startColor = lc;
                lineRenderer.endColor = lc;
            }
        }
    }
}
