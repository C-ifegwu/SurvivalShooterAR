using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using DG.Tweening;
using SurvivalShooter.Core;

namespace SurvivalShooter.AR
{
    /// <summary>
    /// Handles AR Plane detection, Placement Reticle tracking, and Tap-to-Place functionality.
    /// Strictly guarantees single-placement constraint and disables plane detection post-placement.
    /// Includes complete Unity Editor Play Mode simulation for rapid desktop testing.
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
        private Tween reticlePulseTween;

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

                // Add visual reticle pulsing animation
                reticlePulseTween = placementIndicatorInstance.transform.DOScale(1.08f, 0.75f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetUpdate(true);
            }
        }

        private void OnDestroy()
        {
            reticlePulseTween?.Kill();
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
            bool hitPlane = false;

            if (raycastManager != null)
            {
                Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                if (raycastManager.Raycast(screenCenter, raycastHits, TrackableType.PlaneWithinPolygon))
                {
                    currentPlacementPose = raycastHits[0].pose;
                    hitPlane = true;
                }
            }

            #if UNITY_EDITOR || UNITY_STANDALONE
            // Desktop Editor Play Mode Fallback: Automatically simulate floor plane in front of camera
            if (!hitPlane)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 forwardFloor = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                    if (forwardFloor.sqrMagnitude < 0.01f) forwardFloor = Vector3.forward;

                    currentPlacementPose.position = cam.transform.position + forwardFloor * 2.2f + Vector3.down * 0.8f;
                    currentPlacementPose.rotation = Quaternion.LookRotation(forwardFloor, Vector3.up);
                    hitPlane = true;
                }
            }
            #endif

            SetPlaneDetected(hitPlane);
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
                if (!placementIndicatorInstance.activeSelf)
                {
                    placementIndicatorInstance.SetActive(true);
                }
                placementIndicatorInstance.transform.SetPositionAndRotation(currentPlacementPose.position, currentPlacementPose.rotation);
            }
            else
            {
                if (placementIndicatorInstance.activeSelf)
                {
                    placementIndicatorInstance.SetActive(false);
                }
            }
        }

        private void CheckForTapToPlace()
        {
            if (!isPlaneDetected || isObjectPlaced) return;

            bool tapTriggered = false;

            #if UNITY_EDITOR || UNITY_STANDALONE
            // In Editor, click anywhere in game view or press Space/Enter to place
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    tapTriggered = true;
                }
            }
            else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                tapTriggered = true;
            }
            #else
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(touch.fingerId))
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
            Debug.Log($"[ARPlacementManager] Combat Zone successfully anchored at {position}. Locking plane tracking.");

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

            // Dispatch global events
            GameEvents.TriggerGameWorldPlaced(position);

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