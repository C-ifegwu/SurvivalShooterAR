using UnityEngine;
using DG.Tweening;

namespace SurvivalShooter.Audio
{
    /// <summary>
    /// Centralized Audio Manager adhering to performance best practices.
    /// Manages shared AudioSources to eliminate component redundancy across dozens of spawned entities.
    /// Incorporates dynamic BGM crossfading, UI interactions, and directional combat sound.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header( Audio Clips - Mandatory Combat)]
        [SerializeField] private AudioClip playerShootClip;
        [SerializeField] private AudioClip playerDeathClip;
        [SerializeField] private AudioClip enemySpawnClip;
        [SerializeField] private AudioClip enemyShootClip;
        [SerializeField] private AudioClip enemyMeleeDamageClip;

        [Header(Audio Clips - Combat Polish)]
        [SerializeField] private AudioClip enemyHurtClip;
        [SerializeField] private AudioClip enemyDeathClip;
        [SerializeField] private AudioClip gameStartClip;
        [SerializeField] private AudioClip victoryFanfareClip;
        [SerializeField] private AudioClip defeatClip;

        [Header(Audio Clips - UI Polish)]
        [SerializeField] private AudioClip uiButtonClickClip;
        [SerializeField] private AudioClip uiButtonHoverClip;
        [SerializeField] private AudioClip uiWhooshClip;
        [SerializeField] private AudioClip uiPunchClip;
        [SerializeField] private AudioClip uiMechanicalClip;

        [Header(Audio Clips - Background Music)]
        [SerializeField] private AudioClip menuBgmClip;
        [SerializeField] private AudioClip combatBgmClip;

        [Header(Audio Channels)]
        [SerializeField] private AudioSource playerChannel;
        [SerializeField] private AudioSource enemyChannel;
        [SerializeField] private AudioSource uiChannel;
        [SerializeField] private AudioSource musicChannel;

        [Header(Volume Settings)]
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.45f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.85f;

        private Tween musicFadeTween;

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
                playerChannel.spatialBlend = 0f;
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

