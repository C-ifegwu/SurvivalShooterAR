using UnityEngine;
using SurvivalShooter.Audio;
using SurvivalShooter.Core;
using SurvivalShooter.Enemies;
using SurvivalShooter.Pooling;

namespace SurvivalShooter.Player
{
    /// <summary>
    /// First-person shooting. Bullets always come from the Object Pool (no Instantiate/Destroy).
    /// Fire by holding the FIRE button or by tapping/holding anywhere on the screen.
    /// Bullets leave from a virtual "hand" muzzle and converge on the crosshair; a light aim assist
    /// helps on small phone screens.
    /// </summary>
    public class PlayerShooter : Singleton<PlayerShooter>
    {
        [SerializeField] private Camera aimCamera;
        [SerializeField] private Vector3 muzzleLocalOffset = new Vector3(0.09f, -0.11f, 0.25f);
        [SerializeField] private float fireInterval = 0.16f;
        [SerializeField] private int bulletDamage = 25;
        [SerializeField] private float bulletSpeed = 16f;
        [SerializeField] private float maxAimDistance = 25f;
        [SerializeField] [Range(0f, 10f)] private float aimAssistAngle = 4.5f;

        private bool combatEnabled;
        private bool fireButtonHeld;
        private bool screenHoldActive;
        private float nextFireTime;

        public int BulletDamage => bulletDamage;
        public bool CombatEnabled => combatEnabled;

        protected override void OnSingletonAwake()
        {
            if (aimCamera == null) aimCamera = GetComponent<Camera>();
            if (aimCamera == null) aimCamera = Camera.main;
        }

        public void SetCombatEnabled(bool enabled)
        {
            combatEnabled = enabled;
            if (!enabled)
            {
                fireButtonHeld = false;
                screenHoldActive = false;
            }
        }

        /// <summary>Called by the HUD fire button (pointer down / up).</summary>
        public void SetFireButtonHeld(bool held)
        {
            fireButtonHeld = held;
            if (held) TryFire();
        }

        private void Update()
        {
            if (!combatEnabled) return;

            // Tap / hold anywhere on the world (not on UI) also fires.
            if (PointerInput.WorldTapThisFrame(out _)) screenHoldActive = true;
            if (screenHoldActive && !PointerInput.IsHeld(out _)) screenHoldActive = false;

            if (fireButtonHeld || screenHoldActive) TryFire();
        }

        public void TryFire()
        {
            if (!combatEnabled || Time.time < nextFireTime || aimCamera == null) return;
            if (!PoolManager.HasInstance) return;
            nextFireTime = Time.time + fireInterval;

            Transform cam = aimCamera.transform;
            Vector3 aimPoint = ResolveAimPoint(cam);
            Vector3 muzzle = cam.TransformPoint(muzzleLocalOffset);
            Vector3 dir = (aimPoint - muzzle).normalized;

            Projectile p = PoolManager.Instance.SpawnProjectile(Team.Player, muzzle, Quaternion.LookRotation(dir));
            if (p != null) p.Launch(Team.Player, bulletDamage, bulletSpeed);

            if (AudioManager.HasInstance) AudioManager.Instance.Play(SoundId.PlayerShoot);
            GameEvents.RaiseShotFired();
        }

        private Vector3 ResolveAimPoint(Transform cam)
        {
            Vector3 origin = cam.position;
            Vector3 forward = cam.forward;

            // Light aim assist: snap to the closest living enemy inside a small cone.
            if (EnemySpawner.HasInstance && aimAssistAngle > 0f)
            {
                EnemyBase best = null;
                float bestAngle = aimAssistAngle;
                foreach (var e in EnemySpawner.Instance.ActiveEnemies)
                {
                    if (e == null || !e.IsAlive) continue;
                    Vector3 to = e.AimPoint - origin;
                    if (to.magnitude > maxAimDistance) continue;
                    float angle = Vector3.Angle(forward, to);
                    if (angle < bestAngle)
                    {
                        bestAngle = angle;
                        best = e;
                    }
                }
                if (best != null) return best.AimPoint;
            }

            if (Physics.Raycast(origin + forward * 0.3f, forward, out RaycastHit hit, maxAimDistance, ~0, QueryTriggerInteraction.Ignore))
                return hit.point;
            return origin + forward * maxAimDistance;
        }
    }
}
