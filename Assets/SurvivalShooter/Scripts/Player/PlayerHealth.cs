using UnityEngine;
using SurvivalShooter.Core;
using SurvivalShooter.Audio;

namespace SurvivalShooter.Player
{
    /// <summary>
    /// Player Health system implementing IDamageable with reactive event dispatching.
    /// Provides screen damage feedback and triggers game-over upon death.
    /// </summary>
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        public static PlayerHealth Instance { get; private set; }

        [Header("Health Attributes")]
        [SerializeField] private int maxHealth = 100;
        private int currentHealth;
        private bool isDead;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsDead => isDead;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            currentHealth = maxHealth;
        }

        private void Start()
        {
            GameEvents.TriggerPlayerHealthChanged(currentHealth, maxHealth);
        }

        public void TakeDamage(int damageAmount, Vector3 hitPoint, Vector3 hitNormal)
        {
            if (isDead) return;

            currentHealth = Mathf.Max(0, currentHealth - damageAmount);
            Debug.Log($"[PlayerHealth] Player damaged: -{damageAmount} HP. Remaining: {currentHealth}/{maxHealth}");

            // Notify UI & camera shake observers
            GameEvents.TriggerPlayerDamaged(damageAmount);
            GameEvents.TriggerPlayerHealthChanged(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            Debug.Log("[PlayerHealth] Player killed! Triggering Game Over.");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayPlayerDeath();
            }

            GameEvents.TriggerPlayerDied();
        }

        public void ResetHealth()
        {
            isDead = false;
            currentHealth = maxHealth;
            GameEvents.TriggerPlayerHealthChanged(currentHealth, maxHealth);
        }
    }
}
