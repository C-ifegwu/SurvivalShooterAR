using UnityEngine;
using SurvivalShooter.Core;

namespace SurvivalShooter.Pooling
{
    /// <summary>
    /// Pooled projectile implementation adhering strictly to zero runtime instantiation/destruction.
    /// Resets all state, physics, and visual trails on reuse.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PooledProjectile : MonoBehaviour, IPooledObject
    {
        [Header("Projectile Settings")]
        [SerializeField] private float speed = 18f;
        [SerializeField] private int damage = 20;
        [SerializeField] private float maxLifetime = 3.5f;
        [SerializeField] private bool isPlayerProjectile = true;

        [Header("Visuals & FX")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private GameObject impactEffectPrefab;

        private float currentLifetime;
        private Collider projectileCollider;
        private bool isReturned;

        public string PoolTag { get; set; }

        public int Damage
        {
            get => damage;
            set => damage = value;
        }

        public float Speed
        {
            get => speed;
            set => speed = value;
        }

        public bool IsPlayerProjectile
        {
            get => isPlayerProjectile;
            set => isPlayerProjectile = value;
        }

        private void Awake()
        {
            projectileCollider = GetComponent<Collider>();
            if (projectileCollider != null)
            {
                projectileCollider.isTrigger = true;
            }

            if (trailRenderer == null)
            {
                trailRenderer = GetComponentInChildren<TrailRenderer>();
            }
        }

        public void OnObjectSpawn()
        {
            currentLifetime = 0f;
            isReturned = false;

            if (projectileCollider != null)
            {
                projectileCollider.enabled = true;
            }

            if (trailRenderer != null)
            {
                trailRenderer.Clear();
                trailRenderer.emitting = true;
            }
        }

        private void Update()
        {
            if (isReturned) return;

            // Move projectile forward in world space
            transform.Translate(Vector3.forward * (speed * Time.deltaTime), Space.Self);

            currentLifetime += Time.deltaTime;
            if (currentLifetime >= maxLifetime)
            {
                ReturnToPool();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isReturned) return;

            // Ignore triggers such as plane boundaries or placement reticles
            if (other.isTrigger) return;

            // Check damageable target
            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target != null && !target.IsDead)
            {
                // Verify friendly fire rules
                bool isTargetPlayer = other.CompareTag("Player") || other.GetComponentInParent<SurvivalShooter.Player.PlayerHealth>() != null;
                
                // Player bullets only hit enemies; Enemy bullets only hit player
                if (isPlayerProjectile && !isTargetPlayer)
                {
                    target.TakeDamage(damage, transform.position, transform.forward);
                    ReturnToPool();
                    return;
                }
                else if (!isPlayerProjectile && isTargetPlayer)
                {
                    target.TakeDamage(damage, transform.position, transform.forward);
                    ReturnToPool();
                    return;
                }
            }

            // Hit environment or obstacle
            if (!other.CompareTag("Projectile") && !other.CompareTag("ARPlane"))
            {
                ReturnToPool();
            }
        }

        public void ReturnToPool()
        {
            if (isReturned) return;
            isReturned = true;

            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
            }

            if (projectileCollider != null)
            {
                projectileCollider.enabled = false;
            }

            ObjectPoolManager.Instance.ReturnToPool(gameObject, PoolTag);
        }
    }
}
