using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using SurvivalShooter.Core;
using SurvivalShooter.Pooling;

namespace SurvivalShooter.Audio
{
    public enum SoundId
    {
        // Mandatory gameplay sounds
        PlayerShoot,
        PlayerDeath,
        EnemySpawn,
        EnemyShoot,
        MeleeAttack,        // "Enemy damage sound (Melee enemy attack)"
        // Extra feedback
        PlayerHurt,
        EnemyHurt,
        EnemyDeath,
        ArenaPlaced,
        PlaneFound,
        CountdownBeep,
        CountdownGo,
        UIClick,
        UIHover,
        UIBack,
        Defeat
    }

    public enum MusicTrack
    {
        None,
        Menu,
        Combat,
        Victory
    }

    [Serializable]
    public class SoundDefinition
    {
        public SoundId id;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
        [Tooltip("True = played in 3D at the world position (enemy sounds)")]
        public bool spatial;
    }

    /// <summary>
    /// Central audio system (Singleton). Instead of adding an AudioSource to every enemy/bullet,
    /// it uses: 1 music source pair (cross-fade), 1 UI source, 1 player source (2D) and a small
    /// pool of 3D emitters for positional enemy sounds. This avoids duplicated components.
    /// </summary>
    [DefaultExecutionOrder(-95)]
    public class AudioManager : Singleton<AudioManager>
    {
        [SerializeField] private List<SoundDefinition> sounds = new List<SoundDefinition>();

        [Header("Music")]
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip combatMusic;
        [SerializeField] private AudioClip victoryMusic;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.4f;

        [Header("3D Emitters")]
        [SerializeField] private AudioEmitter emitterPrefab;
        [SerializeField] private int emitterCount = 10;

        private readonly Dictionary<SoundId, SoundDefinition> lookup = new Dictionary<SoundId, SoundDefinition>();
        private AudioSource musicA, musicB, uiSource, playerSource;
        private bool musicAActive = true;
        private MusicTrack currentTrack = MusicTrack.None;
        private ObjectPool<AudioEmitter> emitters;
        private float musicBaseVolume;     // track level before user settings
        private float pauseMultiplier = 1f;
        private float nextPreviewTime;

        protected override void OnSingletonAwake()
        {
            foreach (var s in sounds)
            {
                if (s != null) lookup[s.id] = s;
            }

            musicA = CreateSource("Music A", true);
            musicB = CreateSource("Music B", true);
            uiSource = CreateSource("UI", false);
            playerSource = CreateSource("Player", false);

            if (emitterPrefab != null)
            {
                Transform container = new GameObject("3D Emitters").transform;
                container.SetParent(transform, false);
                emitters = new ObjectPool<AudioEmitter>(emitterPrefab, container, emitterCount);
            }
        }

        private void OnEnable() => VolumeSettings.Changed += ApplyMusicVolume;
        private void OnDisable() => VolumeSettings.Changed -= ApplyMusicVolume;

        private AudioSource CreateSource(string label, bool loop)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            src.spatialBlend = 0f;
            src.ignoreListenerPause = loop || label == "UI"; // music + UI keep playing while paused
            return src;
        }

        /// <summary>Which user-controlled volume channel a sound belongs to.</summary>
        public static AudioChannel ChannelOf(SoundId id)
        {
            switch (id)
            {
                case SoundId.EnemySpawn:
                case SoundId.EnemyShoot:
                case SoundId.MeleeAttack:
                case SoundId.EnemyHurt:
                case SoundId.EnemyDeath:
                    return AudioChannel.Enemy;
                case SoundId.UIClick:
                case SoundId.UIHover:
                case SoundId.UIBack:
                    return AudioChannel.Interface;
                default:
                    return AudioChannel.SoundEffects;
            }
        }

        public void Play(SoundId id) => Play(id, null);

