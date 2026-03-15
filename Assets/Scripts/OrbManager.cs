using UnityEngine;
using System.Collections.Generic;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;

namespace TaiChi
{
    public class OrbManager : MonoBehaviour
    {
        [Header("References")]
        public PoseLandmarkerRunner_edited Runner;
        public Camera MainCamera;

        [Header("Orb Prefabs")]
        public GameObject BlueOrbPrefab;  // Correct form (score >= threshold)
        public GameObject RedOrbPrefab;   // Incorrect form (score < threshold)

        [Header("Settings")]
        public float VisibilityThreshold = 0.5f;
        public float OrbDepth = 1.0f;
        public float ScoreThreshold = 0.80f;

        // 8 segments mapped to actual JSON keys from groupmate
        private static readonly (int a, int b, string scoreKey, string label)[] Segments = {
            (11, 13, "left_arm_upper_arm",  "L_UpperArm"),
            (12, 14, "right_arm_upper_arm", "R_UpperArm"),
            (13, 15, "left_arm_forearm",    "L_Forearm"),
            (14, 16, "right_arm_forearm",   "R_Forearm"),
            (23, 25, "left_leg_thigh",      "L_Thigh"),
            (24, 26, "right_leg_thigh",     "R_Thigh"),
            (25, 27, "left_leg_shin",       "L_Shin"),
            (26, 28, "right_leg_shin",      "R_Shin"),
        };

        private struct LandmarkData
        {
            public float x, y, visibility;
        }

        private GameObject[] _orbs;
        private bool[] _isCorrect; // true = blue, false = red

        private readonly Queue<LandmarkData[]> _resultQueue = new Queue<LandmarkData[]>();
        private readonly object _lock = new object();

        private void Awake()
        {
            Debug.Log("[OrbManager] Awake called.");
        }

        private void Start()
        {
            if (MainCamera == null)
                MainCamera = Camera.main;

            _orbs = new GameObject[Segments.Length];

            // Default all orbs to blue
            _isCorrect = new bool[Segments.Length];
            for (int i = 0; i < _isCorrect.Length; i++)
                _isCorrect[i] = true;

            if (Runner == null)
                Runner = Object.FindFirstObjectByType<PoseLandmarkerRunner_edited>();

            if (Runner != null)
            {
                Runner.OnResultOutput += HandleResult;
                Debug.Log("[OrbManager] Subscribed to PoseLandmarkerRunner_edited.");
            }
            else
            {
                Debug.LogError("[OrbManager] PoseLandmarkerRunner_edited not found!");
            }

            // Subscribe to fake data simulator
            FakeDataSimulator.OnFakeScoresUpdated += HandleScoresUpdated;
            Debug.Log("[OrbManager] Subscribed to FakeDataSimulator.");
        }

        private void OnDestroy()
        {
            if (Runner != null)
                Runner.OnResultOutput -= HandleResult;

            FakeDataSimulator.OnFakeScoresUpdated -= HandleScoresUpdated;
            DestroyAllOrbs();
        }

        // Called every UpdateInterval seconds by FakeDataSimulator
        // (Later replaced by real MQTT data)
        private void HandleScoresUpdated(Dictionary<string, float> scores)
        {
            for (int i = 0; i < Segments.Length; i++)
            {
                string key = Segments[i].scoreKey;
                if (!scores.ContainsKey(key)) continue;

                float score = scores[key];
                bool wasCorrect = _isCorrect[i];
                _isCorrect[i] = score >= ScoreThreshold;

                // Destroy orb so it respawns with correct color prefab
                if (wasCorrect != _isCorrect[i])
                {
                    Debug.Log($"[OrbManager] {Segments[i].label} → {(_isCorrect[i] ? "BLUE" : "RED")} (score:{score:F2})");
                    if (_orbs[i] != null)
                    {
                        Destroy(_orbs[i]);
                        _orbs[i] = null;
                    }
                }
            }
        }

        // Background thread — copy data immediately
        private void HandleResult(PoseLandmarkerResult result)
        {
            if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
            {
                lock (_lock) { _resultQueue.Enqueue(null); }
                return;
            }

            var landmarks = result.poseLandmarks[0].landmarks;
            if (landmarks == null || landmarks.Count < 29)
            {
                lock (_lock) { _resultQueue.Enqueue(null); }
                return;
            }

            var copy = new LandmarkData[landmarks.Count];
            for (int i = 0; i < landmarks.Count; i++)
            {
                copy[i] = new LandmarkData
                {
                    x = landmarks[i].x,
                    y = landmarks[i].y,
                    visibility = landmarks[i].visibility.GetValueOrDefault(0f)
                };
            }

            lock (_lock) { _resultQueue.Enqueue(copy); }
        }

        // Main thread
        private void Update()
        {
            while (true)
            {
                LandmarkData[] landmarks = null;
                bool hasItem = false;

                lock (_lock)
                {
                    if (_resultQueue.Count > 0)
                    {
                        landmarks = _resultQueue.Dequeue();
                        hasItem = true;
                    }
                }

                if (!hasItem) break;

                if (landmarks == null)
                    DestroyAllOrbs();
                else
                    PlaceOrbs(landmarks);
            }
        }

        private void PlaceOrbs(LandmarkData[] landmarks)
        {
            for (int i = 0; i < Segments.Length; i++)
            {
                var lmA = landmarks[Segments[i].a];
                var lmB = landmarks[Segments[i].b];

                if (lmA.visibility < VisibilityThreshold || lmB.visibility < VisibilityThreshold)
                {
                    if (_orbs[i] != null) { Destroy(_orbs[i]); _orbs[i] = null; }
                    continue;
                }

                float midX = (lmA.x + lmB.x) / 2f;
                float midY = (lmA.y + lmB.y) / 2f;
                Vector3 worldPos = NormalizedToWorld(midX, midY);

                // Pick prefab based on current score state
                GameObject prefab = _isCorrect[i] ? BlueOrbPrefab : RedOrbPrefab;

                if (_orbs[i] == null)
                {
                    _orbs[i] = Instantiate(prefab, worldPos, Quaternion.identity);
                    _orbs[i].name = $"Orb_{Segments[i].label}";
                }
                else
                {
                    _orbs[i].transform.position = worldPos;
                }
            }
        }

        private Vector3 NormalizedToWorld(float normX, float normY)
        {
            float viewX, viewY;

            if (Application.isMobilePlatform)
            {
                viewX = 1f - normY;
                viewY = 1f - normX;
            }
            else
            {
                viewX = normX;
                viewY = 1f - normY;
            }

            return MainCamera.ViewportToWorldPoint(new Vector3(viewX, viewY, OrbDepth));
        }

        private void DestroyAllOrbs()
        {
            if (_orbs == null) return;
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i] != null) { Destroy(_orbs[i]); _orbs[i] = null; }
            }
        }
    }
}