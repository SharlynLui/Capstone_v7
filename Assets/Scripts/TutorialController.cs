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

        [Header("Progress UI")]
        public Slider SuccessProgressBar;
        public TextMeshProUGUI ProgressStatusText; 

        [Header("Settings")]
        public float ScoreThreshold = 0.6f;
        public float ArrowMoveDuration = 0.4f;
        public float HoldDuration = 5.0f; // Accumulative 5 secs

        public enum TutorialPhase { UITour, Practice, Completed }
        public TutorialPhase CurrentPhase { get; private set; }

        private int _currentStepIndex = 0;
        private float _accumulatedTime = 0f;
        private bool _isThresholdMet = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (TutorialPanel != null) TutorialPanel.SetActive(false);
            if (ArrowImage != null) ArrowImage.gameObject.SetActive(false);

            if (SuccessProgressBar != null)
            {
                SuccessProgressBar.maxValue = HoldDuration;
                SuccessProgressBar.value = 0;
                SuccessProgressBar.interactable = false;
                SuccessProgressBar.gameObject.SetActive(false);
            }

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
            CurrentPhase = TutorialPhase.UITour;
            ShowFullPlayUI();
            if (TutorialPanel != null) TutorialPanel.SetActive(true);
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
            if (index == 0)
            {
                InstructionText.text = GenerateIntroText();
                if (ArrowImage != null) ArrowImage.gameObject.SetActive(false);
            }
            else
            {
                InstructionText.text = WrapInSummaryStyle("", step.InstructionText, "");
                if (ArrowImage != null)
                {
                    if (step.HideArrow) ArrowImage.gameObject.SetActive(false);
                    else
                    {
                        ArrowImage.gameObject.SetActive(true);
                        StartCoroutine(MoveArrowManual(step.ArrowPosition, step.ArrowRotation));
                    }
                }
            }

            if (step.IsCheatModeStep) ShowCheatModeUI();
            else ShowFullPlayUI();

            if (StepCounterText != null)
                StepCounterText.text = $"{index + 1} / {Steps.Length}";
        }

        private void OnNextPressed()
        {
            if (CurrentPhase == TutorialPhase.UITour)
            {
                if (Steps[_currentStepIndex].IsPracticeIntro) StartPractice();
                else
                {
                    _currentStepIndex++;
                    ShowStep(_currentStepIndex);
                }
            }
            else if (CurrentPhase == TutorialPhase.Completed)
            {
                StartScreenController.Instance.ReturnToMenu();
            }
        }

        private void StartPractice()
        {
            CurrentPhase = TutorialPhase.Practice;
            _isThresholdMet = false;
            _accumulatedTime = 0f;

            if (TutorialPanel != null) TutorialPanel.SetActive(false);
            if (ArrowImage != null) ArrowImage.gameObject.SetActive(false);

            if (SuccessProgressBar != null)
            {
                SuccessProgressBar.value = 0;
                SuccessProgressBar.gameObject.SetActive(true); // Always show during practice now
            }

            ShowFullPlayUI();
            var sc = StartScreenController.Instance;
            if (sc != null && sc.OrbManagerRef != null) sc.OrbManagerRef.enabled = true;

            ScoreEventBus.OnScoresUpdated += OnScoresUpdated;
        }

        private void OnScoresUpdated(Dictionary<string, float> scores)
        {
            if (CurrentPhase != TutorialPhase.Practice) return;

            if (scores.TryGetValue("overall", out float overall))
            {
                // Toggle flag based on current score
                _isThresholdMet = (overall >= ScoreThreshold);
            }
        }

        private void Update()
        {
            if (CurrentPhase == TutorialPhase.Practice)
            {
                if (_isThresholdMet)
                {
                    _accumulatedTime += Time.deltaTime;
                    if (SuccessProgressBar != null) SuccessProgressBar.value = _accumulatedTime;

                    // UI Feedback
                    if (ProgressStatusText != null)
                        ProgressStatusText.text = "<b>You are doing well...</b>";

                    if (_accumulatedTime >= HoldDuration)
                    {
                        CompleteTutorial();
                    }
                }
                else
                {
                    // UI Feedback when they drop below the threshold
                    if (ProgressStatusText != null)
                        ProgressStatusText.text = "Synchronize Pose";
                }
            }
        }

        private void CompleteTutorial()
        {
            if (CurrentPhase == TutorialPhase.Completed) return;
            CurrentPhase = TutorialPhase.Completed;

            ScoreEventBus.OnScoresUpdated -= OnScoresUpdated;

            if (SuccessProgressBar != null) SuccessProgressBar.gameObject.SetActive(false);
            if (TutorialPanel != null) TutorialPanel.SetActive(true);

            InstructionText.text = WrapInSummaryStyle(
                "TUTORIAL COMPLETE!",
                "<color=#FFD700><b>GOOD JOB!</b></color>\n\n" +
                "You maintained <color=#00FF00><b>5 seconds</b></color> of good accuracy.\n\n" +
                "You are now ready to begin your <color=#FFD700>Tai Chi journey</color>.",
                ""
            );
        }

        private string GenerateIntroText()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<color=#FFD700><size=115%><b>WELCOME TO TAI CHI MASTER</b></size></color>");
            sb.AppendLine("<size=90%> Master Tai Chi with real-time AR guidance.</size>");
            sb.AppendLine();
            sb.AppendLine("<color=#FFD700><b>── Features ──</b></color>");
            sb.AppendLine("<line-height=110%><size=90%><color=#FFD700><b>Full Play:</b></color> Training sessions with real-time feedback and statistics breakdown for improvement.");
            sb.AppendLine("<size=90%><color=#FFD700><b>Cheat Mode:</b></color> Adjust threshold to suit student's need and access advanced real-time data.</size></line-height>");
            sb.AppendLine();
            sb.AppendLine("<color=#FFD700><b>── Goal ──</b></color>");
            sb.AppendLine("<size=90%>Hold accuracy above <color=#00FF00><b>60%</b></color> for 5s total.</size>");
            return sb.ToString();
        }

        private string WrapInSummaryStyle(string header, string body, string goal = "")
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<color=#AAAAAA><size=100%>INSTRUCTION</size></color>");
            sb.AppendLine();
            sb.AppendLine($"<size=110%>{body}</size>");
            return sb.ToString();
        }

        private void OnCancelPressed()
        {
            CleanUpState();
            var sc = StartScreenController.Instance;
            if (sc != null)
            {
                sc.ScoreUI?.SetActive(false);
                sc.MenuIcon?.SetActive(true);
            }
            ScoreEventBus.OnScoresUpdated -= OnScoresUpdated;
        }

        private void CleanUpState()
        {
            _isThresholdMet = false;
            _accumulatedTime = 0f;
            if (SuccessProgressBar != null)
            {
                SuccessProgressBar.value = 0;
                SuccessProgressBar.gameObject.SetActive(false);
            }
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
            CurrentPhase = TutorialPhase.UITour;
            ScoreEventBus.OnScoresUpdated -= OnScoresUpdated;
            CleanUpState();
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