            if (musicChannel == null)
            {
                musicChannel = gameObject.AddComponent<AudioSource>();
                musicChannel.playOnAwake = false;
                musicChannel.loop = true;
                musicChannel.spatialBlend = 0f;
                musicChannel.volume = musicVolume;
            }
        }

        // ================= Background Music Transitions =================

        public void PlayMenuBGM(float fadeDuration = 0.8f)
        {
            CrossfadeMusic(menuBgmClip, musicVolume * 0.75f, fadeDuration);
        }

        public void PlayCombatBGM(float fadeDuration = 0.5f)
        {
            CrossfadeMusic(combatBgmClip, musicVolume, fadeDuration);
        }

        public void StopMusic(float fadeDuration = 0.5f)
        {
            if (musicChannel == null || !musicChannel.isPlaying) return;

            musicFadeTween?.Kill();
            musicFadeTween = musicChannel.DOFade(0f, fadeDuration)
                .SetUpdate(true)
                .OnComplete(() => musicChannel.Stop());
        }

        private void CrossfadeMusic(AudioClip newClip, float targetVol, float duration)
        {
            if (musicChannel == null || newClip == null) return;
            if (musicChannel.isPlaying && musicChannel.clip == newClip) return;

            musicFadeTween?.Kill();

            if (musicChannel.isPlaying)
            {
                musicFadeTween = musicChannel.DOFade(0f, duration * 0.5f)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        musicChannel.clip = newClip;
                        musicChannel.Play();
                        musicFadeTween = musicChannel.DOFade(targetVol, duration * 0.5f).SetUpdate(true);
                    });
            }
            else
            {
                musicChannel.clip = newClip;
                musicChannel.volume = 0f;
                musicChannel.Play();
                musicFadeTween = musicChannel.DOFade(targetVol, duration).SetUpdate(true);
            }
        }

        // ================= Mandatory Combat Event Sounds =================

        public void PlayPlayerShoot()
        {
            PlayWithPitchVariation(playerChannel, playerShootClip, 0.95f, 1.05f, sfxVolume);
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
            PlayWithPitchVariation(enemyChannel, enemySpawnClip, 0.85f, 1.15f, sfxVolume * 0.8f);
        }

        public void PlayEnemyShoot()
        {
            PlayWithPitchVariation(enemyChannel, enemyShootClip, 0.9f, 1.1f, sfxVolume * 0.85f);
        }

        public void PlayEnemyMeleeDamage()
        {
            PlayWithPitchVariation(enemyChannel, enemyMeleeDamageClip, 0.9f, 1.1f, sfxVolume);
        }

        // ================= Supplemental Polish Sounds =================

        public void PlayEnemyHurt()
        {
            PlayWithPitchVariation(enemyChannel, enemyHurtClip, 0.9f, 1.15f, sfxVolume * 0.65f);
        }

        public void PlayEnemyDeath()
        {
            PlayWithPitchVariation(enemyChannel, enemyDeathClip, 0.85f, 1.05f, sfxVolume * 0.85f);
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
            StopMusic(0.2f);
            if (victoryFanfareClip != null)
            {
                uiChannel.PlayOneShot(victoryFanfareClip, 1.0f);
            }
        }

        public void PlayDefeat()
        {
            StopMusic(0.2f);
            if (defeatClip != null)
            {
                uiChannel.PlayOneShot(defeatClip, 1.0f);
            }
        }

        // ================= UI Polish Sounds =================

        public void PlayButtonClick()
        {
            if (uiButtonClickClip != null)
            {
                uiChannel.pitch = 1.0f;
                uiChannel.PlayOneShot(uiButtonClickClip, 0.9f);
            }
        }

        public void PlayButtonHover()
        {
            if (uiButtonHoverClip != null)
            {
                uiChannel.pitch = Random.Range(0.98f, 1.02f);
                uiChannel.PlayOneShot(uiButtonHoverClip, 0.5f);
            }
        }

        public void PlayWhoosh()
        {
            if (uiWhooshClip != null)
            {
                uiChannel.pitch = Random.Range(0.95f, 1.05f);
                uiChannel.PlayOneShot(uiWhooshClip, 0.75f);
            }
        }

        public void PlayPunch()
        {
            if (uiPunchClip != null)
            {
                uiChannel.pitch = 1.0f;
                uiChannel.PlayOneShot(uiPunchClip, 0.85f);
            }
        }

        public void PlayMechanical()
        {
            if (uiMechanicalClip != null)
            {
                uiChannel.pitch = 1.0f;
                uiChannel.PlayOneShot(uiMechanicalClip, 0.8f);
            }
        }

        private void PlayWithPitchVariation(AudioSource source, AudioClip clip, float minPitch, float maxPitch, float volume = 1f)
        {
            if (source == null || clip == null) return;
            source.pitch = Random.Range(minPitch, maxPitch);
            source.PlayOneShot(clip, volume);
        }

        // Configuration helper for Editor setup
        public void ConfigureClips(
            AudioClip playerShoot,
            AudioClip playerDeath,
            AudioClip enemySpawn,
            AudioClip enemyShoot,
            AudioClip enemyMelee,
            AudioClip hurt,
            AudioClip death,
            AudioClip button,
            AudioClip hover = null,
            AudioClip menuBgm = null,
            AudioClip combatBgm = null,
            AudioClip victory = null,
            AudioClip defeat = null,
            AudioClip whoosh = null,
            AudioClip punch = null,
            AudioClip mechanical = null)
        {
            playerShootClip = playerShoot;
            playerDeathClip = playerDeath;
            enemySpawnClip = enemySpawn;
            enemyShootClip = enemyShoot;
            enemyMeleeDamageClip = enemyMelee;
            enemyHurtClip = hurt;
            enemyDeathClip = death;
            uiButtonClickClip = button;
            uiButtonHoverClip = hover;
            menuBgmClip = menuBgm;
            combatBgmClip = combatBgm;
            victoryFanfareClip = victory;
            defeatClip = defeat;
            uiWhooshClip = whoosh;
            uiPunchClip = punch;
            uiMechanicalClip = mechanical;
        }
    }
}
