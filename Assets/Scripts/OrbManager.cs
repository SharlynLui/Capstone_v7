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
        public GameObject BlueOrbPrefab;  // Correct form
        public GameObject RedOrbPrefab;   // Incorrect form

        [Header("Settings")]
        public float VisibilityThreshold = 0.5f;
        public float OrbDepth = 1.0f;     // Distance from camera lens
        public float ZScale = 2.0f;       // Multiplier for depth movement (Adjust in Inspector)
        public float ScoreThreshold = 0.80f;
        public float SmoothSpeed = 15f;

        // 8 segments mapped to actual JSON keys
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
            public float x, y, z, visibility; // Added Z
        }

        private GameObject[] _orbs;
        private bool[] _isCorrect;

        private readonly Queue<LandmarkData[]> _resultQueue = new Queue<LandmarkData[]>();
        private readonly object _lock = new object();

        private void Start()
        {
            if (MainCamera == null)
                MainCamera = Camera.main;

            _orbs = new GameObject[Segments.Length];
            _isCorrect = new bool[Segments.Length];
            for (int i = 0; i < _isCorrect.Length; i++) _isCorrect[i] = true;

            if (Runner == null)
                Runner = Object.FindFirstObjectByType<PoseLandmarkerRunner_edited>();

            if (Runner != null)
                Runner.OnResultOutput += HandleResult;

            FakeDataSimulator.OnFakeScoresUpdated += HandleScoresUpdated;
        }

        private void OnDestroy()
        {
            if (Runner != null)
                Runner.OnResultOutput -= HandleResult;

            FakeDataSimulator.OnFakeScoresUpdated -= HandleScoresUpdated;
            DestroyAllOrbs();
        }

        private void HandleScoresUpdated(Dictionary<string, float> scores)
        {
            for (int i = 0; i < Segments.Length; i++)
            {
                string key = Segments[i].scoreKey;
                if (!scores.ContainsKey(key)) continue;

                float score = scores[key];
                bool wasCorrect = _isCorrect[i];
                _isCorrect[i] = score >= ScoreThreshold;

                if (wasCorrect != _isCorrect[i])
                {
                    if (_orbs[i] != null)
                    {
                        Destroy(_orbs[i]);
                        _orbs[i] = null;
                    }
                }
            }
        }

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
                    z = landmarks[i].z, // Capture AI Depth
                    visibility = landmarks[i].visibility.GetValueOrDefault(0f)
                };
            }

            lock (_lock) { _resultQueue.Enqueue(copy); }
        }

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

                // Average the X, Y, and Z for the segment center
                float midX = (lmA.x + lmB.x) / 2f;
                float midY = (lmA.y + lmB.y) / 2f;
                float midZ = (lmA.z + lmB.z) / 2f;

                Vector3 worldPos = NormalizedToWorld(midX, midY, midZ);

                GameObject prefab = _isCorrect[i] ? BlueOrbPrefab : RedOrbPrefab;

                if (_orbs[i] == null)
                {
                    _orbs[i] = Instantiate(prefab, worldPos, Quaternion.identity);
                    _orbs[i].name = $"Orb_{Segments[i].label}";
                }
                else
                {
                    // Smooth 3D movement using Lerp
                    _orbs[i].transform.position = Vector3.Lerp(_orbs[i].transform.position, worldPos, Time.deltaTime * SmoothSpeed);
                }
            }
        }

        private Vector3 NormalizedToWorld(float normX, float normY, float normZ)
        {
            float viewX, viewY;

            if (Application.isMobilePlatform)
            {
                // Orientation correction for Landscape Left
                viewX = 1f - normY;
                viewY = 1f - normX;
            }
            else
            {
                viewX = normX;
                viewY = 1f - normY;
            }

            // The Z value in ViewportToWorldPoint is the distance from camera lens.
            // normZ is relative to the hips, so we add it to our base depth.
            float worldZ = OrbDepth + (normZ * ZScale);

            return MainCamera.ViewportToWorldPoint(new Vector3(viewX, viewY, worldZ));
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