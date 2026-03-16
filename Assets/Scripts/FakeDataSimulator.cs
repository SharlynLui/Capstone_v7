using UnityEngine;
using System.Collections.Generic;

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

        // Matches ACTUAL keys from groupmate's JSON under "part scores"
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

        // Event fires with part scores dictionary
        public static event System.Action<Dictionary<string, float>> OnFakeScoresUpdated;

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

            // Build JSON matching EXACT format:
            // { "seq": 324, "overall score": 0.891, "part scores": { ... } }
            string json = BuildJson(_seq, overall, partScores);
            Debug.Log($"[FakeDataSimulator] seq:{_seq} overall:{overall:F3}\n{json}");

            OnFakeScoresUpdated?.Invoke(partScores);
        }

        private string BuildJson(int seq, float overall, Dictionary<string, float> parts)
        {
            var partBuilder = new System.Text.StringBuilder();
            int count = 0;
            foreach (var kvp in parts)
            {
                partBuilder.Append($"\"{kvp.Key}\": {kvp.Value:F3}");
                if (count < parts.Count - 1) partBuilder.Append(", ");
                count++;
            }

            // Exact format from groupmate
            return "{\n" +
                   $"  \"seq\": {seq},\n" +
                   $"  \"overall score\": {overall:F3},\n" +
                   $"  \"part scores\": {{\n" +
                   $"    {partBuilder.ToString().Replace(", ", ",\n    ")}\n" +
                   $"  }}\n" +
                   $"}}";
        }
    }
}