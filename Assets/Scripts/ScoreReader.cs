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
            float total = 0f;
            int count = 0;

            foreach (var kvp in scores)
            {
                total += kvp.Value;
                count++;
            }

            float overall = count > 0 ? total / count : 0f;

            // Update UI
            if (roundFillController != null)
                roundFillController.SetValue(overall, false, animationSpeed);

            // TRIGGER EFFECTS
            // We send the overall score and the 0.80f threshold to the manager
            if (EnvironmentEffects.Instance != null)
            {
                EnvironmentEffects.Instance.UpdateEnvironmentalState(overall >= 0.80f);
            }
        }
    }
}