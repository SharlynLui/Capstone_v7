/* Each of the effects are rendered randomly, can be adjusted such that only test out 1 result.
 * X axis adjustments is actually up and down; Y axis adjustments are left and right
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TaiChi
{
    [System.Serializable]
    public class EffectConfig
    {
        public string EffectName;
        public GameObject Prefab;
        public Vector3 Offset = new Vector3(0, -0.5f, 0);
        public float CustomScale = 1.0f;
    }

    public class EnvironmentalEffects : MonoBehaviour
    {
        public static EnvironmentalEffects Instance;

        [Header("References")]
        public OrbManager OrbManagerRef;

        [Header("Effect Library")]
        public List<EffectConfig> EffectsLibrary = new List<EffectConfig>();

        [Header("Testing Mode")]
        public bool UseStaticEffect = false;
        public int StaticEffectIndex = 0;

        [Header("Timing")]
        public float EffectDuration = 2.0f;
        public float CooldownDuration = 5.0f;

        [Header("Simple Counter Settings")]
        [Tooltip("Number of good data packets needed to trigger the effect")]
        public int TriggerThreshold = 10;

        private int _packetCounter = 0;
        private bool _canTrigger = true;

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public void UpdateEnvironmentalState(bool isAboveThreshold)
        {
            // 1. UPDATE THE COUNTER
            if (isAboveThreshold)
            {
                // Increase the bucket count
                if (_packetCounter < TriggerThreshold) _packetCounter++;
            }
            else
            {
                // Decrease the bucket count (Leaky Bucket)
                // This ignores 1 or 2 bad packets but kills the trigger if data stays bad
                if (_packetCounter > 0) _packetCounter--;
            }

            // 2. TRIGGER LOGIC
            // If the bucket is full AND we aren't currently waiting for a cooldown
            if (_packetCounter >= TriggerThreshold && _canTrigger && EffectsLibrary.Count > 0)
            {
                // Reset counter so it doesn't instantly refire
                _packetCounter = 0;

                Vector3 spawnPos = GetCorrectedSpawnPosition();
                if (spawnPos != Vector3.zero)
                {
                    EffectConfig selectedConfig = GetSelectedConfig();
                    StartCoroutine(SpawnAndDestroyCycle(spawnPos, selectedConfig));
                }
            }
        }

        private EffectConfig GetSelectedConfig()
        {
            if (UseStaticEffect)
            {
                int index = Mathf.Clamp(StaticEffectIndex, 0, EffectsLibrary.Count - 1);
                return EffectsLibrary[index];
            }
            return EffectsLibrary[Random.Range(0, EffectsLibrary.Count)];
        }

        private Vector3 GetCorrectedSpawnPosition()
        {
            if (OrbManagerRef == null) return Vector3.zero;
            return OrbManagerRef.GetFootMidpoint();
        }

        private IEnumerator SpawnAndDestroyCycle(Vector3 basePosition, EffectConfig config)
        {
            _canTrigger = false;

            Vector3 finalPos = basePosition + config.Offset;
            Quaternion baseRotation = Quaternion.LookRotation(
                OrbManagerRef.MainCamera.transform.forward,
                OrbManagerRef.MainCamera.transform.up
            );

            GameObject spawnedEffect = Instantiate(config.Prefab, finalPos, baseRotation);
            spawnedEffect.transform.localScale *= config.CustomScale;
            spawnedEffect.transform.Rotate(0, 0, 90f);

            yield return new WaitForSeconds(EffectDuration);
            if (spawnedEffect != null) Destroy(spawnedEffect);

            yield return new WaitForSeconds(CooldownDuration - EffectDuration);
            _canTrigger = true;
        }
    }
}