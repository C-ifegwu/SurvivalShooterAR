using UnityEngine;
using SurvivalShooter.Core;
using SurvivalShooter.Audio;
using SurvivalShooter.Pooling;

namespace SurvivalShooter.Enemies
{
    /// <summary>
    /// Concrete Shooter Enemy class (Soldier).
    /// Maintains standoff distance and uses the Object Pooling system to fire projectile salvos.
    /// </summary>
    public class ShooterEnemy : EnemyBase
    {
        [Header("Shooter Specifics")]
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private string projectilePoolTag = "EnemyProjectile";
        [SerializeField] private float muzzleFlashDuration = 0.08f;

        protected override void Awake()
        {
            enemyType = EnemyType.ShooterSoldier;
            base.Awake();

            if (muzzlePoint == null)
            {
                // Fallback to chest/weapon height if not assigned
                GameObject muzzle = new GameObject("MuzzlePoint");
                muzzle.transform.SetParent(transform);
                muzzle.transform.localPosition = new Vector3(0.2f, 1.2f, 0.5f);
                muzzlePoint = muzzle.transform;
            }
        }

        protected override void MoveToPlayer()
        {
            RotateTowardsPlayer();

            // Advance towards standoff distance
            transform.position += transform.forward * (moveSpeed * Time.deltaTime);

            if (animator != null)
            {
                animator.SetBool("IsMoving", true);
            }
        }

        protected override void AttackPlayer()
        {
            RotateTowardsPlayer();

            if (animator != null)
            {
                animator.SetBool("IsMoving", false);
                animator.SetTrigger("Shoot");
            }

            FirePooledProjectile();
        }

        private void FirePooledProjectile()
        {
            if (playerTransform == null) return;

            Vector3 spawnPos = muzzlePoint != null ? muzzlePoint.position : (transform.position + Vector3.up * 1.2f);
            
            // Aim at player camera center
            Vector3 aimDirection = (playerTransform.position - spawnPos).normalized;
            Quaternion spawnRot = Quaternion.LookRotation(aimDirection);

            // Fetch projectile from pre-allocated object pool (Zero GC allocation!)
            GameObject projectileObj = ObjectPoolManager.Instance.SpawnFromPool(projectilePoolTag, spawnPos, spawnRot);
            if (projectileObj != null)
            {
                PooledProjectile projectile = projectileObj.GetComponent<PooledProjectile>();
                if (projectile != null)
                {
                    projectile.IsPlayerProjectile = false;
                    projectile.Damage = attackDamage;
                }
            }

            // Audio feedback
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayEnemyShoot();
            }
        }
    }
}
