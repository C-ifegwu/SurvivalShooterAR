using System;
using UnityEngine;

namespace SurvivalShooter.Audio
{
    /// <summary>The independent audio channels the player can control.</summary>
    public enum AudioChannel
    {
        Master,        // everything
        Music,         // background music
        SoundEffects,  // player weapon, hits, countdown, placement
        Enemy,         // zombie/soldier voices, shots, bites, deaths
        Interface      // buttons & menu sounds
    }

    /// <summary>
    /// Persistent volume settings (0–1 per channel + mute) stored in PlayerPrefs so they survive
    /// app restarts. Raises <see cref="Changed"/> so the AudioManager and UI update live (Observer).
    /// </summary>
    public static class VolumeSettings
    {
        public static event Action Changed;

        private const string Prefix = "SSAR_VOLUME_";
        private const string MuteKey = "SSAR_MUTED";
        private static readonly float[] Defaults = { 1f, 0.7f, 0.9f, 0.9f, 0.8f };
        private static float[] values;
        private static bool muted;

        public static int ChannelCount => Defaults.Length;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Changed = null;
            values = null;
        }

        private static void EnsureLoaded()
        {
            if (values != null) return;
            values = new float[Defaults.Length];
            for (int i = 0; i < values.Length; i++)
                values[i] = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + (AudioChannel)i, Defaults[i]));
            muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
        }

        public static float Get(AudioChannel channel)
        {
            EnsureLoaded();
            return values[(int)channel];
        }

        public static float GetDefault(AudioChannel channel) => Defaults[(int)channel];

        public static void Set(AudioChannel channel, float value)
        {
            EnsureLoaded();
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(values[(int)channel], value)) return;
            values[(int)channel] = value;
            PlayerPrefs.SetFloat(Prefix + channel, value);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static bool Muted
        {
            get { EnsureLoaded(); return muted; }
            set
            {
                EnsureLoaded();
                if (muted == value) return;
                muted = value;
                PlayerPrefs.SetInt(MuteKey, value ? 1 : 0);
                PlayerPrefs.Save();
                Changed?.Invoke();
            }
        }

        /// <summary>Final multiplier for a channel = mute × master × channel.</summary>
        public static float Effective(AudioChannel channel)
        {
            EnsureLoaded();
            if (muted) return 0f;
            float master = values[(int)AudioChannel.Master];
            return channel == AudioChannel.Master ? master : master * values[(int)channel];
        }

        public static void ResetToDefaults()
        {
            EnsureLoaded();
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = Defaults[i];
                PlayerPrefs.SetFloat(Prefix + (AudioChannel)i, Defaults[i]);
            }
            muted = false;
            PlayerPrefs.SetInt(MuteKey, 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
