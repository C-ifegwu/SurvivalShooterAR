using System.Collections;
using UnityEngine;
using DG.Tweening;
using SurvivalShooter.Audio;
using SurvivalShooter.Core;
using SurvivalShooter.Pooling;

namespace SurvivalShooter.Enemies
{
    /// <summary>
    /// Abstract base class for every enemy.
    /// Encapsulation: health, stats and state are private/protected and only change through methods.
    /// Abstraction: subclasses only implement <see cref="Behave"/> and <see cref="PerformAttack"/>.
    /// Inheritance: MeleeEnemy / ShooterEnemy reuse movement, damage, hit feedback and death.
    /// Polymorphism: the spawner, projectiles and aim-assist treat every enemy as EnemyBase/IDamageable.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Identity")]
        [SerializeField] protected EnemyType enemyType;

        [Header("Stats")]
        [SerializeField] protected int maxHealth = 50;
        [SerializeField] protected float moveSpeed = 0.8f;
        [SerializeField] protected float turnSpeed = 7f;
        [SerializeField] protected float attackRange = 0.9f;
        [SerializeField] protected float attackCooldown = 1.3f;
        [SerializeField] protected int attackDamage = 10;
        [SerializeField] protected int scoreValue = 100;

        [Header("References")]
        [SerializeField] protected Animator animator;
        [SerializeField] protected Transform model;
        [SerializeField] protected EnemyHealthBar healthBar;

        protected static readonly int SpeedHash = Animator.StringToHash("Speed");
        protected static readonly int AttackHash = Animator.StringToHash("Attack");
        protected static readonly int DieHash = Animator.StringToHash("Die");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private int currentHealth;
        private bool dead;
        private bool active;
        private float nextAttackTime;
        private float floorY;
        private float currentSpeed;
        private Vector3 knockback;
        private Renderer[] renderers;
        private MaterialPropertyBlock flashBlock;
        private CapsuleCollider capsule;
        private Coroutine flashRoutine;
        private Vector3 modelBaseScale;

        protected Transform Target { get; private set; }
        public EnemyType Type => enemyType;
        public Team Team => Team.Enemy;
        public bool IsAlive => !dead;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public float AttackRange => attackRange;
        public int ScoreValue => scoreValue;
        public Vector3 AimPoint => capsule != null ? transform.TransformPoint(capsule.center) : transform.position + Vector3.up * 0.6f;

        // ------------------------------------------------------------------ Lifecycle

        protected virtual void Awake()
        {
            capsule = GetComponent<CapsuleCollider>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;
            if (model == null && animator != null) model = animator.transform;
            if (healthBar == null) healthBar = GetComponentInChildren<EnemyHealthBar>(true);
            renderers = model != null ? model.GetComponentsInChildren<Renderer>() : GetComponentsInChildren<Renderer>();
            flashBlock = new MaterialPropertyBlock();
            modelBaseScale = model != null ? model.localScale : Vector3.one;
        }

        /// <summary>Factory entry point: apply difficulty and play the spawn-in animation.</summary>
        public virtual void Initialize(DifficultyConfig config, Transform target, float floorHeight)
        {
            moveSpeed *= config.enemySpeedMultiplier;
            maxHealth = Mathf.RoundToInt(maxHealth * config.enemyHealthMultiplier);
            attackDamage = Mathf.Max(1, Mathf.RoundToInt(attackDamage * config.enemyDamageMultiplier));
            currentHealth = maxHealth;
            Target = target;
            floorY = floorHeight;
            nextAttackTime = Time.time + 1.2f;

            if (healthBar != null) healthBar.SetValue(1f, true);

            // Spawn animation: rise + scale in, then become active.
            active = false;
            transform.localScale = Vector3.one * 0.01f;
            transform.DOScale(1f, 0.55f).SetEase(Ease.OutBack).OnComplete(() => active = true);

            if (PoolManager.HasInstance)
                PoolManager.Instance.SpawnEffect(EffectType.SpawnPortal, new Vector3(transform.position.x, floorY + 0.01f, transform.position.z), Quaternion.identity);
            if (AudioManager.HasInstance) AudioManager.Instance.Play(SoundId.EnemySpawn, transform.position);
        }

        protected virtual void Update()
        {
            if (dead || !active || Target == null) return;

            Vector3 flatTarget = new Vector3(Target.position.x, floorY, Target.position.z);
            Vector3 toTarget = flatTarget - transform.position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;

            FaceDirection(toTarget);
            currentSpeed = 0f;
            Behave(distance, toTarget.normalized);   // Template method → subclass decides

            // knock-back decay + stay glued to the floor
            if (knockback.sqrMagnitude > 0.0001f)
            {
                transform.position += knockback * Time.deltaTime;
                knockback = Vector3.Lerp(knockback, Vector3.zero, 10f * Time.deltaTime);
            }
            Vector3 p = transform.position;
            p.y = floorY;
            transform.position = p;

            if (animator != null) animator.SetFloat(SpeedHash, currentSpeed, 0.1f, Time.deltaTime);
        }

