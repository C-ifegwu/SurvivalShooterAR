using UnityEngine;

namespace SurvivalShooter.FX
{
    /// <summary>Keeps a quad facing the camera (glows, muzzle flashes).</summary>
    public class Billboard : MonoBehaviour
    {
        private Camera cam;

        private void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position, cam.transform.up);
        }
    }
}
