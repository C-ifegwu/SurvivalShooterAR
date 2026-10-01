using UnityEngine;
using SurvivalShooter.Core;
using SurvivalShooter.FX;

namespace SurvivalShooter.Pooling
{
    /// <summary>
    /// Owns every runtime pool (projectiles + visual effects). Pools are pre-initialised in Awake,
    /// before the first frame of gameplay.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class PoolManager : Singleton<PoolManager>
    {
        [Header("Projectile Prefabs")]
        [SerializeField] private Projectile playerProjectilePrefab;
        [SerializeField] private Projectile enemyProjectilePrefab;
        [SerializeField] private int playerProjectileCount = 40;
        [SerializeField] private int enemyProjectileCount = 30;

        [Header("Effect Prefabs")]
        [SerializeField] private PooledEffect hitSparkPrefab;
        [SerializeField] private PooledEffect enemyImpactPrefab;
        [SerializeField] private PooledEffect spawnPortalPrefab;
        [SerializeField] private PooledEffect deathBurstPrefab;
        [SerializeField] private int effectCount = 12;

        private ObjectPool<Projectile> playerProjectiles;
        private ObjectPool<Projectile> enemyProjectiles;
        private ObjectPool<PooledEffect> hitSparks;
        private ObjectPool<PooledEffect> enemyImpacts;
        private ObjectPool<PooledEffect> spawnPortals;
        private ObjectPool<PooledEffect> deathBursts;

        public ObjectPool<Projectile> PlayerProjectiles => playerProjectiles;
        public ObjectPool<Projectile> EnemyProjectiles => enemyProjectiles;

        protected override void OnSingletonAwake()
        {
            Transform root = new GameObject("[Pools]").transform;
            root.SetParent(transform, false);

            playerProjectiles = CreatePool(playerProjectilePrefab, root, playerProjectileCount, "PlayerProjectiles");
            enemyProjectiles = CreatePool(enemyProjectilePrefab, root, enemyProjectileCount, "EnemyProjectiles");
            hitSparks = CreatePool(hitSparkPrefab, root, effectCount, "HitSparks");
            enemyImpacts = CreatePool(enemyImpactPrefab, root, effectCount, "EnemyImpacts");
            spawnPortals = CreatePool(spawnPortalPrefab, root, effectCount, "SpawnPortals");
            deathBursts = CreatePool(deathBurstPrefab, root, effectCount, "DeathBursts");
        }

        private ObjectPool<T> CreatePool<T>(T prefab, Transform root, int count, string label) where T : Component, IPoolable
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[PoolManager] Missing prefab for pool '{label}'.");
                return null;
            }
            Transform container = new GameObject(label).transform;
            container.SetParent(root, false);
            var pool = new ObjectPool<T>(prefab, container, count);
            return pool;
        }

        public Projectile SpawnProjectile(Team team, Vector3 position, Quaternion rotation)
        {
            var pool = team == Team.Player ? playerProjectiles : enemyProjectiles;
            if (pool == null) return null;
            Projectile p = pool.Get(position, rotation);
            p.BindPool(pool);
            return p;
        }

        public void SpawnEffect(EffectType type, Vector3 position, Quaternion rotation)
        {
            ObjectPool<PooledEffect> pool = type switch
            {
                EffectType.HitSpark => hitSparks,
                EffectType.EnemyImpact => enemyImpacts,
                EffectType.SpawnPortal => spawnPortals,
                EffectType.DeathBurst => deathBursts,
                _ => null
            };
            if (pool == null) return;
            PooledEffect fx = pool.Get(position, rotation);
            fx.BindPool(pool);
        }

        /// <summary>Returns every active projectile and effect to its pool (round end / restart).</summary>
        public void ReleaseAll()
        {
            playerProjectiles?.ReleaseAll();
            enemyProjectiles?.ReleaseAll();
            hitSparks?.ReleaseAll();
            enemyImpacts?.ReleaseAll();
            spawnPortals?.ReleaseAll();
            deathBursts?.ReleaseAll();
        }
    }

    public enum EffectType
    {
        HitSpark,
        EnemyImpact,
        SpawnPortal,
        DeathBurst
    }
}
