using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace TaiChi
{
    public class StartScreenController : MonoBehaviour
    {
        [Header("UI References")]
        public CanvasGroup StartCanvasGroup;
        public Button StartButton;
        public GameObject ScoreUI;

        [Header("Menu Icon")]
        public Button MenuIconButton;   // Drag the MenuIcon Button here
        public GameObject MenuIcon;     // Drag the MenuIcon GameObject here

        [Header("Performance References")]
        public OrbManager OrbManagerRef;  // Changed from MonoBehaviour to OrbManager

        [Header("Settings")]
        public float FadeDuration = 1.5f;

        private void Start()
        {
            // Hide everything at launch
            if (ScoreUI != null) ScoreUI.SetActive(false);
            if (MenuIcon != null) MenuIcon.SetActive(false);
            if (OrbManagerRef != null) OrbManagerRef.enabled = false;

            if (StartCanvasGroup != null)
            {
                StartCanvasGroup.alpha = 1f;
                StartCanvasGroup.interactable = true;
                StartCanvasGroup.blocksRaycasts = true;
            }

            // Wire buttons
            if (StartButton != null)
                StartButton.onClick.AddListener(OnStartPressed);
            else
                Debug.LogError("[StartScreenController] StartButton not assigned!");

            if (MenuIconButton != null)
                MenuIconButton.onClick.AddListener(ReturnToMenu);
            else
                Debug.LogWarning("[StartScreenController] MenuIconButton not assigned!");
        }

        // ── Start Button ─────────────────────────────────────────────

        private void OnStartPressed()
        {
            StartButton.interactable = false;
            StartCoroutine(TransitionToTraining());
        }

        private IEnumerator TransitionToTraining()
        {
            // Show ScoreUI and MenuIcon at the SAME time — before fade starts
            if (ScoreUI != null) ScoreUI.SetActive(true);
            if (MenuIcon != null) MenuIcon.SetActive(true);
            if (OrbManagerRef != null) OrbManagerRef.enabled = true;

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

            if (StartButton != null)
                StartButton.interactable = true;

            Debug.Log("[StartScreenController] Returned to menu.");
        }
    }
}