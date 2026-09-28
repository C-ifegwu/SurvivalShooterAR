using UnityEngine;
using SurvivalShooter.Core;

namespace SurvivalShooter.Enemies
{
    /// <summary>
    /// Factory Pattern implementation for enemy instantiation and parameterization.
    /// Abstracts prefab references and applies difficulty scalers dynamically.
    /// </summary>
    public class EnemyFactory : MonoBehaviour
    {
        public static EnemyFactory Instance { get; private set; }

        [Header("Enemy Prefab Variants")]
        [SerializeField] private GameObject meleeZombiePrefab;
        [SerializeField] private GameObject shooterSoldierPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public EnemyBase CreateEnemy(EnemyType type, Vector3 position, Quaternion rotation, float speedMult, float healthMult, float damageMult)
        {
            GameObject prefabToSpawn = (type == EnemyType.MeleeZombie) ? meleeZombiePrefab : shooterSoldierPrefab;

            if (prefabToSpawn == null)
            {
                Debug.LogError($"[EnemyFactory] Prefab for {type} is not assigned!");
                return null;
            }

            GameObject instance = Instantiate(prefabToSpawn, position, rotation);
            EnemyBase enemy = instance.GetComponent<EnemyBase>();

            if (enemy != null)
            {
                enemy.Initialize(speedMult, healthMult, damageMult);
            }

            return enemy;
        }

        public void SetPrefabs(GameObject meleePrefab, GameObject shooterPrefab)
        {
            meleeZombiePrefab = meleePrefab;
            shooterSoldierPrefab = shooterPrefab;
        }
    }
}
