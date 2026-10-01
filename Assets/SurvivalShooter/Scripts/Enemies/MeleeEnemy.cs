using System.Collections;
using UnityEngine;
using SurvivalShooter.Audio;
using SurvivalShooter.Core;
using SurvivalShooter.Player;

namespace SurvivalShooter.Enemies
{
    /// <summary>
    /// Melee zombie: walks straight at the player, short attack range, deals damage ONLY when
    /// still within close proximity at the moment the strike lands, then waits for its cooldown.
    /// </summary>
    public class MeleeEnemy : EnemyBase
    {
        [Header("Melee")]
        [SerializeField] private float strikeWindup = 0.5f;
        [SerializeField] private float strikeReachBonus = 0.25f;

        protected override void Awake()
        {
            enemyType = EnemyType.Melee;
            base.Awake();
        }

        protected override void Behave(float distance, Vector3 direction)
        {
            if (distance > attackRange)
            {
                Move(direction);
            }
            else
            {
                TryAttack();
            }
        }

        protected override void PerformAttack()
        {
            if (animator != null) animator.SetTrigger(AttackHash);
            StartCoroutine(Strike());
        }

        private IEnumerator Strike()
        {
            yield return new WaitForSeconds(strikeWindup);
            if (!IsAlive) yield break;

            // Proximity check at impact time: stepping back dodges the hit.
            if (HorizontalDistanceToTarget() <= attackRange + strikeReachBonus && PlayerHealth.HasInstance)
            {
                if (AudioManager.HasInstance) AudioManager.Instance.Play(SoundId.MeleeAttack, transform.position);
                PlayerHealth.Instance.TakeDamage(new DamageInfo(attackDamage, AimPoint, transform.forward, Team.Enemy));
            }
        }
    }
}
