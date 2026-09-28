using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace SurvivalShooter.AR
{
    /// <summary>
    /// Custom Plane Tracker replacing Unity default visualizer.
    /// Displays student name "Chibueze Victor Ifegwu" prominently via custom textured mesh.
    /// Only visible when the plane tracking state is actively Tracking.
    /// </summary>
    [RequireComponent(typeof(ARPlane))]
    [RequireComponent(typeof(MeshRenderer))]
    public class CustomPlaneVisualizer : MonoBehaviour
    {
        [Header("Student Branding & Visuals")]
        [SerializeField] private Material customPlaneMaterial;
        [SerializeField] private string studentName = "Chibueze Victor Ifegwu";

        private ARPlane arPlane;
        private MeshRenderer meshRenderer;

        private void Awake()
        {
            arPlane = GetComponent<ARPlane>();
            meshRenderer = GetComponent<MeshRenderer>();

            if (customPlaneMaterial != null && meshRenderer != null)
            {
                meshRenderer.material = customPlaneMaterial;
            }
        }

        private void OnEnable()
        {
            arPlane.boundaryChanged += OnBoundaryChanged;
            UpdateVisibility();
        }

        private void OnDisable()
        {
            arPlane.boundaryChanged -= OnBoundaryChanged;
        }

        private void Update()
        {
            UpdateVisibility();
        }

        private void OnBoundaryChanged(ARPlaneBoundaryChangedEventArgs eventArgs)
        {
            UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (arPlane == null || meshRenderer == null) return;

            // Only visible when actively tracking and horizontal
            bool isTracking = (arPlane.trackingState == TrackingState.Tracking);
            bool isHorizontal = (arPlane.alignment == PlaneAlignment.HorizontalUp || arPlane.alignment == PlaneAlignment.HorizontalDown);

            meshRenderer.enabled = isTracking && isHorizontal;
        }
    }
}
