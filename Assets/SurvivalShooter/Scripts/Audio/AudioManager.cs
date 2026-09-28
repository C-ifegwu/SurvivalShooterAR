using UnityEngine;

namespace SurvivalShooter.Audio
{
    /// <summary>
    /// Centralized Audio Manager adhering to performance best practices.
    /// Manages shared AudioSources to eliminate component redundancy across dozens of spawned entities.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Clips - Mandatory Events")]
        [Tooltip("Played when player fires a projectile.")]
        [SerializeField] private AudioClip playerShootClip;

        [Tooltip("Played when player health reaches zero.")]
        [SerializeField] private AudioClip playerDeathClip;

        [Tooltip("Played when an enemy spawns into the combat zone.")]
        [SerializeField] private AudioClip enemySpawnClip;

        [Tooltip("Played when a shooter enemy fires a projectile.")]
        [SerializeField] private AudioClip enemyShootClip;

        [Tooltip("Played when a melee enemy strikes the player.")]
        [SerializeField] private AudioClip enemyMeleeDamageClip;

        [Header("Audio Clips - Supplemental Polish")]
        [SerializeField] private AudioClip enemyHurtClip;
        [SerializeField] private AudioClip enemyDeathClip;
        [SerializeField] private AudioClip uiButtonClickClip;
        [SerializeField] private AudioClip gameStartClip;
        [SerializeField] private AudioClip victoryFanfareClip;

        [Header("Audio Channels")]
        [SerializeField] private AudioSource playerChannel;
        [SerializeField] private AudioSource enemyChannel;
        [SerializeField] private AudioSource uiChannel;
        [SerializeField] private AudioSource ambientChannel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureChannels();
        }

        private void EnsureChannels()
        {
            if (playerChannel == null)
            {
                playerChannel = gameObject.AddComponent<AudioSource>();
                playerChannel.playOnAwake = false;
                playerChannel.spatialBlend = 0f; // 2D for crisp player feedback
            }

            if (enemyChannel == null)
            {
                enemyChannel = gameObject.AddComponent<AudioSource>();
                enemyChannel.playOnAwake = false;
                enemyChannel.spatialBlend = 0f;
            }

            if (uiChannel == null)
            {
                uiChannel = gameObject.AddComponent<AudioSource>();
                uiChannel.playOnAwake = false;
                uiChannel.spatialBlend = 0f;
            }

            if (ambientChannel == null)
            {
                ambientChannel = gameObject.AddComponent<AudioSource>();
                ambientChannel.playOnAwake = false;
                ambientChannel.loop = true;
                ambientChannel.spatialBlend = 0f;
                ambientChannel.volume = 0.35f;
            }
        }

        // ================= Mandatory Gameplay Event Sounds =================

        public void PlayPlayerShoot()
        {
            PlayWithPitchVariation(playerChannel, playerShootClip, 0.95f, 1.05f, 0.85f);
        }

        public void PlayPlayerDeath()
        {
            if (playerDeathClip != null)
            {
                playerChannel.pitch = 1.0f;
                playerChannel.PlayOneShot(playerDeathClip, 1.0f);
            }
        }

        public void PlayEnemySpawn()
        {
            PlayWithPitchVariation(enemyChannel, enemySpawnClip, 0.85f, 1.15f, 0.75f);
        }

        public void PlayEnemyShoot()
        {
            PlayWithPitchVariation(enemyChannel, enemyShootClip, 0.9f, 1.1f, 0.8f);
        }

        public void PlayEnemyMeleeDamage()
        {
            PlayWithPitchVariation(enemyChannel, enemyMeleeDamageClip, 0.9f, 1.1f, 0.9f);
        }

        // ================= Supplemental Polish Sounds =================

        public void PlayEnemyHurt()
        {
            PlayWithPitchVariation(enemyChannel, enemyHurtClip, 0.9f, 1.15f, 0.6f);
        }

        public void PlayEnemyDeath()
        {
            PlayWithPitchVariation(enemyChannel, enemyDeathClip, 0.85f, 1.05f, 0.8f);
        }

        public void PlayButtonClick()
        {
            if (uiButtonClickClip != null)
            {
                uiChannel.pitch = 1.0f;
                uiChannel.PlayOneShot(uiButtonClickClip, 0.9f);
            }
        }

        public void PlayGameStart()
        {
            if (gameStartClip != null)
            {
                uiChannel.PlayOneShot(gameStartClip, 1.0f);
            }
        }

        public void PlayVictory()
        {
            if (victoryFanfareClip != null)
            {
                uiChannel.PlayOneShot(victoryFanfareClip, 1.0f);
            }
        }

        private void PlayWithPitchVariation(AudioSource source, AudioClip clip, float minPitch, float maxPitch, float volume = 1f)
        {
            if (source == null || clip == null) return;
            source.pitch = Random.Range(minPitch, maxPitch);
            source.PlayOneShot(clip, volume);
        }

        // Clip Setters for automated Editor configuration
        public void ConfigureClips(
            AudioClip playerShoot,
            AudioClip playerDeath,
            AudioClip enemySpawn,
            AudioClip enemyShoot,
            AudioClip enemyMelee,
            AudioClip hurt,
            AudioClip death,
            AudioClip button)
        {
            playerShootClip = playerShoot;
            playerDeathClip = playerDeath;
            enemySpawnClip = enemySpawn;
            enemyShootClip = enemyShoot;
            enemyMeleeDamageClip = enemyMelee;
            enemyHurtClip = hurt;
            enemyDeathClip = death;
            uiButtonClickClip = button;
        }
    }
}
