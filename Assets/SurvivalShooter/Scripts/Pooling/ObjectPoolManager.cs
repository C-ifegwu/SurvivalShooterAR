using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalShooter.Pooling
{
    /// <summary>
    /// Singleton Object Pool Manager ensuring strict zero-allocation projectile lifecycle.
    /// Pre-allocates objects during initialization and recycles them seamlessly during combat.
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        [Serializable]
        public class PoolItem
        {
            public string tag;
            public GameObject prefab;
            public int initialSize = 25;
        }

        public static ObjectPoolManager Instance { get; private set; }

        [Header("Pre-Configured Pools")]
        [SerializeField] private List<PoolItem> pools = new List<PoolItem>();

        private readonly Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<string, PoolItem> configDictionary = new Dictionary<string, PoolItem>();
        private Transform poolContainer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            poolContainer = new GameObject("[ObjectPool_Container]").transform;
            poolContainer.SetParent(transform);

            InitializePools();
        }

        private void InitializePools()
        {
            foreach (var pool in pools)
            {
                if (pool.prefab == null || string.IsNullOrEmpty(pool.tag)) continue;

                configDictionary[pool.tag] = pool;
                Queue<GameObject> objectQueue = new Queue<GameObject>();

                for (int i = 0; i < pool.initialSize; i++)
                {
                    GameObject obj = CreateNewObject(pool.tag, pool.prefab);
                    objectQueue.Enqueue(obj);
                }

                poolDictionary[pool.tag] = objectQueue;
                Debug.Log($"[ObjectPoolManager] Pre-allocated {pool.initialSize} instances of tag '{pool.tag}'.");
            }
        }

        private GameObject CreateNewObject(string tag, GameObject prefab)
        {
            GameObject obj = Instantiate(prefab, poolContainer);
            obj.SetActive(false);

            IPooledObject pooledObj = obj.GetComponent<IPooledObject>();
            if (pooledObj != null)
            {
                pooledObj.PoolTag = tag;
            }

            return obj;
        }

        public void RegisterPool(string tag, GameObject prefab, int initialSize)
        {
            if (poolDictionary.ContainsKey(tag)) return;

            PoolItem item = new PoolItem { tag = tag, prefab = prefab, initialSize = initialSize };
            configDictionary[tag] = item;
            Queue<GameObject> objectQueue = new Queue<GameObject>();

            for (int i = 0; i < initialSize; i++)
            {
                GameObject obj = CreateNewObject(tag, prefab);
                objectQueue.Enqueue(obj);
            }

            poolDictionary[tag] = objectQueue;
        }

        public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
        {
            if (!poolDictionary.ContainsKey(tag))
            {
                Debug.LogError($"[ObjectPoolManager] Pool tag '{tag}' is not registered!");
                return null;
            }

            Queue<GameObject> queue = poolDictionary[tag];
            GameObject objToSpawn;

            if (queue.Count == 0)
            {
                // Pool exhausted; expand conservatively
                objToSpawn = CreateNewObject(tag, configDictionary[tag].prefab);
                Debug.LogWarning($"[ObjectPoolManager] Pool '{tag}' auto-expanded to satisfy demand.");
            }
            else
            {
                objToSpawn = queue.Dequeue();
            }

            objToSpawn.transform.position = position;
            objToSpawn.transform.rotation = rotation;
            objToSpawn.SetActive(true);

            IPooledObject pooled = objToSpawn.GetComponent<IPooledObject>();
            if (pooled != null)
            {
                pooled.OnObjectSpawn();
            }

            return objToSpawn;
        }

        public void ReturnToPool(GameObject obj, string tag)
        {
            if (obj == null) return;

            obj.SetActive(false);
            obj.transform.SetParent(poolContainer);

            if (poolDictionary.ContainsKey(tag))
            {
                poolDictionary[tag].Enqueue(obj);
            }
            else
            {
                Debug.LogWarning($"[ObjectPoolManager] Returning object with unknown pool tag '{tag}', destroying.");
                Destroy(obj);
            }
        }

        public void ResetAllPools()
        {
            // Deactivate all active pooled objects
            foreach (Transform child in poolContainer)
            {
                if (child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        public void ConfigureDefaultPools(GameObject playerBullet, GameObject enemyBullet)
        {
            pools = new List<PoolItem>
            {
                new PoolItem { tag = "PlayerProjectile", prefab = playerBullet, initialSize = 30 },
                new PoolItem { tag = "EnemyProjectile", prefab = enemyBullet, initialSize = 30 }
            };
        }
    }
}