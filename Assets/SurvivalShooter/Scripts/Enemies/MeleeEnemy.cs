using UnityEngine;
using SurvivalShooter.Core;
using SurvivalShooter.Audio;
using SurvivalShooter.Player;

namespace SurvivalShooter.Enemies
{
    /// <summary>
    /// Concrete Melee Enemy class (Zombie).
    /// Exhibits high aggression, short attack range, and ferocious close-quarter strikes.
    /// </summary>
    public class MeleeEnemy : EnemyBase
    {
        [Header("Melee Specifics")]
        [SerializeField] private float strikeDelay = 0.35f;

        protected override void Awake()
        {
            enemyType = EnemyType.MeleeZombie;
            base.Awake();
        }

        protected override void MoveToPlayer()
        {
            RotateTowardsPlayer();

            // Translate forward on plane
            transform.position += transform.forward * (moveSpeed * Time.deltaTime);

            if (animator != null)
            {
                animator.SetBool("IsMoving", true);
            }
        }

        protected override void AttackPlayer()
        {
            if (animator != null)
            {
                animator.SetBool("IsMoving", false);
                animator.SetTrigger("Attack");
            }

            // Play melee bite/growl attack sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayEnemyMeleeDamage();
            }

            // Inflict damage with slight animation synchronization
            StartCoroutine(ExecuteMeleeStrike());
        }

        private System.Collections.IEnumerator ExecuteMeleeStrike()
        {
            yield return new WaitForSeconds(strikeDelay);

            if (isDead) yield break;

            if (GetHorizontalDistanceToPlayer() <= attackRange + 0.5f)
            {
                PlayerHealth player = PlayerHealth.Instance;
                if (player != null && !player.IsDead)
                {
                    player.TakeDamage(attackDamage, transform.position, transform.forward);
                }
            }
        }
    }
}
