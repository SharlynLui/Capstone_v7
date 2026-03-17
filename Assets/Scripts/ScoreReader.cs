using UnityEngine;
using System.Collections.Generic;

namespace TaiChi
{
    public class ScoreReader : MonoBehaviour
    {
        [Header("UI References")]
        public ImgsFillDynamic roundFillController;

        [Header("Settings")]
        public float animationSpeed = 1.0f;

        // ── Lifecycle ────────────────────────────────────────────────

        private void OnEnable()
        {
            ScoreEventBus.OnScoresUpdated += HandleScoresUpdated;
        }

        private void OnDisable()
        {
            ScoreEventBus.OnScoresUpdated -= HandleScoresUpdated;
        }

        // ── Score Handler ────────────────────────────────────────────

        private void HandleScoresUpdated(Dictionary<string, float> scores)
        {
            // Calculate overall as average of all part scores
            // (matches how groupmate computes overall in real MQTT data)
            float total = 0f;
            int count = 0;

            foreach (var kvp in scores)
            {
                total += kvp.Value;
                count++;
            }

            float overall = count > 0 ? total / count : 0f;

            Debug.Log($"[ScoreReader] Overall score: {overall:F2}");

            // Update progress bar
            if (roundFillController != null)
                roundFillController.SetValue(overall, false, animationSpeed);

            //// Trigger environment effects
            //if (EnvironmentEffects.Instance != null)
            //    EnvironmentEffects.Instance.TriggerEffect(overall, 0.80f);
        }
    }
}