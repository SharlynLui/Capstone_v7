using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace TaiChi
{
    public class StartScreenController : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("The CanvasGroup on the StartPanel for smooth fading")]
        public CanvasGroup StartCanvasGroup;

        [Tooltip("The Start button inside the panel")]
        public Button StartButton;

        [Tooltip("The parent object for your score text and ImgFillRound")]
        public GameObject MainHUD;

        [Header("Performance References")]
        [Tooltip("Drag the object with your OrbManager script here")]
        public MonoBehaviour OrbManagerRef;

        [Header("Settings")]
        public float FadeDuration = 1.5f;

        private void Start()
        {
            // 1. Ensure Start Screen is the only thing the user sees
            if (StartCanvasGroup != null)
            {
                StartCanvasGroup.alpha = 1f;
                StartCanvasGroup.interactable = true;
                StartCanvasGroup.blocksRaycasts = true;
            }

            // 2. Initial State: HUD is hidden, Orbs are NOT spawning
            if (MainHUD != null) MainHUD.SetActive(false);

            if (OrbManagerRef != null)
                OrbManagerRef.enabled = false; // Stops the 50Hz loop immediately
            else
                Debug.LogWarning("OrbManagerRef is missing! Orbs will spawn in the background.");

            // 3. Wire up button
            if (StartButton != null)
                StartButton.onClick.AddListener(OnStartPressed);
        }

        private void OnStartPressed()
        {
            Debug.Log("[Menu] Start Button Tapped. Transitioning...");

            // Disable button so user doesn't double-tap
            StartButton.interactable = false;

            StartCoroutine(TransitionToTraining());
        }

        private IEnumerator TransitionToTraining()
        {
            // 4. Activate Training Logic
            // We enable this at the START of the fade so MediaPipe/Orbs 
            // have a moment to initialize while the screen is still fading.
            if (MainHUD != null) MainHUD.SetActive(true);
            if (OrbManagerRef != null) OrbManagerRef.enabled = true;

            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.deltaTime;
                if (StartCanvasGroup != null)
                    StartCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / FadeDuration);

                yield return null;
            }

            // 5. Cleanup
            if (StartCanvasGroup != null)
            {
                StartCanvasGroup.alpha = 0f;
                StartCanvasGroup.interactable = false;
                StartCanvasGroup.blocksRaycasts = false;
            }

            // Turn off the entire StartPanel object to save GPU draw calls
            gameObject.SetActive(false);

            Debug.Log("[Menu] Start Screen disabled.");
        }
    }
}