using UnityEngine;
using System.Collections.Generic;
using TMPro;

namespace TaiChi
{
    public class ScoreReader : MonoBehaviour
    {
        [Header("UI References")]
        public ImgsFillDynamic roundFillController;

        [Header("Settings")]
        public float animationSpeed = 1.0f;
        public float EnvThreshold = 0.8f; // Default 80%

        [Header("UI Display")]
        public TextMeshProUGUI EnvValueText; 

        // ── Environmental Effects Handler ────────────────────────────────────────────
        public void SetEnvThresholdFromSlider(float value)
        {
            EnvThreshold = value;

            // Update the % text at the side of the slider
            if (EnvValueText != null)
            {
                EnvValueText.text = (value * 100f).ToString("F0") + "%";
            }

            Debug.Log($"[Cheat] Env Effect Threshold changed to: {value * 100f:F0}%");
        }
        public void UpdateEnvironmentalState(float currentOverallScore)
        {
            // Use the adjustable EnvThreshold instead of a hardcoded 0.8f
            bool shouldTrigger = currentOverallScore >= EnvThreshold;

            if (EnvironmentalEffects.Instance != null)
            {
                EnvironmentalEffects.Instance.UpdateEnvironmentalState(shouldTrigger);
            }
        }

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

            // Update the Progress Bar UI
            if (roundFillController != null)
                roundFillController.SetValue(overall, false, animationSpeed);

            bool isAboveThreshold = overall >= EnvThreshold;

            if (EnvironmentalEffects.Instance != null)
            {
                // Only triggers if real data (overall) above cheat slider (EnvThreshold)
                EnvironmentalEffects.Instance.UpdateEnvironmentalState(isAboveThreshold);
            }
        }
    }
}