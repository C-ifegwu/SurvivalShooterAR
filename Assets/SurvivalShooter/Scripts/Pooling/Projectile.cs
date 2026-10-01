using UnityEngine;
using SurvivalShooter.Core;

namespace SurvivalShooter.Pooling
{
    /// <summary>
    /// Pooled projectile used by both the player and the Shooter enemy.
    /// Movement uses a sphere-sweep each frame (no Rigidbody needed, no tunnelling at high speed).
    /// Everything is reset in <see cref="OnSpawned"/> so reused bullets behave like new ones.
    /// </summary>
    public class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField] private float defaultSpeed = 18f;
        [SerializeField] private float radius = 0.06f;
        [SerializeField] private float maxLifetime = 2.5f;
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private Transform visual;
        [SerializeField] private Transform glow;
        [SerializeField] private float growTime = 0.1f;

        private static readonly RaycastHit[] HitBuffer = new RaycastHit[16];

        private ObjectPool<Projectile> ownerPool;
        private Team team;
        private int damage;
        private float speed;
        private float age;
        private bool released;
        private Vector3 visualBaseScale = Vector3.one;
        private Vector3 glowBaseScale = Vector3.one;

        public Team Team => team;

        private void Awake()
        {
            if (trail == null) trail = GetComponentInChildren<TrailRenderer>();
            if (visual != null) visualBaseScale = visual.localScale;
            if (glow != null) glowBaseScale = glow.localScale;
        }

        public void BindPool(ObjectPool<Projectile> pool) => ownerPool = pool;

        /// <summary>Configure a freshly spawned projectile.</summary>
        public void Launch(Team owner, int dmg, float projectileSpeed)
        {
            team = owner;
            damage = dmg;
            speed = projectileSpeed > 0f ? projectileSpeed : defaultSpeed;
        }

        public void OnSpawned()
        {
            age = 0f;
            released = false;
            speed = defaultSpeed;
            if (visual != null) visual.localScale = visualBaseScale * 0.3f;
            if (glow != null) glow.localScale = glowBaseScale * 0.1f;
            if (trail != null)
            {
                trail.Clear();
                trail.emitting = true;
            }
        }

        public void OnDespawned()
        {
            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }
        }

        private void Update()
        {
            if (released) return;

            float dt = Time.deltaTime;
            age += dt;
            if (age >= maxLifetime)
            {
                Despawn();
                return;
            }

            // Grow in so the glow never flares right in front of the camera lens
            float k = Mathf.Clamp01(age / growTime);
            if (visual != null) visual.localScale = visualBaseScale * Mathf.Lerp(0.3f, 1f, k);
            if (glow != null) glow.localScale = glowBaseScale * Mathf.Lerp(0.1f, 1f, k * k);

            Vector3 start = transform.position;
            Vector3 dir = transform.forward;
            float distance = speed * dt;

            int count = Physics.SphereCastNonAlloc(start, radius, dir, HitBuffer, distance, ~0, QueryTriggerInteraction.Ignore);
            if (count > 0 && ResolveHits(count, dir))
            {
                return;
            }

            transform.position = start + dir * distance;
        }

        private bool ResolveHits(int count, Vector3 dir)
        {
            // Sort nearest first (tiny insertion sort, no allocations)
            for (int i = 1; i < count; i++)
            {
                RaycastHit key = HitBuffer[i];
                int j = i - 1;
                while (j >= 0 && HitBuffer[j].distance > key.distance)
                {
                    HitBuffer[j + 1] = HitBuffer[j];
                    j--;
                }
                HitBuffer[j + 1] = key;
            }

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = HitBuffer[i];
                if (hit.collider == null) continue;

                IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
                if (target != null)
                {
                    if (target.Team == team || !target.IsAlive) continue; // no friendly fire, pass through
                    Vector3 point = hit.point == Vector3.zero ? hit.collider.bounds.center : hit.point;
                    target.TakeDamage(new DamageInfo(damage, point, dir, team));
                    if (PoolManager.HasInstance && team == Team.Player)
                        PoolManager.Instance.SpawnEffect(EffectType.EnemyImpact, point, Quaternion.LookRotation(-dir));
                    Despawn();
                    return true;
                }

                // Environment (AR plane collider etc.)
                if (PoolManager.HasInstance)
                    PoolManager.Instance.SpawnEffect(EffectType.HitSpark, hit.point, Quaternion.LookRotation(hit.normal));
                Despawn();
                return true;
            }
            return false;
        }

        public void Despawn()
        {
            if (released) return;
            released = true;
            if (ownerPool != null) ownerPool.Release(this);
            else gameObject.SetActive(false);
        }
    }
}
