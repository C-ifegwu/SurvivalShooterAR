using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using SurvivalShooter.Core;

namespace SurvivalShooter.AR
{
    /// <summary>
    /// Handles AR Plane detection, Placement Reticle tracking, and Tap-to-Place functionality.
    /// Strictly guarantees single-placement constraint and disables plane detection post-placement.
    /// </summary>
    public class ARPlacementManager : MonoBehaviour
    {
        public static ARPlacementManager Instance { get; private set; }

        [Header("AR Foundation References")]
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARPlaneManager planeManager;

        [Header("Placement Visuals")]
        [SerializeField] private GameObject placementIndicatorPrefab;
        [SerializeField] private GameObject combatArenaPrefab;

        private GameObject placementIndicatorInstance;
        private Pose currentPlacementPose;
        private bool isPlaneDetected;
        private bool isObjectPlaced;
        private readonly List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();

        public bool IsObjectPlaced => isObjectPlaced;
        public bool IsPlaneDetected => isPlaneDetected;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (raycastManager == null)
            {
                raycastManager = FindFirstObjectByType<ARRaycastManager>();
            }

            if (planeManager == null)
            {
                planeManager = FindFirstObjectByType<ARPlaneManager>();
            }
        }

        private void Start()
        {
            if (placementIndicatorPrefab != null)
            {
                placementIndicatorInstance = Instantiate(placementIndicatorPrefab);
                placementIndicatorInstance.SetActive(false);
            }
        }

        private void Update()
        {
            if (isObjectPlaced)
            {
                if (placementIndicatorInstance != null && placementIndicatorInstance.activeSelf)
                {
                    placementIndicatorInstance.SetActive(false);
                }
                return;
            }

            UpdatePlacementPose();
            UpdatePlacementIndicator();
            CheckForTapToPlace();
        }

        private void UpdatePlacementPose()
        {
            if (raycastManager == null)
            {
                // Fallback for editor simulation
                SimulateEditorPlacementPose();
                return;
            }

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            // Raycast against horizontal planes only
            if (raycastManager.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinPolygon))
            {
                currentPlacementPose = raycastHits[0].pose;
                SetPlaneDetected(true);
            }
            else
            {
                SetPlaneDetected(false);
            }
        }

        private void SimulateEditorPlacementPose()
        {
            #if UNITY_EDITOR
            // In editor play mode, place a simulated pose 2 meters ahead
            Camera cam = Camera.main;
            if (cam != null)
            {
                currentPlacementPose.position = cam.transform.position + cam.transform.forward * 2.0f + Vector3.down * 1.0f;
                currentPlacementPose.rotation = Quaternion.identity;
                SetPlaneDetected(true);
            }
            #endif
        }

        private void SetPlaneDetected(bool detected)
        {
            if (isPlaneDetected != detected)
            {
                isPlaneDetected = detected;
                GameEvents.TriggerPlaneDetectedStatusChanged(isPlaneDetected);
            }
        }

        private void UpdatePlacementIndicator()
        {
            if (placementIndicatorInstance == null) return;

            if (isPlaneDetected && !isObjectPlaced)
            {
                placementIndicatorInstance.SetActive(true);
                placementIndicatorInstance.transform.SetPositionAndRotation(currentPlacementPose.position, currentPlacementPose.rotation);
            }
            else
            {
                placementIndicatorInstance.SetActive(false);
            }
        }

        private void CheckForTapToPlace()
        {
            if (!isPlaneDetected || isObjectPlaced) return;

            bool tapTriggered = false;

            #if UNITY_EDITOR || UNITY_STANDALONE
            if (Input.GetMouseButtonDown(0))
            {
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    tapTriggered = true;
                }
            }
            #else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    if (!EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                    {
                        tapTriggered = true;
                    }
                }
            }
            #endif

            if (tapTriggered)
            {
                PlaceGameWorld(currentPlacementPose.position, currentPlacementPose.rotation);
            }
        }

        public void PlaceGameWorld(Vector3 position, Quaternion rotation)
        {
            if (isObjectPlaced) return; // Strict single-instance constraint

            isObjectPlaced = true;
            Debug.Log($"[ARPlacementManager] Game World placed at {position}. Freezing plane detection.");

            // Spawn visual arena bounds/perimeter if assigned
            if (combatArenaPrefab != null)
            {
                Instantiate(combatArenaPrefab, position, rotation);
            }

            // Hide placement reticle
            if (placementIndicatorInstance != null)
            {
                placementIndicatorInstance.SetActive(false);
            }

            // Stop detecting new planes to lock the combat environment
            if (planeManager != null)
            {
                planeManager.requestedDetectionMode = PlaneDetectionMode.None;
            }

            // Dispatch global event
            GameEvents.TriggerGameWorldPlaced(position);

            // Notify GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlacementComplete();
            }
        }

        public void ResetPlacement()
        {
            isObjectPlaced = false;
            isPlaneDetected = false;

            if (planeManager != null)
            {
                planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
            }

            if (placementIndicatorInstance != null)
            {
                placementIndicatorInstance.SetActive(false);
            }
        }

        public void ConfigurePrefabs(GameObject reticle, GameObject arena = null)
        {
            placementIndicatorPrefab = reticle;
            combatArenaPrefab = arena;
        }
    }
}