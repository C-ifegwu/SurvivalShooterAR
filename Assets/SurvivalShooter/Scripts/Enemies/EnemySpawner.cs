using System.Collections.Generic;
using UnityEngine;
using SurvivalShooter.Core;
using SurvivalShooter.Audio;

namespace SurvivalShooter.Enemies
{
    /// <summary>
    /// Spawner responsible for managing enemy waves on the detected AR plane.
    /// Manages active enemy collections and wipes all entities on game termination.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        public static EnemySpawner Instance { get; private set; }

        [Header("Spawn Layout Settings")]
        [SerializeField] private float minSpawnRadius = 3.0f;
        [SerializeField] private float maxSpawnRadius = 6.5f;

        [Header("Spawn Rhythm")]
        [SerializeField] private float baseSpawnInterval = 3.0f;
        [SerializeField] private float minSpawnInterval = 1.2f;
        [SerializeField] private int maxSimultaneousEnemies = 12;

        [Header("Spawn Ratios")]
        [Range(0f, 1f)]
        [SerializeField] private float shooterRatio = 0.35f; // 35% shooters, 65% melee

        private readonly List<EnemyBase> activeEnemies = new List<EnemyBase>();
        private float nextSpawnTime;
        private float currentSpawnInterval;
        private bool isSpawningActive;
        private Vector3 arenaCenter;
        private float planeYLevel;

        // Difficulty scalers
        private float speedMultiplier = 1.0f;
        private float healthMultiplier = 1.0f;
        private float damageMultiplier = 1.0f;

        public int ActiveEnemyCount => activeEnemies.Count;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnGameStateChanged += HandleGameStateChanged;
            GameEvents.OnEnemyKilled += HandleEnemyKilled;
            GameEvents.OnGameWorldPlaced += HandleWorldPlaced;
        }

        private void OnDisable()
        {
            GameEvents.OnGameStateChanged -= HandleGameStateChanged;
            GameEvents.OnEnemyKilled -= HandleEnemyKilled;
            GameEvents.OnGameWorldPlaced -= HandleWorldPlaced;
        }

        private void HandleWorldPlaced(Vector3 anchorPosition)
        {
            arenaCenter = anchorPosition;
            planeYLevel = anchorPosition.y;
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.Playing)
            {
                StartSpawning();
            }
            else
            {
                StopSpawning();
                if (state == GameState.GameOver || state == GameState.Victory || state == GameState.ScanningPlanes)
                {
                    WipeAllEnemies();
                }
            }
        }

        private void HandleEnemyKilled(EnemyType type, int score)
        {
            // Clean up null or dead references
            activeEnemies.RemoveAll(e => e == null || e.IsDead);
        }

        public void SetDifficultyParameters(float spawnInterval, float speedMult, float healthMult, float damageMult)
        {
            currentSpawnInterval = spawnInterval;
            speedMultiplier = speedMult;
            healthMultiplier = healthMult;
            damageMultiplier = damageMult;
        }

        public void StartSpawning()
        {
            isSpawningActive = true;
            nextSpawnTime = Time.time + 1.5f; // Brief grace period before first spawn
        }

        public void StopSpawning()
        {
            isSpawningActive = false;
        }

        private void Update()
        {
            if (!isSpawningActive) return;

            // Prune destroyed enemies from list
            activeEnemies.RemoveAll(e => e == null);

            if (Time.time >= nextSpawnTime && activeEnemies.Count < maxSimultaneousEnemies)
            {
                SpawnRandomEnemy();
                // Gradually increase frequency as wave progresses
                float dynamicInterval = Mathf.Max(minSpawnInterval, currentSpawnInterval - (Time.timeSinceLevelLoad * 0.005f));
                nextSpawnTime = Time.time + dynamicInterval;
            }
        }

        private void SpawnRandomEnemy()
        {
            if (EnemyFactory.Instance == null) return;

            Vector3 spawnPosition = CalculateRandomPlanePosition();
            Quaternion spawnRotation = Quaternion.identity;

            // Face towards player/center
            Transform player = Camera.main != null ? Camera.main.transform : null;
            if (player != null)
            {
                Vector3 lookDir = (player.position - spawnPosition);
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    spawnRotation = Quaternion.LookRotation(lookDir);
                }
            }

            // Determine type by ratio
            EnemyType typeToSpawn = (Random.value < shooterRatio) ? EnemyType.ShooterSoldier : EnemyType.MeleeZombie;

            EnemyBase spawnedEnemy = EnemyFactory.Instance.CreateEnemy(
                typeToSpawn,
                spawnPosition,
                spawnRotation,
                speedMultiplier,
                healthMultiplier,
                damageMultiplier
            );

            if (spawnedEnemy != null)
            {
                activeEnemies.Add(spawnedEnemy);

                // Play audio event
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayEnemySpawn();
                }
            }
        }

        private Vector3 CalculateRandomPlanePosition()
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(minSpawnRadius, maxSpawnRadius);

            Vector3 origin = (Camera.main != null) ? Camera.main.transform.position : arenaCenter;
            float x = origin.x + Mathf.Cos(angle) * radius;
            float z = origin.z + Mathf.Sin(angle) * radius;

            return new Vector3(x, planeYLevel, z);
        }

        /// <summary>
        /// Strictly satisfies requirement: All enemies must be wiped when game ends.
        /// </summary>
        public void WipeAllEnemies()
        {
            for (int i = activeEnemies.Count - 1; i >= 0; i--)
            {
                if (activeEnemies[i] != null)
                {
                    activeEnemies[i].Wipe();
                }
            }
            activeEnemies.Clear();
            Debug.Log("[EnemySpawner] All enemies successfully wiped from AR combat space.");
        }
    }
}
