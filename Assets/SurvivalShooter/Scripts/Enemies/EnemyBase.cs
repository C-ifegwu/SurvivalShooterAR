using System.Collections;
using UnityEngine;
using SurvivalShooter.Core;
using SurvivalShooter.Audio;

namespace SurvivalShooter.Enemies
{
    /// <summary>
    /// Abstract Base Class demonstrating Abstraction, Encapsulation, and Polymorphism.
    /// Defines core attributes and behavior contracts for all enemy variants.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [Header("Enemy Classification")]
        [SerializeField] protected EnemyType enemyType;

        [Header("Health & Defense")]
        [SerializeField] protected int maxHealth = 20;
        protected int currentHealth;
        protected bool isDead;

        [Header("Movement & Combat")]
        [SerializeField] protected float moveSpeed = 1.2f;
        [SerializeField] protected float attackRange = 1.5f;
        [SerializeField] protected float attackCooldown = 1.5f;
        [SerializeField] protected int attackDamage = 10;
        [SerializeField] protected int scoreValue = 100;

        [Header("Components & Visuals")]
        [SerializeField] protected Animator animator;
        [SerializeField] protected Collider enemyCollider;
        [SerializeField] protected Renderer[] renderers;

        protected Transform playerTransform;
        protected float nextAttackTime;
        protected Coroutine flashCoroutine;
        private Color[] originalColors;

        // Public Encapsulated Properties
        public EnemyType Type => enemyType;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public int ScoreValue => scoreValue;
        public float AttackRange => attackRange;

        protected virtual void Awake()
        {
            if (enemyCollider == null)
            {
                enemyCollider = GetComponent<Collider>();
            }

            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }

            CacheOriginalColors();
            currentHealth = maxHealth;
        }

        protected virtual void Start()
        {
            LocatePlayer();
        }

        protected virtual void Update()
        {
            if (isDead) return;

            if (playerTransform == null)
            {
                LocatePlayer();
                return;
            }

            float distanceToPlayer = GetHorizontalDistanceToPlayer();

            if (distanceToPlayer > attackRange)
            {
                MoveToPlayer();
            }
            else
            {
                RotateTowardsPlayer();
                if (Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + attackCooldown;
                    AttackPlayer();
                }
            }
        }

        protected void LocatePlayer()
        {
            if (Camera.main != null)
            {
                playerTransform = Camera.main.transform;
            }
        }

        protected float GetHorizontalDistanceToPlayer()
        {
            if (playerTransform == null) return float.MaxValue;
            Vector3 playerFlat = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
            return Vector3.Distance(transform.position, playerFlat);
        }

        protected void RotateTowardsPlayer()
        {
            if (playerTransform == null) return;
            Vector3 direction = (playerTransform.position - transform.position);
            direction.y = 0; // Lock to plane horizontal orientation
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
            }
        }

        // ================= Abstract Methods for Polymorphism =================

        /// <summary>
        /// Specific locomotion logic implemented by derived enemy classes.
        /// </summary>
        protected abstract void MoveToPlayer();

        /// <summary>
        /// Specific attack logic (Melee strike vs Projectile shot) implemented by derived classes.
        /// </summary>
        protected abstract void AttackPlayer();

        // ================= Damage & Hit Feedback =================

        public virtual void TakeDamage(int damageAmount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (isDead) return;

            currentHealth -= damageAmount;
            PlayHitFeedback();

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                Die();
            }
        }

        protected virtual void PlayHitFeedback()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayEnemyHurt();
            }

            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            flashCoroutine = StartCoroutine(HitFlashRoutine());
        }

        private IEnumerator HitFlashRoutine()
        {
            SetRenderersColor(Color.red);
            yield return new WaitForSeconds(0.12f);
            RestoreOriginalColors();
            flashCoroutine = null;
        }

        protected virtual void Die()
        {
            if (isDead) return;
            isDead = true;

            if (enemyCollider != null)
            {
                enemyCollider.enabled = false;
            }

            if (animator != null)
            {
                animator.SetTrigger("Die");
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayEnemyDeath();
            }

            // Fire observer event for UI, score, and leaderboard tallying
            GameEvents.TriggerEnemyKilled(enemyType, scoreValue);

            // Despawn after animation completes
            StartCoroutine(DespawnAfterDelay(2.0f));
        }

        protected IEnumerator DespawnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            Destroy(gameObject);
        }

        /// <summary>
        /// Instantly removes enemy during game-over or restart without triggering death rewards.
        /// </summary>
        public virtual void Wipe()
        {
            StopAllCoroutines();
            Destroy(gameObject);
        }

        public virtual void Initialize(float speedMultiplier, float healthMultiplier, float damageMultiplier)
        {
            moveSpeed *= speedMultiplier;
            maxHealth = Mathf.RoundToInt(maxHealth * healthMultiplier);
            currentHealth = maxHealth;
            attackDamage = Mathf.RoundToInt(attackDamage * damageMultiplier);
        }

        private void CacheOriginalColors()
        {
            if (renderers == null) return;
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].material.HasProperty("_BaseColor"))
                {
                    originalColors[i] = renderers[i].material.GetColor("_BaseColor");
                }
                else if (renderers[i] != null && renderers[i].material.HasProperty("_Color"))
                {
                    originalColors[i] = renderers[i].material.GetColor("_Color");
                }
                else
                {
                    originalColors[i] = Color.white;
                }
            }
        }

        private void SetRenderersColor(Color color)
        {
            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                if (r.material.HasProperty("_BaseColor"))
                {
                    r.material.SetColor("_BaseColor", color);
                }
                else if (r.material.HasProperty("_Color"))
                {
                    r.material.SetColor("_Color", color);
                }
            }
        }

        private void RestoreOriginalColors()
        {
            if (renderers == null || originalColors == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                if (renderers[i].material.HasProperty("_BaseColor"))
                {
                    renderers[i].material.SetColor("_BaseColor", originalColors[i]);
                }
                else if (renderers[i].material.HasProperty("_Color"))
                {
                    renderers[i].material.SetColor("_Color", originalColors[i]);
                }
            }
        }
    }
}
