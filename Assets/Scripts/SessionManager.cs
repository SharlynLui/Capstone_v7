using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace TaiChi
{
    public class SessionManager : MonoBehaviour
    {
        public static SessionManager Instance { get; private set; }

        [Header("Countdown UI")]
        public TextMeshProUGUI CountdownText;

        [Header("Session UI")]
        public Button StartSessionButton;
        public Button StopSessionButton;

        [Header("Summary Panel Reference")]
        public SessionSummaryPanel SummaryPanel;

        // ── Session State ─────────────────────────────────────────
        public bool IsSessionActive { get; private set; } = false;

        // ── Accumulated Data ──────────────────────────────────────
        private float _overallSum = 0f;
        private float _overallSqSum = 0f;
        private Dictionary<string, float> _jointSums = new Dictionary<string, float>();
        private int _sampleCount = 0;
        private float _peakScore = 0f;
        private float _peakScoreTime = 0f;
        private float _sessionStartTime = 0f;

        // ── Lifecycle ─────────────────────────────────────────────
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (StartSessionButton != null)
            {
                StartSessionButton.gameObject.SetActive(false);
                StartSessionButton.onClick.AddListener(OnStartSessionPressed);
            }

            if (StopSessionButton != null)
            {
                StopSessionButton.gameObject.SetActive(false);
                StopSessionButton.onClick.AddListener(StopSession);
            }

            if (CountdownText != null)
                CountdownText.gameObject.SetActive(false); // ← hide text object
        }

        // ── Show Start Button (called by StartScreenController) ───
        public void ShowStartButton()
        {
            if (StartSessionButton != null)
                StartSessionButton.gameObject.SetActive(true);
        }

        // ── Reset UI (called by ReturnToMenu) ─────────────────────
        public void ResetSessionUI()
        {
            if (StartSessionButton != null)
                StartSessionButton.gameObject.SetActive(false);
            if (StopSessionButton != null)
                StopSessionButton.gameObject.SetActive(false);
            if (CountdownText != null)
                CountdownText.gameObject.SetActive(false); // ← hide text object
            IsSessionActive = false;
        }

        // ── Start Flow ────────────────────────────────────────────
        private void OnStartSessionPressed()
        {
            StartCoroutine(CountdownThenStart());
        }

        private IEnumerator CountdownThenStart()
        {
            if (StartSessionButton != null)
                StartSessionButton.gameObject.SetActive(false);

            // --- NEW: HIDE MENU ICON WHEN STARTING ---
            // This ensures that as soon as the user commits to starting, the "Back" button disappears.
            if (StartScreenController.Instance != null && StartScreenController.Instance.MenuIcon != null)
            {
                StartScreenController.Instance.MenuIcon.SetActive(false);
            }

            // Show countdown text
            if (CountdownText != null)
                CountdownText.gameObject.SetActive(true);

            for (int i = 3; i > 0; i--)
            {
                if (CountdownText != null)
                    CountdownText.text = i.ToString();
                yield return new WaitForSeconds(1f);
            }

            // Hide countdown text
            if (CountdownText != null)
                CountdownText.gameObject.SetActive(false);

            // Show stop button only after countdown finishes ← restored original behaviour
            if (StopSessionButton != null)
                StopSessionButton.gameObject.SetActive(true);

            BeginSession();
        }

        private void BeginSession()
        {
            _overallSum = 0f;
            _overallSqSum = 0f;
            _jointSums.Clear();
            _sampleCount = 0;
            _peakScore = 0f;
            _peakScoreTime = 0f;
            _sessionStartTime = Time.time;

            IsSessionActive = true;

            ScoreEventBus.OnScoresUpdated += OnScoresUpdated;
            Debug.Log("[SessionManager] Session started.");
        }

        // ── Score Accumulation ────────────────────────────────────
        private void OnScoresUpdated(Dictionary<string, float> scores)
        {
            if (!IsSessionActive) return;

            float overall;
            if (!scores.TryGetValue("overall", out overall))
            {
                float total = 0f;
                foreach (var kvp in scores) total += kvp.Value;
                overall = scores.Count > 0 ? total / scores.Count : 0f;
            }

            _overallSum += overall;
            _overallSqSum += overall * overall;
            _sampleCount++;

            foreach (var kvp in scores)
            {
                if (kvp.Key == "overall") continue;
                if (!_jointSums.ContainsKey(kvp.Key))
                    _jointSums[kvp.Key] = 0f;
                _jointSums[kvp.Key] += kvp.Value;
            }

            if (overall > _peakScore)
            {
                _peakScore = overall;
                _peakScoreTime = Time.time - _sessionStartTime;
            }
        }

        // ── Stop Flow ─────────────────────────────────────────────
        public void StopSession()
        {
            if (!IsSessionActive) return;

            IsSessionActive = false;
            ScoreEventBus.OnScoresUpdated -= OnScoresUpdated;

            if (StopSessionButton != null)
                StopSessionButton.gameObject.SetActive(false);

            // Hide gameplay UI
            if (StartScreenController.Instance != null)
            {
                if (StartScreenController.Instance.ScoreUI != null)
                    StartScreenController.Instance.ScoreUI.SetActive(false);
                if (StartScreenController.Instance.MenuIcon != null)
                    StartScreenController.Instance.MenuIcon.SetActive(false);
            }

            SessionResult result = BuildResult();
            SummaryPanel.Show(result);

            Debug.Log("[SessionManager] Session ended.");
        }

        // ── Build Result ──────────────────────────────────────────
        private SessionResult BuildResult()
        {
            float avgOverall = _sampleCount > 0 ? _overallSum / _sampleCount : 0f;

            float variance = 0f;
            if (_sampleCount > 1)
            {
                float meanSq = _overallSqSum / _sampleCount;
                variance = meanSq - (avgOverall * avgOverall);
            }

            var jointAverages = new Dictionary<string, float>();
            foreach (var kvp in _jointSums)
                jointAverages[kvp.Key] = kvp.Value / _sampleCount;

            return new SessionResult
            {
                Duration = Time.time - _sessionStartTime,
                AverageOverall = avgOverall,
                Variance = variance,
                JointAverages = jointAverages,
                PeakScore = _peakScore,
                PeakScoreTime = _peakScoreTime
            };
        }
    }
}