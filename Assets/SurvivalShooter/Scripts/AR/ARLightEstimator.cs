using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace SurvivalShooter.AR
{
    /// <summary>
    /// Matches the virtual directional light to the real room brightness so enemies blend
    /// into the camera feed (uses AR Foundation light estimation when the device supports it).
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class ARLightEstimator : MonoBehaviour
    {
        [SerializeField] private ARCameraManager cameraManager;
        [SerializeField] private float minIntensity = 0.55f;
        [SerializeField] private float maxIntensity = 1.6f;

        private Light targetLight;
        private float baseIntensity;

        private void Awake()
        {
            targetLight = GetComponent<Light>();
            baseIntensity = targetLight.intensity;
            if (cameraManager == null) cameraManager = FindAnyObjectByType<ARCameraManager>();
        }

        private void OnEnable()
        {
            if (cameraManager != null) cameraManager.frameReceived += OnFrame;
        }

        private void OnDisable()
        {
            if (cameraManager != null) cameraManager.frameReceived -= OnFrame;
        }

        private void OnFrame(ARCameraFrameEventArgs args)
        {
            if (args.lightEstimation.averageBrightness.HasValue)
            {
                float b = args.lightEstimation.averageBrightness.Value;
                float target = Mathf.Clamp(baseIntensity * (0.5f + b * 1.5f), minIntensity, maxIntensity);
                targetLight.intensity = Mathf.Lerp(targetLight.intensity, target, 0.1f);
            }
            if (args.lightEstimation.colorCorrection.HasValue)
            {
                Color c = args.lightEstimation.colorCorrection.Value;
                targetLight.color = Color.Lerp(targetLight.color, Color.Lerp(Color.white, c, 0.5f), 0.1f);
            }
        }
    }
}
