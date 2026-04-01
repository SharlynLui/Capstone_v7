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
        public Button MenuIconButton;   // Drag the MenuIcon Button here
        public GameObject MenuIcon;     // Drag the MenuIcon GameObject here

        [Header("Performance References")]
        public OrbManager OrbManagerRef;  // Changed from MonoBehaviour to OrbManager

        [Header("Settings")]
        public float FadeDuration = 1f;

        public enum GameMode { FullPlay, Tutorial, Cheat }
        private GameMode activeMode;

        [Header("Cheat Mode References")]
        public Button CheatButton;      // The button inside StartPanel
        public GameObject CheatIcon;    // The special icon that opens sliders
        public GameObject CheatPanel;

        private void Start()
        {
            // Hide everything at launch
            if (ScoreUI != null) ScoreUI.SetActive(false);
            if (MenuIcon != null) MenuIcon.SetActive(false);
            if (OrbManagerRef != null) OrbManagerRef.enabled = false;
            if (CheatIcon != null) CheatIcon.SetActive(false);
            if (CheatPanel != null) CheatPanel.SetActive(false);

            if (StartCanvasGroup != null)
            {
                StartCanvasGroup.alpha = 1f;
                StartCanvasGroup.interactable = true;
                StartCanvasGroup.blocksRaycasts = true;
            }

            // Wire buttons
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

        // ── Start Button ─────────────────────────────────────────────

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

        public void ToggleCheatPanel()
        {
            if (CheatPanel != null)
            {
                // 1. Determine the new state: If panel is active, we are CLOSING it.
                bool isOpening = !CheatPanel.activeSelf;

                // 2. Set the Panel visibility
                CheatPanel.SetActive(isOpening);

                // 3. Reverse the visibility for Gameplay UI
                // If the panel is OPENING (true), Gameplay UI should be HIDDEN (false)
                bool showGameplayUI = !isOpening;

                if (ScoreUI != null) ScoreUI.SetActive(showGameplayUI);
                if (MenuIcon != null) MenuIcon.SetActive(showGameplayUI);
                if (CheatIcon != null) CheatIcon.SetActive(showGameplayUI);

                //// 4. Handle the OrbManager
                //if (OrbManagerRef != null)
                //{
                //    if (isOpening)
                //    {
                //        // Stop the 50Hz updates and clear current orbs so they don't 
                //        // float over your sliders.
                //        OrbManagerRef.enabled = false;
                //        OrbManagerRef.HideAllOrbs();
                //    }
                //    else
                //    {
                //        // Resume the tracking logic
                //        OrbManagerRef.enabled = true;
                //    }
                //}

                Debug.Log(isOpening ? "[Cheat] Panel Opened - Gameplay UI Hidden" : "[Cheat] Panel Closed - Gameplay UI Restored");
            }
        }

        private IEnumerator TransitionToTraining()
        {
            // Show universal gameplay UI
            if (ScoreUI != null) ScoreUI.SetActive(true);
            if (MenuIcon != null) MenuIcon.SetActive(true);
            if (OrbManagerRef != null) OrbManagerRef.enabled = true;

            // Mode-specific UI
            if (activeMode == GameMode.Cheat && CheatIcon != null)
            {
                CheatIcon.SetActive(true);
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

            // Fully hide start panel
            if (StartCanvasGroup != null)
            {
                StartCanvasGroup.alpha = 0f;
                StartCanvasGroup.interactable = false;
                StartCanvasGroup.blocksRaycasts = false;
            }

            gameObject.SetActive(false);
            Debug.Log("[StartScreenController] Game started.");
        }

        // ── Menu Icon Button ─────────────────────────────────────────

        public void ReturnToMenu()
        {
            // Hide all gameplay elements
            if (ScoreUI != null) ScoreUI.SetActive(false);
            if (MenuIcon != null) MenuIcon.SetActive(false);
            if (CheatIcon != null) CheatIcon.SetActive(false);
            if (CheatPanel != null) CheatPanel.SetActive(false);
            if (OrbManagerRef != null)
            {
                OrbManagerRef.enabled = false;
                OrbManagerRef.HideAllOrbs(); // hides existing orbs immediately
            }

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

            Debug.Log("[StartScreenController] Returned to menu.");
        }
    }
}