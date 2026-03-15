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

        [Header("Orb Prefab")]
        public GameObject OrbPrefab;

        [Header("Settings")]
        public float VisibilityThreshold = 0.5f;
        public float OrbDepth = 1.0f;

        // 8 segments: (landmarkA, landmarkB, name)
        private static readonly (int a, int b, string label)[] Segments = {
            (11, 13, "L_UpperArm"),
            (12, 14, "R_UpperArm"),
            (13, 15, "L_Forearm"),
            (14, 16, "R_Forearm"),
            (23, 25, "L_Thigh"),
            (24, 26, "R_Thigh"),
            (25, 27, "L_Calf"),
            (26, 28, "R_Calf"),
        };

        private struct LandmarkData
        {
            public float x, y, visibility;
        }

        private GameObject[] _orbs;
        private readonly Queue<LandmarkData[]> _resultQueue = new Queue<LandmarkData[]>();
        private readonly object _lock = new object();

        private void Start()
        {
            if (MainCamera == null)
                MainCamera = Camera.main;

            _orbs = new GameObject[Segments.Length];

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

                if (_orbs[i] == null)
                {
                    _orbs[i] = Instantiate(OrbPrefab, worldPos, Quaternion.identity);
                    _orbs[i].name = $"Orb_{Segments[i].label}";
                    Debug.Log($"[OrbManager] Spawned {_orbs[i].name}");
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

            // On Android, Landmark (0,0) is often the sensor's top-left, 
            // which is the screen's top-right or bottom-left in landscape.
            if (Application.isMobilePlatform)
            {
                // For Landscape Left (Home button on right):
                // Vertical hand movement (AI's X) -> Screen's Y
                // Horizontal hand movement (AI's Y) -> Screen's X
                viewX = 1f - normY;
                viewY = 1f - normX;
            }
            else
            {
                // Standard mapping for Laptop Webcam
                viewX = normX;
                viewY = 1f - normY;
            }

            Vector3 viewportPoint = new Vector3(viewX, viewY, OrbDepth);
            return MainCamera.ViewportToWorldPoint(viewportPoint);
        }

        private void DestroyAllOrbs()
        {
            if (_orbs == null) return;
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i] != null) { Destroy(_orbs[i]); _orbs[i] = null; }
            }
        }

        private void OnDestroy()
        {
            if (Runner != null)
                Runner.OnResultOutput -= HandleResult;
            DestroyAllOrbs();
        }
    }
}