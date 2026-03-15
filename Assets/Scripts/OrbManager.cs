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
        public float OrbDepth = 1f;
        public float VisibilityThreshold = 0.5f;

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

        private GameObject[] _orbs;

        // ── Thread-safe queue ────────────────────────────────────────
        // MediaPipe fires callbacks on a background thread.
        // We store the latest result here and process it in Update()
        // which runs on the main thread where Unity API calls are safe.
        private PoseLandmarkerResult? _pendingResult = null;
        private bool _hasNewResult = false;
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

        // Called on background thread by MediaPipe — only store the result
        private void HandleResult(PoseLandmarkerResult result)
        {
            lock (_lock)
            {
                _pendingResult = result;
                _hasNewResult = true;
            }
        }

        // Called on main thread every frame — safe to use Unity API here
        private void Update()
        {
            PoseLandmarkerResult? result = null;

            lock (_lock)
            {
                if (_hasNewResult)
                {
                    result = _pendingResult;
                    _hasNewResult = false;
                }
            }

            if (result.HasValue)
                ProcessResult(result.Value);
        }

        private void ProcessResult(PoseLandmarkerResult result)
        {
            // No person detected
            if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
            {
                DestroyAllOrbs();
                return;
            }

            var landmarks = result.poseLandmarks[0].landmarks;

            if (landmarks == null || landmarks.Count < 29)
            {
                DestroyAllOrbs();
                return;
            }

            for (int i = 0; i < Segments.Length; i++)
            {
                var lmA = landmarks[Segments[i].a];
                var lmB = landmarks[Segments[i].b];

                // Joint not visible enough — destroy orb
                if (lmA.visibility < VisibilityThreshold || lmB.visibility < VisibilityThreshold)
                {
                    if (_orbs[i] != null)
                    {
                        Destroy(_orbs[i]);
                        _orbs[i] = null;
                    }
                    continue;
                }

                // Midpoint between the two landmarks (normalized 0-1)
                float midX = (lmA.x + lmB.x) / 2f;
                float midY = (lmA.y + lmB.y) / 2f;

                // Safe to call ViewportToWorldPoint here — we are on main thread
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
            // MediaPipe: x=0 LEFT, y=0 TOP
            // Unity viewport: x=0 LEFT, y=0 BOTTOM — flip Y
            float viewX = normX;
            float viewY = 1f - normY;

            Vector3 viewportPoint = new Vector3(viewX, viewY, OrbDepth);
            return MainCamera.ViewportToWorldPoint(viewportPoint);
        }

        private void DestroyAllOrbs()
        {
            if (_orbs == null) return;
            for (int i = 0; i < _orbs.Length; i++)
            {
                if (_orbs[i] != null)
                {
                    Destroy(_orbs[i]);
                    _orbs[i] = null;
                }
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