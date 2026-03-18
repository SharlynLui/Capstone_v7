using UnityEngine;
using System.Collections.Generic;
using System.Threading;
using System.Text;

namespace TaiChi
{
    public class FakeDataSimulator : MonoBehaviour
    {
        [Header("Settings")]
        public float UpdateInterval = 0.02f; // 50Hz

        [Tooltip("If true, randomly generates scores. If false uses ManualScore for all joints.")]
        public bool RandomScores = true;

        [Range(0f, 1f)]
        public float ManualScore = 0.85f;

        // Matches the keys your OrbManager and MQTT Router expect
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

        private Thread _simThread;
        private bool _isRunning = false;
        private int _seq = 0;


        private void Start()
        {
            _isRunning = true;
            _simThread = new Thread(SimLoop);
            _simThread.IsBackground = true; // Ensures the thread dies if Unity crashes
            _simThread.Start();
            Debug.Log("[FakeDataSimulator] Threaded 50Hz simulation started.");
        }

        private void SimLoop()
        {
            while (_isRunning)
            {
                SendFakeScores();

                // Sleep for the interval (e.g., 20ms for 50Hz)
                int sleepMs = Mathf.Max(1, (int)(UpdateInterval * 1000));
                Thread.Sleep(sleepMs);
            }
        }

        private void SendFakeScores()
        {
            _seq++;
            var partScores = new Dictionary<string, float>();

            foreach (var key in JointKeys)
            {
                // Note: We use System.Random because UnityEngine.Random is NOT thread-safe
                float score;
                if (RandomScores)
                {
                    // Generates a float between 0.5 and 1.0
                    score = (float)(new System.Random().NextDouble() * 0.5 + 0.5);
                }
                else
                {
                    score = ManualScore;
                }
                partScores[key] = score;
            }

            // Dispatching back to Main Thread for EventBus/Unity UI safety
            MainThreadDispatcher.Enqueue(() =>
            {
                ScoreEventBus.Publish(partScores);
            });
        }

        private void OnDisable()
        {
            _isRunning = false;
        }

        private void OnDestroy()
        {
            _isRunning = false;
            if (_simThread != null && _simThread.IsAlive)
            {
                _simThread.Join(500); // Give it 500ms to close gracefully
                _simThread.Abort();
            }
        }
    }
}