using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using SurvivalShooter.Audio;

namespace SurvivalShooter.UI
{
    /// <summary>
    /// Audio settings overlay: one slider per channel (Master, Music, Sound Effects, Enemy Audio,
    /// Interface) plus Mute All and Reset. Values are saved instantly and applied live.
    /// Opened from the main menu (gear) and from the pause menu.
    /// </summary>
    public class SettingsScreen : UIScreen
    {
        [Serializable]
        public class ChannelRow
        {
            public AudioChannel channel;
            public Slider slider;
            public TMP_Text valueText;
            public Image fill;
            public Image icon;
        }

        [SerializeField] private ChannelRow[] rows;
        [SerializeField] private Button doneButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button muteButton;
        [SerializeField] private TMP_Text muteLabel;
        [SerializeField] private Image muteIcon;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;
        [SerializeField] private CanvasGroup slidersGroup;

        [SerializeField] private Color accent = new Color(0.25f, 0.91f, 1f);
        [SerializeField] private Color mutedColor = new Color(0.45f, 0.5f, 0.56f);

        private bool refreshing;

        protected override void Awake()
        {
            base.Awake();
            foreach (var row in rows)
            {
                ChannelRow r = row;
                r.slider.minValue = 0f;
                r.slider.maxValue = 1f;
                r.slider.wholeNumbers = false;
                r.slider.onValueChanged.AddListener(v => OnSliderChanged(r, v));
            }
            doneButton.onClick.AddListener(() => UIManager.Instance.HideSettings());
            resetButton.onClick.AddListener(() =>
            {
                VolumeSettings.ResetToDefaults();
                Refresh(true);
            });
            muteButton.onClick.AddListener(() =>
            {
                VolumeSettings.Muted = !VolumeSettings.Muted;
                Refresh(false);
            });
        }

        protected override void OnShown() => Refresh(true);

        private void OnSliderChanged(ChannelRow row, float value)
        {
            UpdateLabel(row, value);
            if (refreshing) return;
            VolumeSettings.Set(row.channel, value);
            if (VolumeSettings.Muted && value > 0f) { VolumeSettings.Muted = false; RefreshMute(); }
            if (AudioManager.HasInstance) AudioManager.Instance.PreviewChannel(row.channel);
        }

        private void Refresh(bool animate)
        {
            refreshing = true;
            foreach (var row in rows)
            {
                float v = VolumeSettings.Get(row.channel);
                DOTween.Kill(row.slider);
                if (animate && isActiveAndEnabled)
                {
                    row.slider.SetValueWithoutNotify(0f);
                    DOTween.To(() => row.slider.value, x => { row.slider.SetValueWithoutNotify(x); UpdateLabel(row, x); }, v, 0.45f)
                        .SetEase(Ease.OutCubic).SetUpdate(true).SetTarget(row.slider);
                }
                else
                {
                    row.slider.SetValueWithoutNotify(v);
                    UpdateLabel(row, v);
                }
            }
            refreshing = false;
            RefreshMute();
        }

        private void UpdateLabel(ChannelRow row, float value)
        {
            if (row.valueText != null) row.valueText.text = Mathf.RoundToInt(value * 100f) + "%";
            bool silent = VolumeSettings.Muted || value <= 0.001f;
            Color c = silent ? mutedColor : accent;
            if (row.fill != null) row.fill.color = c;
            if (row.icon != null) row.icon.color = c;
        }

        private void RefreshMute()
        {
            bool muted = VolumeSettings.Muted;
            if (muteLabel != null) muteLabel.text = muted ? "UNMUTE ALL" : "MUTE ALL";
            if (muteIcon != null) muteIcon.sprite = muted ? soundOffSprite : soundOnSprite;
            if (slidersGroup != null) slidersGroup.DOFade(muted ? 0.45f : 1f, 0.2f).SetUpdate(true);
            foreach (var row in rows) UpdateLabel(row, row.slider.value);
        }
    }
}
