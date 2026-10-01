using System;
using System.Collections.Generic;
using UnityEngine;
using SurvivalShooter.Core;

namespace SurvivalShooter.Enemies
{
    /// <summary>
    /// Factory pattern: callers ask for an EnemyType; the factory knows which prefab to build,
    /// where to parent it and how to initialise it. The spawner never touches prefabs directly.
    /// </summary>
    public class EnemyFactory : Singleton<EnemyFactory>
    {
        [Serializable]
        public struct Entry
        {
            public EnemyType type;
            public EnemyBase prefab;
        }

        [SerializeField] private Entry[] prefabs;

        private readonly Dictionary<EnemyType, EnemyBase> map = new Dictionary<EnemyType, EnemyBase>();

        protected override void OnSingletonAwake()
        {
            foreach (var e in prefabs)
            {
                if (e.prefab != null) map[e.type] = e.prefab;
            }
        }

        public EnemyBase Create(EnemyType type, Vector3 position, Quaternion rotation, Transform parent,
                                DifficultyConfig config, Transform target, float floorHeight)
        {
            if (!map.TryGetValue(type, out EnemyBase prefab))
            {
                Debug.LogError($"[EnemyFactory] No prefab registered for {type}");
                return null;
            }

            EnemyBase enemy = Instantiate(prefab, position, rotation, parent);
            enemy.name = $"{type}_{Time.frameCount}";
            enemy.Initialize(config, target, floorHeight);
            return enemy;
        }
    }
}