        public void Play(SoundId id, Vector3? worldPosition)
        {
            if (!lookup.TryGetValue(id, out SoundDefinition def) || def.clips == null || def.clips.Length == 0) return;
            float volume = def.volume * VolumeSettings.Effective(ChannelOf(id));
            if (volume <= 0.001f) return;
            AudioClip clip = def.clips[UnityEngine.Random.Range(0, def.clips.Length)];
            if (clip == null) return;
            float pitch = UnityEngine.Random.Range(def.pitchRange.x, def.pitchRange.y);

            if (def.spatial && worldPosition.HasValue && emitters != null)
            {
                AudioEmitter e = emitters.Get(worldPosition.Value, Quaternion.identity);
                e.Play(clip, volume, pitch, emitters);
                return;
            }

            AudioSource src = IsPlayerSound(id) ? playerSource : uiSource;
            src.pitch = pitch;
            src.PlayOneShot(clip, volume);
        }

        /// <summary>
        /// Short sample so the player hears the level while dragging a slider. Played through the
        /// 2D interface source (which ignores pause) but at the chosen channel's volume, so it also
        /// works from the pause menu.
        /// </summary>
        public void PreviewChannel(AudioChannel channel)
        {
            if (channel == AudioChannel.Music || Time.unscaledTime < nextPreviewTime) return; // music plays live
            SoundId id = channel switch
            {
                AudioChannel.SoundEffects => SoundId.PlayerShoot,
                AudioChannel.Enemy => SoundId.EnemySpawn,
                _ => SoundId.UIClick
            };
            nextPreviewTime = Time.unscaledTime + (channel == AudioChannel.Enemy ? 0.6f : 0.18f);
            if (!lookup.TryGetValue(id, out SoundDefinition def) || def.clips == null || def.clips.Length == 0) return;
            AudioClip clip = def.clips[UnityEngine.Random.Range(0, def.clips.Length)];
            float volume = def.volume * VolumeSettings.Effective(channel);
            if (clip == null || volume <= 0.001f) return;
            uiSource.pitch = 1f;
            uiSource.PlayOneShot(clip, volume);
        }

        private static bool IsPlayerSound(SoundId id) =>
            id == SoundId.PlayerShoot || id == SoundId.PlayerHurt || id == SoundId.PlayerDeath;

        private float MusicTarget => musicBaseVolume * pauseMultiplier * VolumeSettings.Effective(AudioChannel.Music);

        public void PlayMusic(MusicTrack track, float fade = 0.8f)
        {
            if (track == currentTrack) return;
            currentTrack = track;

            AudioClip clip = track switch
            {
                MusicTrack.Menu => menuMusic,
                MusicTrack.Combat => combatMusic,
                MusicTrack.Victory => victoryMusic,
                _ => null
            };

            AudioSource from = musicAActive ? musicA : musicB;
            AudioSource to = musicAActive ? musicB : musicA;
            musicAActive = !musicAActive;

            from.DOKill();
            from.DOFade(0f, fade).SetUpdate(true).OnComplete(from.Stop);

            to.DOKill();
            if (clip == null) return;
            to.clip = clip;
            to.loop = track != MusicTrack.Victory;
            to.volume = 0f;
            to.Play();
            musicBaseVolume = track == MusicTrack.Menu ? musicVolume * 0.8f : musicVolume;
            to.DOFade(MusicTarget, fade).SetUpdate(true);
        }

        /// <summary>Re-applies the Music/Master setting to the track that is playing right now.</summary>
        private void ApplyMusicVolume()
        {
            AudioSource active = musicAActive ? musicA : musicB;
            if (active == null || !active.isPlaying) return;
            active.DOKill();
            active.volume = MusicTarget;
        }

        public void SetPaused(bool paused)
        {
            AudioListener.pause = paused;
            pauseMultiplier = paused ? 0.35f : 1f;
            AudioSource active = musicAActive ? musicA : musicB;
            if (active.isPlaying)
            {
                active.DOKill();
                active.DOFade(MusicTarget, 0.25f).SetUpdate(true);
            }
        }
    }
}
