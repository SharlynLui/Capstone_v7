using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace TaiChi
{
    public class TutorialController : MonoBehaviour
    {
        public static TutorialController Instance { get; private set; }

        [Header("Steps Configuration")]
        public TutorialStep[] Steps;

        [Header("Panel UI References")]
        public GameObject TutorialPanel;
        public TextMeshProUGUI InstructionText;
        public TextMeshProUGUI StepCounterText;
        public Button NextButton;
        public Button CancelButton;
        public RectTransform ArrowImage;

        [Header("Settings")]
        public float ScoreThreshold = 0.6f;
        public float ArrowMoveDuration = 0.4f;

        public enum TutorialPhase { UITour, Practice, Completed }
        public TutorialPhase CurrentPhase { get; private set; }

        private int _currentStepIndex = 0;
        private float _highestScore = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (TutorialPanel != null) TutorialPanel.SetActive(false);
            if (ArrowImage != null) ArrowImage.gameObject.SetActive(false);

            if (NextButton != null)
            {
                NextButton.onClick.AddListener(OnNextPressed);
                UpdateNextButtonLabel("");
            }

            if (CancelButton != null)
                CancelButton.onClick.AddListener(OnCancelPressed);
        }

        public void StartTutorial()
        {
            _currentStepIndex = 0;
            _highestScore = 0f;
            CurrentPhase = TutorialPhase.UITour;

            ShowFullPlayUI();

            if (TutorialPanel != null) TutorialPanel.SetActive(true);
            UpdateNextButtonLabel("");

            ShowStep(_currentStepIndex);
        }

        private void ShowStep(int index)
        {
            if (Steps == null || index >= Steps.Length)
            {
                StartPractice();
                return;
            }

            TutorialStep step = Steps[index];

            // ── Style Management ──
            if (index == 0)
            {
                InstructionText.text = GenerateIntroText();
                if (ArrowImage != null) ArrowImage.gameObject.SetActive(false);
            }
            else
            {
                string header = $"Step {index}";
                // Updated Goal Text for the Practice Intro Step
                string goal = step.IsPracticeIntro ? "Tap the button to start the challenge!" : "Tap the button to continue";
                InstructionText.text = WrapInSummaryStyle(header, step.InstructionText, goal);

                if (ArrowImage != null)
                {
                    ArrowImage.gameObject.SetActive(true);
                    StartCoroutine(MoveArrowManual(step.ArrowPosition, step.ArrowRotation));
                }
            }

            // ── UI Mode Toggles ──
            if (step.IsCheatModeStep) ShowCheatModeUI();
            else ShowFullPlayUI();

            if (StepCounterText != null)
                StepCounterText.text = $"{index + 1} / {Steps.Length}";

            UpdateNextButtonLabel("");
        }

        private void OnNextPressed()
        {
            if (CurrentPhase == TutorialPhase.UITour)
            {
                // Check if the current step is the Practice Intro
                if (Steps[_currentStepIndex].IsPracticeIntro)
                {
                    StartPractice();
                }
                else
                {
                    _currentStepIndex++;
                    ShowStep(_currentStepIndex);
                }
            }
            else if (CurrentPhase == TutorialPhase.Completed)
            {
                // Final exit back to menu
                StartScreenController.Instance.ReturnToMenu();
            }
        }

        private void StartPractice()
        {
            CurrentPhase = TutorialPhase.Practice;
            _highestScore = 0f;

            // HIDE UI so user can see their body/orbs clearly
            if (TutorialPanel != null) TutorialPanel.SetActive(false);
            if (ArrowImage != null) ArrowImage.gameObject.SetActive(false);

            ShowFullPlayUI();

            // Turn on the Orbs for live feedback
            var sc = StartScreenController.Instance;
            if (sc != null && sc.OrbManagerRef != null) sc.OrbManagerRef.enabled = true;

            // Subscribe to live score data
            ScoreEventBus.OnScoresUpdated += OnScoresUpdated;
        }

        private void OnScoresUpdated(Dictionary<string, float> scores)
        {
            if (CurrentPhase != TutorialPhase.Practice) return;

            if (scores.TryGetValue("overall", out float overall))
            {
                // Track highest score reached during this attempt
                if (overall > _highestScore) _highestScore = overall;

                // If they hit the 60% goal, show the final panel
                if (_highestScore >= ScoreThreshold)
                {
                    CompleteTutorial();
                }
            }
        }

        private void CompleteTutorial()
        {
            if (CurrentPhase == TutorialPhase.Completed) return;
            CurrentPhase = TutorialPhase.Completed;

            // Stop tracking scores
            ScoreEventBus.OnScoresUpdated -= OnScoresUpdated;

            // Show the Tutorial Panel again with the "DONE" message
            if (TutorialPanel != null) TutorialPanel.SetActive(true);

            InstructionText.text = WrapInSummaryStyle(
                "TUTORIAL COMPLETE!",
                "Excellent form! You reached the 60% accuracy threshold. You are now ready to begin your Tai Chi journey.",
                "Tap the button to return to menu"
            );

            UpdateNextButtonLabel("");
        }

        // ── Visual Styling Helpers (Summary Style) ──

        private string GenerateIntroText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<color=#FFD700><size=130%><b>WELCOME TO TAI CHI AR</b></size></color>");
            sb.AppendLine();
            sb.AppendLine("<color=#AAAAAA>THE MISSION</color>");
            sb.AppendLine("<size=90%>Master Tai Chi through real-time motion tracking and AR guidance.</size>");
            sb.AppendLine();
            sb.AppendLine("<color=#FFD700><b>── How it Works ──</b></color>");
            sb.AppendLine("<color=#AAAAAA>Full Play   </color> Standard training with accuracy.");
            sb.AppendLine("<color=#AAAAAA>Cheat Mode  </color> Access debug tools and sensor data.");
            sb.AppendLine();
            sb.AppendLine("<color=#FFD700><b>── Your Goal ──</b></color>");
            sb.AppendLine("Learn the UI, then match the pose to reach <color=#00FF00><b>60% Accuracy</b></color>.");
            return sb.ToString();
        }

        private string WrapInSummaryStyle(string header, string body, string goal = "")
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<color=#FFD700><size=130%><b>{header.ToUpper()}</b></size></color>");
            sb.AppendLine();
            sb.AppendLine("<color=#AAAAAA>INSTRUCTION</color>");
            sb.AppendLine($"<size=90%>{body}</size>");
            sb.AppendLine();
            if (!string.IsNullOrEmpty(goal))
            {
                sb.AppendLine("<color=#FFD700><b>── Current Goal ──</b></color>");
                sb.AppendLine($"<color=#00FF00>{goal}</color>");
            }
            return sb.ToString();
        }

        // ── Component Management ──

        private void OnCancelPressed()
        {
            if (TutorialPanel != null) TutorialPanel.SetActive(false);
            if (ArrowImage != null) ArrowImage.gameObject.SetActive(false);

            var sc = StartScreenController.Instance;
            if (sc != null)
            {
                sc.ScoreUI?.SetActive(false);
                sc.CheatIcon?.SetActive(false);
                sc.CheatPanel?.SetActive(false);
                sc.DebugIcon?.SetActive(false);
                sc.DebugPanel?.SetActive(false);
                sc.MenuIcon?.SetActive(true);
            }
            ScoreEventBus.OnScoresUpdated -= OnScoresUpdated;
        }

        private IEnumerator MoveArrowManual(Vector3 targetPos, Vector3 targetRot)
        {
            Vector3 startPos = ArrowImage.anchoredPosition3D;
            Quaternion startRot = ArrowImage.localRotation;
            Quaternion endRot = Quaternion.Euler(targetRot);

            float elapsed = 0f;
            while (elapsed < ArrowMoveDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / ArrowMoveDuration;
                ArrowImage.anchoredPosition3D = Vector3.Lerp(startPos, targetPos, t);
                ArrowImage.localRotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
            ArrowImage.anchoredPosition3D = targetPos;
            ArrowImage.localRotation = endRot;
        }

        public void Reset()
        {
            _currentStepIndex = 0;
            _highestScore = 0f;
            CurrentPhase = TutorialPhase.UITour;
            ScoreEventBus.OnScoresUpdated -= OnScoresUpdated;
            if (TutorialPanel != null) TutorialPanel.SetActive(false);
            if (ArrowImage != null) ArrowImage.gameObject.SetActive(false);
        }

        private void ShowFullPlayUI()
        {
            var sc = StartScreenController.Instance;
            if (sc == null) return;
            sc.ScoreUI?.SetActive(true);
            sc.MenuIcon?.SetActive(true);
            sc.CheatIcon?.SetActive(false);
            sc.CheatPanel?.SetActive(false);
            sc.DebugIcon?.SetActive(false);
            sc.DebugPanel?.SetActive(false);
        }

        private void ShowCheatModeUI()
        {
            var sc = StartScreenController.Instance;
            if (sc == null) return;
            sc.CheatIcon?.SetActive(true);
            sc.DebugIcon?.SetActive(true);
            sc.CheatPanel?.SetActive(false);
            sc.DebugPanel?.SetActive(false);
        }

        private void UpdateNextButtonLabel(string text)
        {
            if (NextButton != null)
            {
                var label = NextButton.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = "";
            }
        }
    }
}