using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using SurvivalShooter.AR;
using SurvivalShooter.Core;

namespace SurvivalShooter.Enemies
{
    /// <summary>
    /// Spawns enemies on the detected AR plane around the placed arena, ramps up the spawn rate
    /// over the round and wipes every enemy when the round ends.
    /// Spawn points are validated by ray-casting down onto the AR plane's collider, so enemies
    /// appear on the real detected surface.
    /// </summary>
    public class EnemySpawner : Singleton<EnemySpawner>
    {
        [Header("Placement")]
        [SerializeField] private float minRadius = 1.9f;
        [SerializeField] private float maxRadius = 3.4f;
        [SerializeField] private float minDistanceFromPlayer = 2.3f;
        [SerializeField] [Range(0f, 1f)] private float inFrontBias = 0.7f;
        [SerializeField] private float separationRadius = 0.55f;

        private readonly List<EnemyBase> active = new List<EnemyBase>();
        private DifficultyConfig config;
        private bool spawning;
        private float roundTime;
        private float nextSpawn;
        private int spawnedThisRound;

        public IReadOnlyList<EnemyBase> ActiveEnemies => active;
        public int AliveCount
        {
            get
            {
                int n = 0;
                foreach (var e in active) if (e != null && e.IsAlive) n++;
                return n;
            }
        }

        public void Configure(DifficultyConfig cfg)
        {
            config = cfg;
            roundTime = 0f;
            spawnedThisRound = 0;
            nextSpawn = 1.2f;
        }

        public void SetSpawning(bool enabled) => spawning = enabled;

        private void Update()
        {
            active.RemoveAll(e => e == null);
            if (!spawning || config == null) return;

            roundTime += Time.deltaTime;
            if (roundTime >= nextSpawn && AliveCount < config.maxAliveEnemies)
            {
                SpawnOne();
                float progress = Mathf.Clamp01(roundTime / Mathf.Max(1f, config.survivalTime));
                nextSpawn = roundTime + Mathf.Lerp(config.startSpawnInterval, config.endSpawnInterval, progress);
            }
        }

        private void SpawnOne()
        {
            if (!EnemyFactory.HasInstance || !ARPlacementManager.HasInstance) return;
            var placement = ARPlacementManager.Instance;
            Camera cam = placement.ARCamera;
            if (!placement.IsArenaPlaced || cam == null) return;

            // First two enemies are always melee so the player learns the basics.
            EnemyType type = spawnedThisRound < 2 ? EnemyType.Melee
                : (Random.value < config.shooterChance ? EnemyType.Shooter : EnemyType.Melee);

            Vector3 pos = FindSpawnPoint(placement, cam, out float floorY);
            Vector3 look = cam.transform.position - pos; look.y = 0f;
            Quaternion rot = look.sqrMagnitude > 0.001f ? Quaternion.LookRotation(look) : Quaternion.identity;

            EnemyBase enemy = EnemyFactory.Instance.Create(type, pos, rot, placement.ArenaRoot, config, cam.transform, floorY);
            if (enemy == null) return;
            active.Add(enemy);
            spawnedThisRound++;
            GameEvents.RaiseEnemySpawned(type, enemy.transform);
        }

        private Vector3 FindSpawnPoint(ARPlacementManager placement, Camera cam, out float floorY)
        {
            Vector3 center = placement.ArenaRoot.position;
            Vector3 camFlat = cam.transform.position; camFlat.y = center.y;
            Vector3 camFwd = cam.transform.forward; camFwd.y = 0f;
            camFwd = camFwd.sqrMagnitude > 0.001f ? camFwd.normalized : Vector3.forward;

            Vector3 fallback = center + Quaternion.Euler(0f, Random.Range(-60f, 60f), 0f) * camFwd * maxRadius;
            floorY = placement.FloorHeight;

            for (int attempt = 0; attempt < 14; attempt++)
            {
                Vector3 dir;
                if (Random.value < inFrontBias)
                    dir = Quaternion.Euler(0f, Random.Range(-70f, 70f), 0f) * camFwd;
                else
                    dir = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward;

                Vector3 candidate = center + dir * Random.Range(minRadius, maxRadius);
                if (Vector3.Distance(candidate, camFlat) < minDistanceFromPlayer) continue;

                // Prefer points that are really on a detected AR plane.
                if (Physics.Raycast(candidate + Vector3.up * 1.5f, Vector3.down, out RaycastHit hit, 3f, ~0, QueryTriggerInteraction.Ignore)
                    && hit.collider.GetComponentInParent<ARPlane>() != null)
                {
                    floorY = hit.point.y;
                    return hit.point;
                }

                // Remember a decent point in case the plane is still small.
                if (attempt < 4) fallback = candidate;
            }

            fallback.y = placement.FloorHeight;
            return fallback;
        }

        /// <summary>Simple steering so enemies don't stack inside each other.</summary>
        public Vector3 GetSeparation(EnemyBase self)
        {
            Vector3 push = Vector3.zero;
            Vector3 p = self.transform.position;
            foreach (var other in active)
            {
                if (other == null || other == self || !other.IsAlive) continue;
                Vector3 d = p - other.transform.position; d.y = 0f;
                float dist = d.magnitude;
                if (dist > 0.0001f && dist < separationRadius)
                    push += d / dist * (1f - dist / separationRadius);
            }
            return push * 1.2f;
        }

        public void WipeAllEnemies()
        {
            foreach (var e in active)
            {
                if (e != null) e.Wipe();
            }
            active.Clear();
        }
    }
}
