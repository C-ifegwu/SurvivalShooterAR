using UnityEngine;
using SurvivalShooter.Pooling;

namespace SurvivalShooter.FX
{
    /// <summary>
    /// Pooled particle effect (hit sparks, spawn portal, death burst). Returns itself to the pool
    /// once all particle systems have finished playing.
    /// </summary>
    public class PooledEffect : MonoBehaviour, IPoolable
    {
        [SerializeField] private float lifetime = 1.2f;
        private ParticleSystem[] systems;
        private ObjectPool<PooledEffect> ownerPool;
        private float age;
        private bool running;

        private void Awake()
        {
            systems = GetComponentsInChildren<ParticleSystem>(true);
        }

        public void BindPool(ObjectPool<PooledEffect> pool) => ownerPool = pool;

        public void OnSpawned()
        {
            age = 0f;
            running = true;
            foreach (var ps in systems)
            {
                ps.Clear(true);
                ps.Play(true);
            }
        }

        public void OnDespawned()
        {
            running = false;
            foreach (var ps in systems) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void Update()
        {
            if (!running) return;
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                running = false;
                if (ownerPool != null) ownerPool.Release(this);
                else gameObject.SetActive(false);
            }
        }
    }
}
