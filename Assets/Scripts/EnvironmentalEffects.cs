/* Each of the effects are rendered randomly, can be adjusted such that only test out 1 result.
 * X axis adjustmetns is actually up and down; Y axis adjustments are left and right
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TaiChi
{
    // A helper class to store individual settings for each effect
    [System.Serializable]
    public class EffectConfig
    {
        public string EffectName;
        public GameObject Prefab;
        public Vector3 Offset = new Vector3(0, -0.5f, 0); // Defaulting Y to -0.5 to lower it
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
        public bool UseStaticEffect = false; // Toggle this in Inspector to test one specific effect
        public int StaticEffectIndex = 0;    // The index of the effect you want to test

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
            if (isAboveThreshold && _canTrigger && EffectsLibrary.Count > 0)
            {
                Vector3 spawnPos = GetCorrectedSpawnPosition();
                if (spawnPos != Vector3.zero)
                {
                    // Pick the effect configuration based on Test Mode
                    EffectConfig selectedConfig;
                    if (UseStaticEffect)
                    {
                        int index = Mathf.Clamp(StaticEffectIndex, 0, EffectsLibrary.Count - 1);
                        selectedConfig = EffectsLibrary[index];
                    }
                    else
                    {
                        selectedConfig = EffectsLibrary[Random.Range(0, EffectsLibrary.Count)];
                    }

                    StartCoroutine(SpawnAndDestroyCycle(spawnPos, selectedConfig));
                }
            }
        }

        private Vector3 GetCorrectedSpawnPosition()
        {
            if (OrbManagerRef == null) return Vector3.zero;
            return OrbManagerRef.GetFootMidpoint();
        }

        private IEnumerator SpawnAndDestroyCycle(Vector3 basePosition, EffectConfig config)
        {
            _canTrigger = false;

            // 1. Calculate the offset position relative to the base foot midpoint
            Vector3 finalPos = basePosition + config.Offset;

            // 2. Base rotation logic (proven working)
            Quaternion baseRotation = Quaternion.LookRotation(OrbManagerRef.MainCamera.transform.forward, OrbManagerRef.MainCamera.transform.up);

            // 3. Spawn and Apply Scale
            GameObject spawnedEffect = Instantiate(config.Prefab, finalPos, baseRotation);
            spawnedEffect.transform.localScale *= config.CustomScale;

            // 4. Your proven 90-degree correction for Landscape/Phone orientation
            spawnedEffect.transform.Rotate(0, 0, 90f);

            yield return new WaitForSeconds(EffectDuration);
            if (spawnedEffect != null) Destroy(spawnedEffect);

            yield return new WaitForSeconds(CooldownDuration - EffectDuration);
            _canTrigger = true;
        }
    }
}