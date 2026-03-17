using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace TaiChi
{
    public class FakeDataSimulator : MonoBehaviour
    {
        [Header("Settings")]
        public float UpdateInterval = 1.0f;
        public float ScoreThreshold = 0.80f;

        [Tooltip("If true, randomly generates scores. If false uses ManualScore for all joints.")]
        public bool RandomScores = true;

        [Range(0f, 1f)]
        public float ManualScore = 0.85f;

        // MATCHES ACTUAL KEYS FROM MQTT LOGS
        private static readonly string[] JointKeys = {
            "right_arm_upper_arm",
            "left_arm_upper_arm",
            "right_arm_forearm",
            "left_arm_forearm",
            "right_leg_thigh",
            "left_leg_thigh",
            "right_leg_shin",
            "left_leg_shin",
        };

        private float _timer = 0f;
        private int _seq = 0;

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= UpdateInterval)
            {
                _timer = 0f;
                SendFakeScores();
            }
        }

        private void SendFakeScores()
        {
            _seq++;

            var partScores = new Dictionary<string, float>();
            float total = 0f;

            foreach (var key in JointKeys)
            {
                float score = RandomScores
                    ? Random.Range(0.5f, 1.0f)
                    : ManualScore;
                partScores[key] = score;
                total += score;
            }

            float overall = total / JointKeys.Length;

            // --- CRITICAL MATCHING SECTION ---
            // We build the JSON to match your MQTT logs: {"scores": {"overall": X, "part": { ... }}}
            string json = BuildMqttMatchedJson(_seq, overall, partScores);

            // This ensures the OrbManager reacts even if MQTT is disconnected
            ScoreEventBus.Publish(partScores);

            Debug.Log($"[FakeDataSimulator] Simulated MQTT Message:\n{json}");
        }

        private string BuildMqttMatchedJson(int seq, float overall, Dictionary<string, float> parts)
        {
            StringBuilder partBuilder = new StringBuilder();
            int count = 0;
            foreach (var kvp in parts)
            {
                partBuilder.Append($"\"{kvp.Key}\": {kvp.Value:F3}");
                if (count < parts.Count - 1) partBuilder.Append(", ");
                count++;
            }

            // This structure now matches your MQTTVisualizer/Router logic perfectly
            return "{\n" +
                   $"  \"seq\": {seq},\n" +
                   "  \"scores\": {\n" +
                   $"    \"overall\": {overall:F3},\n" +
                   "    \"part\": {\n" +
                   $"      {partBuilder.ToString().Replace(", ", ",\n      ")}\n" +
                   "    }\n" +
                   "  }\n" +
                   "}";
        }
    }
}