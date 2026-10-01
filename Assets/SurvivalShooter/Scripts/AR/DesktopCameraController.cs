using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalShooter.AR
{
    /// <summary>
    /// Editor-only testing helper.
    /// • No AR session: right-mouse look + WASD move, so the game runs on a plain desktop.
    /// • XR Simulation running: arrow keys (and I/J/K/L) look around without holding the right
    ///   mouse button, and <see cref="RotateSimulationCamera"/> lets automated tests aim the camera.
    /// Does nothing in device builds.
    /// </summary>
    public class DesktopCameraController : MonoBehaviour
    {
        [SerializeField] private float lookSpeed = 0.15f;
        [SerializeField] private float keyLookSpeed = 70f;
        [SerializeField] private float moveSpeed = 1.6f;
        private float yaw, pitch;

#if UNITY_EDITOR
        private static Transform simCamera;
#endif

        private void Start()
        {
            Vector3 e = transform.eulerAngles;
            yaw = e.y;
            pitch = e.x > 180f ? e.x - 360f : e.x;
        }

        /// <summary>Rotate the XR Simulation camera (Editor only). Returns false if not available.</summary>
        public static bool RotateSimulationCamera(float pitchDelta, float yawDelta)
        {
#if UNITY_EDITOR
            if (simCamera == null)
            {
                var provider = FindAnyObjectByType<UnityEngine.XR.Simulation.SimulationCameraPoseProvider>();
                if (provider == null) return false;
                simCamera = provider.transform;
            }
            Vector3 e = simCamera.rotation.eulerAngles;
            float p = e.x > 180f ? e.x - 360f : e.x;
            p = Mathf.Clamp(p + pitchDelta, -80f, 80f);
            simCamera.rotation = Quaternion.Euler(p, e.y + yawDelta, 0f);
            return true;
#else
            return false;
#endif
        }

        private void LateUpdate()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            var mouse = Mouse.current;
            var kb = Keyboard.current;
            if (kb == null) return;

            if (ARPlacementManager.IsXRRunning)
            {
                float dp = 0f, dy = 0f;
                if (kb.upArrowKey.isPressed || kb.iKey.isPressed) dp -= 1f;
                if (kb.downArrowKey.isPressed || kb.kKey.isPressed) dp += 1f;
                if (kb.leftArrowKey.isPressed || kb.jKey.isPressed) dy -= 1f;
                if (kb.rightArrowKey.isPressed || kb.lKey.isPressed) dy += 1f;
                if (dp != 0f || dy != 0f)
                    RotateSimulationCamera(dp * keyLookSpeed * Time.unscaledDeltaTime, dy * keyLookSpeed * Time.unscaledDeltaTime);
                return;
            }

            if (mouse == null) return;
            if (mouse.rightButton.isPressed)
            {
                Vector2 d = mouse.delta.ReadValue();
                yaw += d.x * lookSpeed;
                pitch = Mathf.Clamp(pitch - d.y * lookSpeed, -75f, 75f);
            }
            if (kb.upArrowKey.isPressed) pitch -= keyLookSpeed * Time.unscaledDeltaTime;
            if (kb.downArrowKey.isPressed) pitch += keyLookSpeed * Time.unscaledDeltaTime;
            if (kb.leftArrowKey.isPressed) yaw -= keyLookSpeed * Time.unscaledDeltaTime;
            if (kb.rightArrowKey.isPressed) yaw += keyLookSpeed * Time.unscaledDeltaTime;
            pitch = Mathf.Clamp(pitch, -75f, 75f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

            Vector3 move = Vector3.zero;
            if (kb.wKey.isPressed) move += Vector3.forward;
            if (kb.sKey.isPressed) move += Vector3.back;
            if (kb.aKey.isPressed) move += Vector3.left;
            if (kb.dKey.isPressed) move += Vector3.right;
            if (move != Vector3.zero)
            {
                Vector3 world = Quaternion.Euler(0f, yaw, 0f) * move.normalized;
                transform.position += world * (moveSpeed * Time.unscaledDeltaTime);
            }
#endif
        }
    }
}
