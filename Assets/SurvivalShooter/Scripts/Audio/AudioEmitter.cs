using UnityEngine;
using SurvivalShooter.Pooling;

namespace SurvivalShooter.Audio
{
    /// <summary>Pooled 3D AudioSource used for positional enemy sounds.</summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioEmitter : MonoBehaviour, IPoolable
    {
        private AudioSource source;
        private ObjectPool<AudioEmitter> pool;
        private float releaseAt;
        private bool playing;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 1.2f;
            source.maxDistance = 20f;
            source.dopplerLevel = 0f;
        }

        public void Play(AudioClip clip, float volume, float pitch, ObjectPool<AudioEmitter> owner)
        {
            pool = owner;
            source.clip = clip;
            source.volume = volume;
            source.pitch = pitch;
            source.Play();
            releaseAt = Time.unscaledTime + clip.length / Mathf.Max(0.1f, pitch) + 0.05f;
            playing = true;
        }

        public void OnSpawned() { }

        public void OnDespawned()
        {
            playing = false;
            if (source != null) source.Stop();
        }

        private void Update()
        {
            if (playing && Time.unscaledTime >= releaseAt && !AudioListener.pause)
            {
                playing = false;
                pool?.Release(this);
            }
        }
    }
}
