using System.Collections;
using UnityEngine;
using DG.Tweening;
using SurvivalShooter.Audio;
using SurvivalShooter.Core;
using SurvivalShooter.Pooling;

namespace SurvivalShooter.Enemies
{
    /// <summary>
    /// Ranged soldier: advances until it reaches its preferred shooting distance, stops, then fires
    /// pooled projectiles at the player after a short visible "charge" telegraph. Backs off if the
    /// player walks too close. Longer attack range than the melee enemy.
    /// </summary>
    public class ShooterEnemy : EnemyBase
    {
        [Header("Shooter")]
        [SerializeField] private float stopDistance = 2.4f;
        [SerializeField] private float retreatDistance = 1.2f;
        [SerializeField] private float projectileSpeed = 4.5f;
        [SerializeField] private float spreadDegrees = 2f;
        [SerializeField] private float chargeTime = 0.35f;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform muzzleFlash;

        private static readonly int ShootHash = Animator.StringToHash("Shoot");
        private Vector3 flashScale;
        private bool charging;

        protected override void Awake()
        {
            enemyType = EnemyType.Shooter;
            base.Awake();
            if (muzzleFlash != null)
            {
                flashScale = muzzleFlash.localScale;
                muzzleFlash.gameObject.SetActive(false);
            }
        }

        protected override void Behave(float distance, Vector3 direction)
        {
            if (charging) return;

            if (distance > stopDistance)
            {
                Move(direction);
            }
            else if (distance < retreatDistance)
            {
                Move(-direction, 0.6f);
            }

            if (distance <= attackRange) TryAttack();
        }

        protected override void PerformAttack()
        {
            StartCoroutine(ChargeAndFire());
        }

        private IEnumerator ChargeAndFire()
        {
            charging = true;
            if (animator != null) animator.SetTrigger(ShootHash);

            if (muzzleFlash != null)
            {
                muzzleFlash.gameObject.SetActive(true);
                muzzleFlash.localScale = Vector3.zero;
                muzzleFlash.DOKill();
                muzzleFlash.DOScale(flashScale, chargeTime).SetEase(Ease.InQuad);
            }

            yield return new WaitForSeconds(chargeTime);
            if (!IsAlive) yield break;

            Fire();

            if (muzzleFlash != null)
            {
                muzzleFlash.DOKill();
                muzzleFlash.DOScale(flashScale * 1.8f, 0.06f).OnComplete(() =>
                {
                    if (muzzleFlash != null) muzzleFlash.gameObject.SetActive(false);
                });
            }

            yield return new WaitForSeconds(0.25f);
            charging = false;
        }

        private void Fire()
        {
            if (Target == null || !PoolManager.HasInstance) return;
            Vector3 origin = muzzle != null ? muzzle.position : AimPoint + transform.forward * 0.3f;
            Vector3 aim = Target.position + Vector3.down * 0.08f;
            Vector3 dir = (aim - origin).normalized;
            dir = Quaternion.Euler(Random.Range(-spreadDegrees, spreadDegrees), Random.Range(-spreadDegrees, spreadDegrees), 0f) * dir;

            Projectile p = PoolManager.Instance.SpawnProjectile(Team.Enemy, origin, Quaternion.LookRotation(dir));
            if (p != null) p.Launch(Team.Enemy, attackDamage, projectileSpeed);

            if (AudioManager.HasInstance) AudioManager.Instance.Play(SoundId.EnemyShoot, origin);
        }

        /// <summary>No death clip on this rig, so the soldier falls over procedurally.</summary>
        protected override void PlayDeathAnimation()
        {
            if (muzzleFlash != null) muzzleFlash.gameObject.SetActive(false);
            if (animator != null) animator.speed = 0.3f;
            if (model != null)
            {
                model.DOKill();
                model.DOLocalRotate(new Vector3(-88f, 0f, 0f), 0.5f, RotateMode.LocalAxisAdd).SetEase(Ease.InQuad);
            }
        }
    }
}
