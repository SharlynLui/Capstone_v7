using UnityEngine;
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

        [Header("Depth from Camera")]
        public float OrbDepth = 1.5f;

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

        // Track instantiated orbs — null means not currently in scene
        private GameObject[] _orbs;

        private void Start()
        {
            if (MainCamera == null)
                MainCamera = Camera.main;

            // Initialize array with nulls — no orbs spawned yet
            _orbs = new GameObject[Segments.Length];

            SubscribeToRunner();
        }

        private void SubscribeToRunner()
        {
            if (Runner == null)
                Runner = Object.FindFirstObjectByType<PoseLandmarkerRunner_edited>();

            if (Runner != null)
            {
                Runner.OnResultOutput += HandleResult;
                Debug.Log("[OrbManager] Subscribed to PoseLandmarkerRunner.");
            }
            else
            {
                Debug.LogError("[OrbManager] PoseLandmarkerRunner not found! Drag it into the Inspector.");
            }
        }

        private void OnDestroy()
        {
            if (Runner != null)
                Runner.OnResultOutput -= HandleResult;

            // Clean up any remaining orbs
            DestroyAllOrbs();
        }

        private void HandleResult(PoseLandmarkerResult result)
        {
            // No person detected — destroy all orbs
            if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
            {
                DestroyAllOrbs();
                return;
            }

            // Get first detected person's landmarks
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

                // Landmark not visible — destroy this orb if it exists
                if (lmA.visibility < 0.5f || lmB.visibility < 0.5f)
                {
                    if (_orbs[i] != null)
                    {
                        Destroy(_orbs[i]);
                        _orbs[i] = null;
                    }
                    continue;
                }

                // Calculate midpoint in normalized coords
                float midX = (lmA.x + lmB.x) / 2f;
                float midY = (lmA.y + lmB.y) / 2f;

                // Convert to world position
                Vector3 worldPos = NormalizedToWorld(midX, midY);

                if (_orbs[i] == null)
                {
                    // Orb does not exist yet — instantiate it
                    _orbs[i] = Instantiate(OrbPrefab, worldPos, Quaternion.identity);
                    _orbs[i].name = $"Orb_{Segments[i].label}";
                    Debug.Log($"[OrbManager] Instantiated {_orbs[i].name}");
                }
                else
                {
                    // Orb already exists — just move it
                    _orbs[i].transform.position = worldPos;
                }
            }
        }

        private Vector3 NormalizedToWorld(float normX, float normY)
        {
            // MediaPipe: x=0 is LEFT, y=0 is TOP
            // Unity viewport: x=0 is LEFT, y=0 is BOTTOM — flip Y
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
    }
}