        // ------------------------------------------------------------------ Abstract behaviour

        /// <summary>Per-frame AI decided by each concrete enemy.</summary>
        protected abstract void Behave(float distanceToTarget, Vector3 directionToTarget);

        /// <summary>The concrete attack (melee strike / projectile).</summary>
        protected abstract void PerformAttack();

        // ------------------------------------------------------------------ Shared helpers

        protected void Move(Vector3 direction, float speedFactor = 1f)
        {
            Vector3 separation = EnemySpawner.HasInstance ? EnemySpawner.Instance.GetSeparation(this) : Vector3.zero;
            Vector3 velocity = (direction * speedFactor + separation) * moveSpeed;
            velocity.y = 0f;
            transform.position += velocity * Time.deltaTime;
            currentSpeed = Mathf.Abs(speedFactor);
        }

        protected void FaceDirection(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            Quaternion look = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        protected bool AttackReady => Time.time >= nextAttackTime;

        protected void TryAttack()
        {
            if (!AttackReady) return;
            nextAttackTime = Time.time + attackCooldown;
            PerformAttack();
        }

        protected float HorizontalDistanceToTarget()
        {
            if (Target == null) return float.MaxValue;
            Vector3 a = transform.position; a.y = 0f;
            Vector3 b = Target.position; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        // ------------------------------------------------------------------ Damage / feedback

        public virtual void TakeDamage(DamageInfo info)
        {
            if (dead || info.SourceTeam == Team.Enemy) return;

            currentHealth = Mathf.Max(0, currentHealth - info.Amount);
            bool killed = currentHealth <= 0;

            if (healthBar != null) healthBar.SetValue((float)currentHealth / maxHealth);
            PlayHitFeedback(info);
            GameEvents.RaiseEnemyHit(enemyType, info.Point, killed);

            if (killed) Die();
        }

        protected virtual void PlayHitFeedback(DamageInfo info)
        {
            Vector3 push = info.Direction; push.y = 0f;
            knockback += push.normalized * 0.9f;

            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());

            if (model != null)
            {
                model.DOKill(true);
                model.localScale = modelBaseScale;
                model.DOPunchScale(modelBaseScale * 0.12f, 0.18f, 6, 0.6f);
            }

            if (AudioManager.HasInstance) AudioManager.Instance.Play(SoundId.EnemyHurt, transform.position);
        }

        private IEnumerator FlashRoutine()
        {
            Color flash = new Color(1.7f, 0.75f, 0.7f, 1f);
            SetFlash(flash);
            yield return new WaitForSeconds(0.07f);
            SetFlash(new Color(1.3f, 0.45f, 0.45f, 1f));
            yield return new WaitForSeconds(0.06f);
            ClearFlash();
            flashRoutine = null;
        }

        private void SetFlash(Color c)
        {
            flashBlock.Clear();
            flashBlock.SetColor(BaseColorId, c);
            flashBlock.SetColor(ColorId, c);
            foreach (var r in renderers) if (r != null) r.SetPropertyBlock(flashBlock);
        }

        private void ClearFlash()
        {
            flashBlock.Clear();
            foreach (var r in renderers) if (r != null) r.SetPropertyBlock(null);
        }

        protected virtual void Die()
        {
            if (dead) return;
            dead = true;
            StopAllCoroutines();
            ClearFlash();
            capsule.enabled = false;
            if (healthBar != null) healthBar.Hide();

            if (PoolManager.HasInstance) PoolManager.Instance.SpawnEffect(EffectType.DeathBurst, AimPoint, Quaternion.identity);
            if (AudioManager.HasInstance) AudioManager.Instance.Play(SoundId.EnemyDeath, transform.position);
            GameEvents.RaiseEnemyKilled(enemyType, transform.position, scoreValue);

            PlayDeathAnimation();
            StartCoroutine(SinkAndDestroy(1.6f));
        }

        /// <summary>Subclasses decide how the body reacts (animation clip vs procedural fall).</summary>
        protected virtual void PlayDeathAnimation()
        {
            if (animator != null) animator.SetTrigger(DieHash);
        }

        private IEnumerator SinkAndDestroy(float delay)
        {
            yield return new WaitForSeconds(delay);
            transform.DOKill();
            transform.DOScale(0f, 0.45f).SetEase(Ease.InBack);
            yield return new WaitForSeconds(0.5f);
            Destroy(gameObject);
        }

        /// <summary>Instant removal on game end/restart — no score, no effects.</summary>
        public void Wipe()
        {
            dead = true;
            StopAllCoroutines();
            transform.DOKill();
            if (model != null) model.DOKill();
            Destroy(gameObject);
        }

        protected virtual void OnDestroy()
        {
            transform.DOKill();
            if (model != null) model.DOKill();
        }
    }
}
