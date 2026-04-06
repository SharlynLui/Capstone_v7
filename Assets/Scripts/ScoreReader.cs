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
        public UnityEngine.UI.Slider EnvSlider;

        [Header("Debug Display")]
        public TextMeshProUGUI MqttDebugText;

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
        public void ResetToDefaults()
        {
            // Set the internal logic
            EnvThreshold = 0.8f;

            // Sync the physical Slider handle
            if (EnvSlider != null) EnvSlider.value = 0.8f;

            // Sync the initial text box
            if (EnvValueText != null) EnvValueText.text = "Default";
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

        // WITHOUT MQTT DEBUG 
        //private void HandleScoresUpdated(Dictionary<string, float> scores)
        //{
        //    float total = 0f;
        //    int count = 0;

        //    foreach (var kvp in scores)
        //    {
        //        total += kvp.Value;
        //        count++;
        //    }

        //    float overall = count > 0 ? total / count : 0f;

        //    // Update the Progress Bar UI
        //    if (roundFillController != null)
        //        roundFillController.SetValue(overall, false, animationSpeed);

        //    bool isAboveThreshold = overall >= EnvThreshold;

        //    if (EnvironmentalEffects.Instance != null)
        //    {
        //        // Only triggers if real data (overall) above cheat slider (EnvThreshold)
        //        EnvironmentalEffects.Instance.UpdateEnvironmentalState(isAboveThreshold);
        //    }
        //}
        private void HandleScoresUpdated(Dictionary<string, float> scores)
        {
            float overall;

            if (scores.TryGetValue("overall", out float providedOverall))
            {
                overall = providedOverall; // Use the JSON-provided overall directly
            }
            else
            {
                // Fallback: average all keys (for FakeDataSimulator which has no overall key)
                float total = 0f;
                foreach (var kvp in scores)
                    total += kvp.Value;
                overall = scores.Count > 0 ? total / scores.Count : 0f;
            }

            // 1. UPDATE THE DEBUG TEXT IMMEDIATELY (50Hz)
            if (MqttDebugText != null)
            {
                // "F2" ensures you see the 0.44 precision
                MqttDebugText.text = $"MQTT IN: {overall:F2} ({(overall * 100f):F0}%)";

                // Visual indicator: Green if passing, White if failing
                MqttDebugText.color = overall >= EnvThreshold ? Color.green : Color.white;
            }

            // 2. Update the Progress Circle (Main UI)
            if (roundFillController != null)
                roundFillController.SetValue(overall, false, animationSpeed);

            // 3. Trigger Effects check
            if (EnvironmentalEffects.Instance != null)
            {
                EnvironmentalEffects.Instance.UpdateEnvironmentalState(overall >= EnvThreshold);
            }
        }
    }
}