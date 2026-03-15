using UnityEngine;
using System.Collections.Generic;

namespace TaiChi
{
    public class FakeDataSimulator : MonoBehaviour
    {
        [Header("Settings")]
        public float UpdateInterval = 1.0f;
        public float ScoreThreshold = 0.80f;

        [Tooltip("If true, randomly generates scores. If false uses ManualScore.")]
        public bool RandomScores = true;

        [Range(0f, 1f)]
        public float ManualScore = 0.85f;

        // Event that OrbManager listens to
        // Key = joint name, Value = score
        public static event System.Action<Dictionary<string, float>> OnFakeScoresUpdated;

        private float _timer = 0f;

        // Matches ACTUAL keys from groupmate's JSON
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
            var scores = new Dictionary<string, float>();

            foreach (var key in JointKeys)
            {
                scores[key] = RandomScores
                    ? Random.Range(0.5f, 1.0f)
                    : ManualScore;
            }

            // Log for debugging
            foreach (var kvp in scores)
                Debug.Log($"[FakeData] {kvp.Key}: {kvp.Value:F2} → {(kvp.Value >= ScoreThreshold ? "BLUE" : "RED")}");

            OnFakeScoresUpdated?.Invoke(scores);
        }
    }
}