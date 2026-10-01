using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using SurvivalShooter.Audio;
using SurvivalShooter.Core;

namespace SurvivalShooter.AR
{
    /// <summary>
    /// Horizontal plane detection + tap-to-place.
    /// - A reticle follows the detected plane under the screen centre.
    /// - One tap places the combat arena; the arena is parented to an ARAnchor so it stays locked
    ///   to the real world. Further taps are ignored (single-instance rule).
    /// - After placement, detection of new planes is switched off (optional requirement).
    /// In the Editor without XR Simulation, a virtual floor is used so the loop can still be tested.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class ARPlacementManager : Singleton<ARPlacementManager>
    {
        [Header("AR Foundation")]
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private ARAnchorManager anchorManager;
        [SerializeField] private Camera arCamera;

        [Header("Visuals")]
        [SerializeField] private PlacementReticle reticlePrefab;
        [SerializeField] private GameObject arenaPrefab;

#pragma warning disable 0414 // only used in Editor/Standalone builds
        [Header("Editor Fallback")]
        [SerializeField] private float fallbackFloorOffset = 1.4f;
        [SerializeField] private float fallbackDelay = 1.5f;
#pragma warning restore 0414

        private static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();

        private PlacementReticle reticle;
        private GameObject anchorObject;
        private Transform arenaRoot;
        private bool placementEnabled;
        private bool hasPose;
        private Pose pose;
        private float scanTimer;
        private bool usingFallback;

        public Action ArenaPlacedCallback { get; set; }
        public bool IsArenaPlaced => arenaRoot != null;
        public Transform ArenaRoot => arenaRoot;
        public float FloorHeight { get; private set; }
        public bool UsingDesktopFallback => usingFallback;
        public Camera ARCamera => arCamera != null ? arCamera : Camera.main;

        protected override void OnSingletonAwake()
        {
            if (arCamera == null) arCamera = Camera.main;
            if (raycastManager == null) raycastManager = FindAnyObjectByType<ARRaycastManager>();
            if (planeManager == null) planeManager = FindAnyObjectByType<ARPlaneManager>();
            if (anchorManager == null) anchorManager = FindAnyObjectByType<ARAnchorManager>();

            if (reticlePrefab != null)
            {
                reticle = Instantiate(reticlePrefab);
                reticle.name = "PlacementReticle";
                reticle.SetVisible(false, true);
            }
        }

        /// <summary>True when a real AR (or XR Simulation) session is running.</summary>
        public static bool IsXRRunning
        {
            get
            {
                var settings = XRGeneralSettings.Instance;
                return settings != null && settings.Manager != null && settings.Manager.activeLoader != null
                       && ARSession.state >= ARSessionState.SessionInitializing;
            }
        }

        public void SetPlacementEnabled(bool enabled)
        {
            placementEnabled = enabled && !IsArenaPlaced;
            scanTimer = 0f;
            if (!placementEnabled)
            {
                if (reticle != null) reticle.SetVisible(false);
                SetHasPose(false, true);
            }
        }

        private void Update()
        {
            if (!placementEnabled || IsArenaPlaced) return;

            scanTimer += Time.deltaTime;
            UpdatePose();

            if (reticle != null)
            {
                reticle.SetVisible(hasPose);
                if (hasPose) reticle.Follow(pose);
            }

            if (hasPose && PointerInput.WorldTapThisFrame(out _))
            {
                PlaceArena(pose);
            }
        }

        private void UpdatePose()
        {
            bool found = false;
            Camera cam = ARCamera;

            if (raycastManager != null && cam != null && IsXRRunning)
            {
                Vector2 centre = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                if (raycastManager.Raycast(centre, Hits, TrackableType.PlaneWithinPolygon))
                {
                    foreach (var hit in Hits)
                    {
                        if (hit.trackable is ARPlane plane && plane.alignment != PlaneAlignment.HorizontalUp) continue;
                        pose = hit.pose;
                        found = true;
                        break;
                    }
                }
            }

#if UNITY_EDITOR || UNITY_STANDALONE
            // Desktop fallback: no AR session at all → virtual floor under the camera.
            usingFallback = !IsXRRunning && scanTimer > fallbackDelay;
            if (!found && usingFallback && cam != null)
            {
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
                Plane floor = new Plane(Vector3.up, new Vector3(0f, cam.transform.position.y - fallbackFloorOffset, 0f));
                if (floor.Raycast(ray, out float dist) && dist < 8f)
                {
                    pose = new Pose(ray.GetPoint(dist), Quaternion.identity);
                    found = true;
                }
            }
#endif
            if (found && cam != null)
            {
                // Face the arena towards the player.
                Vector3 toCam = cam.transform.position - pose.position;
                toCam.y = 0f;
                if (toCam.sqrMagnitude > 0.001f) pose.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
            }

            SetHasPose(found);
        }

        private void SetHasPose(bool value, bool force = false)
        {
            if (hasPose == value && !force) return;
            hasPose = value;
            GameEvents.RaisePlaneStatusChanged(value);
            if (value && AudioManager.HasInstance) AudioManager.Instance.Play(SoundId.PlaneFound);
        }

        public void PlaceArena(Pose placePose)
        {
            if (IsArenaPlaced) return; // single-instance constraint

            anchorObject = new GameObject("ArenaAnchor");
            anchorObject.transform.SetPositionAndRotation(placePose.position, placePose.rotation);
            if (IsXRRunning && anchorManager != null)
            {
                anchorObject.AddComponent<ARAnchor>(); // world-locks the arena
            }

            GameObject arena = arenaPrefab != null
                ? Instantiate(arenaPrefab, anchorObject.transform)
                : new GameObject("CombatArena");
            arena.transform.SetParent(anchorObject.transform, false);
            arena.transform.localPosition = Vector3.zero;
            arena.transform.localRotation = Quaternion.identity;
            arenaRoot = arena.transform;
            FloorHeight = placePose.position.y;

            placementEnabled = false;
            if (reticle != null) reticle.SetVisible(false);

            // Optional requirement: stop detecting new planes once the game is placed.
            // Disabling the manager stops the plane subsystem but keeps the already-detected
            // planes (and their colliders) frozen in place, so the tracker stays visible under the arena.
            if (planeManager != null) planeManager.enabled = false;
            CustomPlaneVisualizer.SetArenaMode(true);

            if (AudioManager.HasInstance) AudioManager.Instance.Play(SoundId.ArenaPlaced);
            GameEvents.RaiseArenaPlaced(arenaRoot);
            ArenaPlacedCallback?.Invoke();
        }

        public void ResetPlacement()
        {
            if (anchorObject != null) Destroy(anchorObject);
            anchorObject = null;
            arenaRoot = null;
            if (planeManager != null)
            {
                planeManager.requestedDetectionMode = PlaneDetectionMode.Horizontal;
                planeManager.enabled = true;
            }
            CustomPlaneVisualizer.SetArenaMode(false);
        }

        /// <summary>Arena floor point projected under the given world position.</summary>
        public Vector3 ProjectToFloor(Vector3 world) => new Vector3(world.x, FloorHeight, world.z);
    }
}
