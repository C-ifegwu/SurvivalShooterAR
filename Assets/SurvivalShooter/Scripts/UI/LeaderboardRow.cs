using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SurvivalShooter.Data;

namespace SurvivalShooter.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class LeaderboardRow : MonoBehaviour
    {
        [SerializeField] private TMP_Text indexText;
        [SerializeField] private TMP_Text dateText;
        [SerializeField] private TMP_Text difficultyText;
        [SerializeField] private Image difficultyChip;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text killsText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private Image background;
        [SerializeField] private GameObject latestTag;

        [SerializeField] private Color normalChip = new Color(0.25f, 0.91f, 1f, 0.22f);
        [SerializeField] private Color hardChip = new Color(1f, 0.25f, 0.38f, 0.25f);
        [SerializeField] private Color rowColor = new Color(1f, 1f, 1f, 0.05f);
        [SerializeField] private Color latestRowColor = new Color(0.25f, 0.91f, 1f, 0.12f);

        public void Bind(int index, SessionRecord r, bool isLatest, bool isBest)
        {
            indexText.text = index.ToString("00");
            dateText.text = r.Timestamp.ToString("dd MMM · HH:mm").ToUpperInvariant();
            difficultyText.text = r.difficulty;
            bool hard = r.difficulty != null && r.difficulty.ToUpperInvariant().Contains("HARD");
            if (difficultyChip != null) difficultyChip.color = hard ? hardChip : normalChip;
            if (resultText != null)
            {
                resultText.text = r.survived ? "SURVIVED" : "OVERRUN";
                resultText.color = r.survived ? new Color(0.24f, 1f, 0.63f) : new Color(1f, 0.4f, 0.5f);
            }
            killsText.text = r.kills.ToString();
            int m = Mathf.FloorToInt(r.timeSurvived / 60f);
            int s = Mathf.FloorToInt(r.timeSurvived % 60f);
            timeText.text = $"{m:00}:{s:00}";
            scoreText.text = isBest ? $"<color=#FFB23F>{r.score:N0}</color>" : r.score.ToString("N0");
            if (background != null) background.color = isLatest ? latestRowColor : rowColor;
            if (latestTag != null) latestTag.SetActive(isLatest);
        }
    }
}
