using UnityEngine;
using System.Collections;

namespace TaiChi
{
    public class EnvironmentEffects : MonoBehaviour
    {
        public static EnvironmentEffects Instance;

        [Header("References")]
        public GameObject EffectPrefab;
        public OrbManager OrbManagerRef;

        [Header("Timing")]
        public float EffectDuration = 2.0f;
        public float CooldownDuration = 5.0f;

        private bool _canTrigger = true;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public void UpdateEnvironmentalState(bool isAboveThreshold)
        {
            if (isAboveThreshold && _canTrigger)
            {
                // Get current foot position from the working OrbManager logic
                Vector3 spawnPos = GetCorrectedSpawnPosition();

                if (spawnPos != Vector3.zero)
                {
                    StartCoroutine(SpawnAndDestroyCycle(spawnPos));
                }
            }
        }

        private Vector3 GetCorrectedSpawnPosition()
        {
            if (OrbManagerRef == null) return Vector3.zero;

            // Use the helper you added to OrbManager
            return OrbManagerRef.GetFootMidpoint();
        }

        private IEnumerator SpawnAndDestroyCycle(Vector3 position)
        {
            _canTrigger = false;

            // 1. Get the base rotation from the camera (so it faces the right way)
            Quaternion baseRotation = Quaternion.LookRotation(OrbManagerRef.MainCamera.transform.forward, OrbManagerRef.MainCamera.transform.up);

            // 2. Spawn the object
            GameObject spawnedEffect = Instantiate(EffectPrefab, position, baseRotation);

            // 3. THE CORRECTION: Rotate it 90 degrees to fix the "Upright in X" issue
            // If it's leaning left/right, rotate Z. If it's leaning forward/back, rotate X.
            // Try Z first for Landscape Left issues:
            spawnedEffect.transform.Rotate(0, 0, 90f);

            yield return new WaitForSeconds(EffectDuration);

            if (spawnedEffect != null) Destroy(spawnedEffect);

            yield return new WaitForSeconds(CooldownDuration - EffectDuration);
            _canTrigger = true;
        }
    }
}