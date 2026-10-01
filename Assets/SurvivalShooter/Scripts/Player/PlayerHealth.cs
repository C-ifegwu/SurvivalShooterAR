using UnityEngine;
using SurvivalShooter.Audio;
using SurvivalShooter.Core;

namespace SurvivalShooter.Player
{
    /// <summary>
    /// Player health (lives on the AR camera = first-person player).
    /// A small sphere collider on the camera is what enemy projectiles hit.
    /// Raises events for the HUD (health bar, damage vignette) and triggers Game Over on death.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class PlayerHealth : Singleton<PlayerHealth>, IDamageable
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private float hurtSoundCooldown = 0.25f;

        /// <summary>Editor test hook: damage events still fire but health is not reduced.</summary>
        public static bool DebugInvulnerable;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => DebugInvulnerable = false;

        private int currentHealth;
        private bool dead;
        private float nextHurtSound;

        public Team Team => Team.Player;
        public bool IsAlive => !dead;
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public float Health01 => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

        protected override void OnSingletonAwake()
        {
            currentHealth = maxHealth;
            var col = GetComponent<SphereCollider>();
            col.isTrigger = false;
        }

        private void Start() => GameEvents.RaisePlayerHealthChanged(currentHealth, maxHealth);

        public void TakeDamage(DamageInfo info)
        {
            if (dead || info.SourceTeam == Team.Player) return;
            if (GameManager.HasInstance && GameManager.Instance.CurrentState != GameStateId.Playing) return;

            if (!DebugInvulnerable) currentHealth = Mathf.Max(0, currentHealth - info.Amount);
            Vector3 source = info.Point - info.Direction;
            GameEvents.RaisePlayerDamaged(info.Amount, source);
            GameEvents.RaisePlayerHealthChanged(currentHealth, maxHealth);

            if (AudioManager.HasInstance && Time.time >= nextHurtSound)
            {
                nextHurtSound = Time.time + hurtSoundCooldown;
                AudioManager.Instance.Play(SoundId.PlayerHurt);
            }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (currentHealth > 0 && info.Amount >= 10) Handheld.Vibrate();
#endif

            if (currentHealth <= 0) Die();
        }

        private void Die()
        {
            if (dead) return;
            dead = true;
            if (AudioManager.HasInstance) AudioManager.Instance.Play(SoundId.PlayerDeath);
            GameEvents.RaisePlayerDied();
        }

        public void ResetHealth()
        {
            dead = false;
            currentHealth = maxHealth;
            GameEvents.RaisePlayerHealthChanged(currentHealth, maxHealth);
        }
    }
}
