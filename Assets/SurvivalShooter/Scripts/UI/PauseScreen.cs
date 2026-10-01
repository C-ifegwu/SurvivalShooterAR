using UnityEngine;
using UnityEngine.UI;
using SurvivalShooter.Core;

namespace SurvivalShooter.UI
{
    public class PauseScreen : UIScreen
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Button settingsButton;

        protected override void Awake()
        {
            base.Awake();
            resumeButton.onClick.AddListener(() => GameManager.Instance.RequestResume());
            restartButton.onClick.AddListener(() => GameManager.Instance.RequestRestart());
            menuButton.onClick.AddListener(() => GameManager.Instance.RequestMainMenu());
            if (settingsButton != null) settingsButton.onClick.AddListener(() => UIManager.Instance.ShowSettings());
        }
    }
}
