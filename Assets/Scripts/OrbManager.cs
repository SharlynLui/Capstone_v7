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
        public GameObject BlueOrbPrefab;
        public GameObject RedOrbPrefab; 

        [Header("Settings")]
        public float VisibilityThreshold = 0.5f;
        public float OrbDepth = 1.0f;     // Distance from camera lens
        public float ZScale = 1.0f;       // Multiplier for depth movement
        public float ScoreThreshold = 0.80f;
        public float SmoothSpeed = 15f;

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
            public float x, y, z, visibility;
        }

        private GameObject[] _orbs;
        private bool[] _isCorrect;

        private readonly Queue<LandmarkData[]> _resultQueue = new Queue<LandmarkData[]>();
        private readonly object _lock = new object();


        private float[] _lastSwapTime;
        public float MinSwapInterval = 0.2f; // Orbs must stay a color for at least 200ms

        public GameObject GetOrbAtIndex(int index)
        {
            if (_orbs != null && index < _orbs.Length)
            {
                return _orbs[index];
            }
            return null;
        }

        private void Start()
        {
            if (MainCamera == null)
                MainCamera = Camera.main;

            _orbs = new GameObject[Segments.Length];
            _isCorrect = new bool[Segments.Length];
            for (int i = 0; i < _isCorrect.Length; i++) _isCorrect[i] = true;

            // MediaPipe Subscription
            if (Runner == null)
                Runner = Object.FindFirstObjectByType<PoseLandmarkerRunner_edited>();

            if (Runner != null)
                Runner.OnResultOutput += HandleResult;

            // --- KEY CHANGE HERE ---
            // Subscribe to the central Event Bus instead of the Fake Simulator directly
            ScoreEventBus.OnScoresUpdated += HandleScoresUpdated;
            Debug.Log("[OrbManager] Subscribed to ScoreEventBus.");


            _lastSwapTime = new float[Segments.Length];
        }

        private void OnDestroy()
        {
            if (Runner != null)
                Runner.OnResultOutput -= HandleResult;

            // --- KEY CHANGE HERE ---
            ScoreEventBus.OnScoresUpdated -= HandleScoresUpdated;
            DestroyAllOrbs();
        }

        private void HandleScoresUpdated(Dictionary<string, float> scores)
        {
            for (int i = 0; i < Segments.Length; i++)
            {
                string key = Segments[i].scoreKey;
                if (!scores.ContainsKey(key)) continue;

                float score = scores[key];
                bool newCorrectState = score >= ScoreThreshold;

                // ONLY swap if the state is different AND enough time has passed
                if (newCorrectState != _isCorrect[i])
                {
                    if (Time.time - _lastSwapTime[i] >= MinSwapInterval)
                    {
                        _isCorrect[i] = newCorrectState;
                        _lastSwapTime[i] = Time.time;

                        // Clear existing orb so it respawns with the new color
                        if (_orbs[i] != null)
                        {
                            Destroy(_orbs[i]);
                            _orbs[i] = null;
                        }
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
                    z = landmarks[i].z,
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
                    _orbs[i].transform.position = Vector3.Lerp(_orbs[i].transform.position, worldPos, Time.deltaTime * SmoothSpeed);
                }
            }
        }

        private Vector3 NormalizedToWorld(float normX, float normY, float normZ)
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

        public Vector3 GetFootMidpoint()
        {
            // Index 6 and 7 in your 'Segments' array are the Shins/Ankles
            if (_orbs[6] != null && _orbs[7] != null)
            {
                return (_orbs[6].transform.position + _orbs[7].transform.position) / 2f;
            }

            // Fallback: If orbs aren't active, return zero
            return Vector3.zero;
        }
    }
}