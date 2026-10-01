using UnityEngine;

namespace SurvivalShooter.FX
{
    /// <summary>Slow spin around the world Y axis (enemy type rings).</summary>
    public class SpinY : MonoBehaviour
    {
        [SerializeField] private float degreesPerSecond = 60f;

        private void Update() => transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
    }
}
