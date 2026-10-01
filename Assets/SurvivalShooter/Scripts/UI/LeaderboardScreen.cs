using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using SurvivalShooter.Data;

namespace SurvivalShooter.UI
{
    /// <summary>Shows the latest 5 sessions stored on the device (newest first).</summary>
    public class LeaderboardScreen : UIScreen
    {
        [SerializeField] private LeaderboardRow[] rows;
        [SerializeField] private GameObject emptyState;
        [SerializeField] private TMP_Text bestText;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button clearButton;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() => UIManager.Instance.HideLeaderboard());
            if (clearButton != null)
                clearButton.onClick.AddListener(() =>
                {
                    if (LeaderboardManager.HasInstance) LeaderboardManager.Instance.Clear();
                    Populate();
                });
        }

        protected override void OnShown() => Populate();

        private void Populate()
        {
            IReadOnlyList<SessionRecord> sessions = LeaderboardManager.HasInstance
                ? LeaderboardManager.Instance.Sessions
                : new List<SessionRecord>();

            int best = LeaderboardManager.HasInstance ? LeaderboardManager.Instance.BestScore : 0;
            if (bestText != null) bestText.text = best > 0 ? $"ALL-TIME BEST  <color=#3FE8FF>{best:N0}</color>" : "ALL-TIME BEST  —";

            if (emptyState != null) emptyState.SetActive(sessions.Count == 0);
            if (clearButton != null) clearButton.gameObject.SetActive(sessions.Count > 0);

            for (int i = 0; i < rows.Length; i++)
            {
                bool has = i < sessions.Count;
                rows[i].gameObject.SetActive(has);
                if (!has) continue;
                rows[i].Bind(i + 1, sessions[i], i == 0, sessions[i].score == best && best > 0);

                RectTransform rt = (RectTransform)rows[i].transform;
                rt.DOKill(true);
                CanvasGroup cg = rows[i].GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 0f;
                    cg.DOFade(1f, 0.3f).SetDelay(0.1f + i * 0.06f).SetUpdate(true);
                }
            }
        }
    }
}
