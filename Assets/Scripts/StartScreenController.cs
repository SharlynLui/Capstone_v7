using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace TaiChi
{
    public class StartScreenController : MonoBehaviour
    {
        [Header("UI References")]
        public CanvasGroup StartCanvasGroup;
        public Button FullPlayButton;
        public GameObject ScoreUI;

        [Header("Menu Icon")]
        public Button MenuIconButton;
        public GameObject MenuIcon;

        [Header("Performance References")]
        public OrbManager OrbManagerRef;
        public ScoreReader ScoreReaderRef;

        [Header("Debug Panel References")]
        public GameObject DebugIcon;
        public GameObject DebugPanel;

        [Header("Summary Panel References")]
        public GameObject SummaryPanel;

        [Header("Tutorial References")]
        public Button TutorialButton;
        public GameObject TutorialPanel;

        [Header("Settings")]
        public float FadeDuration = 1f;

        public enum GameMode { FullPlay, Tutorial, Cheat }
        private GameMode activeMode;

        [Header("Cheat Mode References")]
        public Button CheatButton;
        public GameObject CheatIcon;
        public GameObject CheatPanel;

        private void Start()
        {
            // Hide everything at launch
            if (ScoreUI != null) ScoreUI.SetActive(false);
            if (MenuIcon != null) MenuIcon.SetActive(false);
            if (OrbManagerRef != null) OrbManagerRef.enabled = false;
            if (CheatIcon != null) CheatIcon.SetActive(false);
            if (CheatPanel != null) CheatPanel.SetActive(false);
            if (DebugIcon != null) DebugIcon.SetActive(false);
            if (DebugPanel != null) DebugPanel.SetActive(false);
            if (SummaryPanel != null) SummaryPanel.SetActive(false);
            if (TutorialButton != null)
                TutorialButton.onClick.AddListener(OnTutorialPressed);

            if (StartCanvasGroup != null)
            {
                StartCanvasGroup.alpha = 1f;
                StartCanvasGroup.interactable = true;
                StartCanvasGroup.blocksRaycasts = true;
            }

            if (FullPlayButton != null)
                FullPlayButton.onClick.AddListener(OnStartPressed);
            else
                Debug.LogError("[StartScreenController] FullPlayButton not assigned!");

            if (MenuIconButton != null)
                MenuIconButton.onClick.AddListener(ReturnToMenu);
            else
                Debug.LogWarning("[StartScreenController] MenuIconButton not assigned!");

            if (CheatButton != null)
                CheatButton.onClick.AddListener(OnCheatPressed);
        }

        // ── Start Button ──────────────────────────────────────────
        private void OnStartPressed()
        {
            activeMode = GameMode.FullPlay;
            StartCoroutine(TransitionToTraining());
        }

        private void OnCheatPressed()
        {
            activeMode = GameMode.Cheat;
            StartCoroutine(TransitionToTraining());
        }

        // ── Cheat Panel ───────────────────────────────────────────
        public void ToggleCheatPanel()
        {
            if (CheatPanel != null)
            {
                bool isOpening = !CheatPanel.activeSelf;
                CheatPanel.SetActive(isOpening);

                bool showGameplayUI = !isOpening;
                if (ScoreUI != null) ScoreUI.SetActive(showGameplayUI);
                if (MenuIcon != null) MenuIcon.SetActive(showGameplayUI);
                if (CheatIcon != null) CheatIcon.SetActive(showGameplayUI);
                if (DebugIcon != null) DebugIcon.SetActive(showGameplayUI);

                Debug.Log(isOpening ? "[Cheat] Panel Opened" : "[Cheat] Panel Closed");
            }
        }

        // ── Debug Panel ───────────────────────────────────────────
        public static StartScreenController Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void ToggleDebugPanel()
        {
            if (DebugPanel != null)
            {
                bool isOpening = !DebugPanel.activeSelf;
                DebugPanel.SetActive(isOpening);

                if (ScoreUI != null) ScoreUI.SetActive(!isOpening);
                if (CheatIcon != null) CheatIcon.SetActive(!isOpening);
                if (MenuIcon != null) MenuIcon.SetActive(!isOpening);
                if (DebugIcon != null) DebugIcon.SetActive(!isOpening);
            }
        }

        // ── Tutorial Panel ───────────────────────────────────────────
        private void OnTutorialPressed()
        {
            activeMode = GameMode.Tutorial;
            StartCoroutine(TransitionToTraining());
        }

        // ── Transition ────────────────────────────────────────────
        private IEnumerator TransitionToTraining()
        {
            if (ScoreUI != null) ScoreUI.SetActive(true);
            if (MenuIcon != null) MenuIcon.SetActive(true);
            if (OrbManagerRef != null) OrbManagerRef.enabled = true;

            if (activeMode == GameMode.Cheat && CheatIcon != null)
                CheatIcon.SetActive(true);
            if (activeMode == GameMode.Cheat && DebugIcon != null)
                DebugIcon.SetActive(true);

            if (activeMode == GameMode.FullPlay)
            {
                // 1. Show the Menu Icon so user can go back if they haven't started yet
                if (MenuIcon != null) MenuIcon.SetActive(true);

                // 2. Show the "Start Session" button from your SessionManager
                if (SessionManager.Instance != null)
                    SessionManager.Instance.ShowStartButton();
            }

            // Fade out start panel
            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.deltaTime;
                if (StartCanvasGroup != null)
                    StartCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / FadeDuration);
                yield return null;
            }

            if (StartCanvasGroup != null)
            {
                StartCanvasGroup.alpha = 0f;
                StartCanvasGroup.interactable = false;
                StartCanvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);

            // Tutorial mode start tutorial
            if (activeMode == GameMode.Tutorial && TutorialController.Instance != null)
                TutorialController.Instance.StartTutorial();

            // Show Start Session button only in FullPlay mode
            if (activeMode == GameMode.FullPlay && SessionManager.Instance != null)
                SessionManager.Instance.ShowStartButton();

            Debug.Log("[StartScreenController] Game started.");
        }

        // ── Return to Menu ────────────────────────────────────────
        public void ReturnToMenu()
        {
            if (OrbManagerRef != null)
            {
                OrbManagerRef.ResetToDefaults();
                OrbManagerRef.enabled = false;
                OrbManagerRef.HideAllOrbs();
            }

            if (ScoreReaderRef != null)
                ScoreReaderRef.ResetToDefaults();

            // Hide all gameplay and panel elements
            if (ScoreUI != null) ScoreUI.SetActive(false);
            if (MenuIcon != null) MenuIcon.SetActive(false);
            if (CheatIcon != null) CheatIcon.SetActive(false);
            if (CheatPanel != null) CheatPanel.SetActive(false);
            if (DebugIcon != null) DebugIcon.SetActive(false);
            if (DebugPanel != null) DebugPanel.SetActive(false);
            if (SummaryPanel != null) SummaryPanel.SetActive(false);  // ← new

            if (OrbManagerRef != null)
            {
                OrbManagerRef.enabled = false;
                OrbManagerRef.HideAllOrbs();
            }

            if (SessionManager.Instance != null)
                SessionManager.Instance.ResetSessionUI();

            // Reshow start panel
            gameObject.SetActive(true);

            if (StartCanvasGroup != null)
            {
                StartCanvasGroup.alpha = 1f;
                StartCanvasGroup.interactable = true;
                StartCanvasGroup.blocksRaycasts = true;
            }

            if (FullPlayButton != null)
                FullPlayButton.interactable = true;

            if (TutorialPanel != null) TutorialPanel.SetActive(false);
            if (TutorialController.Instance != null)
                TutorialController.Instance.Reset();

            Debug.Log("[StartScreenController] Returned to menu.");
        }
    }
}