using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using SurvivalShooter.AR;
using SurvivalShooter.Core;

namespace SurvivalShooter.UI
{
    /// <summary>Guides the player through plane scanning and tap-to-place.</summary>
    public class ScanningScreen : UIScreen
    {
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private RectTransform phoneIcon;
        [SerializeField] private Image statusDot;
        [SerializeField] private Image cardOutline;
        [SerializeField] private RectTransform tapPulse;

        [SerializeField] private Color searchingColor = new Color(1f, 0.7f, 0.22f);
        [SerializeField] private Color foundColor = new Color(0.24f, 1f, 0.63f);

        private Tween phoneTween;
        private Tween pulseTween;
        private Tween dotTween;

        protected override void Awake()
        {
            base.Awake();
            backButton.onClick.AddListener(() => GameManager.Instance.RequestMainMenu());
        }

        private void OnEnable() => GameEvents.PlaneStatusChanged += SetFound;
        private void OnDisable() => GameEvents.PlaneStatusChanged -= SetFound;

        protected override void OnShown()
        {
            SetFound(false);
            if (hintText != null)
            {
                hintText.text = "Point at the floor or a large table.\nMove slowly — good light helps.";
#if UNITY_EDITOR
                hintText.text += ARPlacementManager.IsXRRunning
                    ? "\n<size=80%><color=#8FA6B8>XR Simulation: arrow keys / RMB to look · WASD+RMB to move · click to place</color></size>"
                    : "\n<size=80%><color=#8FA6B8>Editor: arrow keys / RMB to look · WASD to move · click to place</color></size>";
#endif
            }
        }

        protected override void OnHidden()
        {
            phoneTween?.Kill();
            pulseTween?.Kill();
            dotTween?.Kill();
        }

        private void SetFound(bool found)
        {
            if (!IsVisible) return;

            titleText.text = found ? "SURFACE LOCKED" : "SCANNING FOR A FLOOR";
            bodyText.text = found ? "Tap anywhere to deploy the combat zone" : "Slowly sweep your phone across the ground";
            Color c = found ? foundColor : searchingColor;
            if (statusDot != null) statusDot.color = c;
            if (cardOutline != null) cardOutline.DOColor(new Color(c.r, c.g, c.b, 0.55f), 0.25f).SetUpdate(true);

            dotTween?.Kill();
            if (statusDot != null)
            {
                statusDot.rectTransform.localScale = Vector3.one;
                dotTween = statusDot.rectTransform.DOScale(1.35f, found ? 0.35f : 0.7f).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            }

            phoneTween?.Kill();
            if (phoneIcon != null)
            {
                phoneIcon.localRotation = Quaternion.identity;
                phoneIcon.anchoredPosition = new Vector2(0f, phoneIcon.anchoredPosition.y);
                if (!found)
                {
                    phoneIcon.localRotation = Quaternion.Euler(0, 0, 12f);
                    phoneTween = phoneIcon.DOLocalRotate(new Vector3(0, 0, -12f), 1.1f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
                }
            }

            pulseTween?.Kill();
            if (tapPulse != null)
            {
                tapPulse.gameObject.SetActive(found);
                if (found)
                {
                    tapPulse.localScale = Vector3.one * 0.6f;
                    var img = tapPulse.GetComponent<Image>();
                    Sequence s = DOTween.Sequence().SetUpdate(true).SetLoops(-1);
                    s.Append(tapPulse.DOScale(1.4f, 1f).SetEase(Ease.OutCubic));
                    if (img != null)
                    {
                        img.color = new Color(foundColor.r, foundColor.g, foundColor.b, 0.9f);
                        s.Join(img.DOFade(0f, 1f));
                    }
                    pulseTween = s;
                }
            }

            if (found && content != null)
            {
                content.DOKill(true);
                content.DOPunchScale(Vector3.one * 0.03f, 0.3f, 6, 0.5f).SetUpdate(true);
            }
        }
    }
}
