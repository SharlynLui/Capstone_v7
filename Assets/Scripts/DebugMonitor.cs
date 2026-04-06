using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Text;

namespace TaiChi
{
    public class DebugMonitor : MonoBehaviour
    {
        public TextMeshProUGUI DebugText; // The big text block in the panel
        
        private void OnEnable() => ScoreEventBus.OnScoresUpdated += UpdateDebugDisplay;
        private void OnDisable() => ScoreEventBus.OnScoresUpdated -= UpdateDebugDisplay;

        private void UpdateDebugDisplay(Dictionary<string, float> scores)
        {
            if (DebugText == null) return;

            // Pull overall directly from dictionary, fallback to average if missing
            float overall;
            if (!scores.TryGetValue("overall", out overall))
            {
                float total = 0f;
                foreach (var kvp in scores) total += kvp.Value;
                overall = scores.Count > 0 ? total / scores.Count : 0f;
            }

            string overallColor = overall >= 0.8f ? "#00FF00" : overall >= 0.5f ? "#FFD700" : "#FF4444";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<color=#FFD700><b>LIVE POSE DATA</b></color>");
            sb.AppendLine($"<size=80%>Timestamp: {Time.time:F2}s</size>\n");
            sb.AppendLine($"OVERALL : <color={overallColor}><b>{overall:F3}</b></color>");
            sb.AppendLine("─────────────────────────");

            foreach (var kvp in scores)
            {
                if (kvp.Key == "overall") continue; // already shown above
                string color = kvp.Value >= 0.8f ? "#00FF00" : "#FFFFFF";
                sb.AppendLine($"{kvp.Key.PadRight(15)} : <color={color}>{kvp.Value:F3}</color>");
            }

            DebugText.text = sb.ToString();
        }

        public void ClosePanel()
        {
            // Don't hide the panel yourself — let ToggleDebugPanel handle everything
            if (StartScreenController.Instance != null)
                StartScreenController.Instance.ToggleDebugPanel();
        }
    }
}