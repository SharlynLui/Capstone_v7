using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TaiChi
{
    public class SessionSummaryPanel : MonoBehaviour
    {
        [Header("Text Fields")]
        public TextMeshProUGUI SummaryText;

        private void Start()
        {
            gameObject.SetActive(false);
        }

        public void Show(SessionResult result)
        {
            gameObject.SetActive(true);

            int minutes = Mathf.FloorToInt(result.Duration / 60f);
            int seconds = Mathf.FloorToInt(result.Duration % 60f);

            int peakMin = Mathf.FloorToInt(result.PeakScoreTime / 60f);
            int peakSec = Mathf.FloorToInt(result.PeakScoreTime % 60f);

            string consistency;
            string consistencyColor;
            if (result.Variance < 0.01f)
            {
                consistency = "High";
                consistencyColor = "#00FF00";
            }
            else if (result.Variance < 0.03f)
            {
                consistency = "Medium";
                consistencyColor = "#FFD700";
            }
            else
            {
                consistency = "Low";
                consistencyColor = "#FF4444";
            }

            var sorted = result.JointAverages
                .OrderBy(kvp => kvp.Value)
                .ToList();

            StringBuilder sb = new StringBuilder();

            //// ── Header ────────────────────────────────────────────
            //sb.AppendLine("<color=#FFD700><size=120%><b>SESSION SUMMARY</b></size></color>");
            //sb.AppendLine();

            // ── Overall Score (large, prominent) ──────────────────
            string overallColor = ScoreHexColor(result.AverageOverall);
            sb.AppendLine("<color=#AAAAAA>OVERALL SCORE</color>");
            sb.AppendLine($"<color={overallColor}><size=150%><b>{result.AverageOverall * 100f:F1}%</b></size></color>");
            sb.AppendLine();

            // ── Session Stats ─────────────────────────────────────
            sb.AppendLine("<color=#FFD700><b>── Session Stats ──</b></color>");
            sb.AppendLine($"<color=#AAAAAA>Duration   </color>  {minutes:D2}:{seconds:D2}");
            sb.AppendLine($"<color=#AAAAAA>Peak Score </color>  <color={ScoreHexColor(result.PeakScore)}>{result.PeakScore * 100f:F1}%</color>  at  {peakMin:D2}:{peakSec:D2}");
            sb.AppendLine($"<color=#AAAAAA>Consistency</color>  <color={consistencyColor}><b>{consistency}</b></color>");
            sb.AppendLine();

            // ── Joint Breakdown ───────────────────────────────────
            sb.AppendLine("<color=#FFD700><b>── Joint Breakdown ──</b></color>");
            foreach (var kvp in sorted)
            {
                string color = ScoreHexColor(kvp.Value);
                string name = FormatJointName(kvp.Key).PadRight(22);
                string bar = ScoreBar(kvp.Value);
                sb.AppendLine($"<color=#CCCCCC>{name}</color><color={color}>{bar}  {kvp.Value * 100f:F1}%</color>");
            }
            sb.AppendLine();

            // ── Focus Areas ───────────────────────────────────────
            sb.AppendLine("<color=#FFD700><b>── Focus Areas ──</b></color>");
            int rank = 1;
            foreach (var kvp in sorted.Take(3))
            {
                string color = ScoreHexColor(kvp.Value);
                sb.AppendLine($"<color=#AAAAAA>{rank}.</color> <color=#CCCCCC>{FormatJointName(kvp.Key)}</color>  <color={color}>{kvp.Value * 100f:F1}%</color>");
                rank++;
            }

            SummaryText.text = sb.ToString();
        }

        public void ClosePanel()
        {
            gameObject.SetActive(false);
            if (StartScreenController.Instance != null)
                StartScreenController.Instance.ReturnToMenu();
        }

        // ── Helpers ───────────────────────────────────────────────
        private string ScoreHexColor(float score)
        {
            if (score >= 0.8f) return "#00FF00";
            if (score >= 0.5f) return "#FFD700";
            return "#FF4444";
        }

        private string ScoreBar(float score)
        {
            // 10 segment bar using filled/empty blocks
            int filled = Mathf.RoundToInt(score * 10f);
            string bar = "";
            for (int i = 0; i < 10; i++)
                bar += i < filled ? "█" : "░";
            return bar;
        }

        private string FormatJointName(string key)
        {
            return System.Globalization.CultureInfo.CurrentCulture
                .TextInfo.ToTitleCase(key.Replace("_", " "));
        }
    }
}