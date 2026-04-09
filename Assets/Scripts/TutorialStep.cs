using UnityEngine;

namespace TaiChi
{
    [System.Serializable]
    public class TutorialStep
    {
        [TextArea(2, 5)]
        public string InstructionText;
        public GameObject HighlightTarget;  // Keep this for UI toggling logic

        [Header("Manual Arrow Calibration")]
        public Vector3 ArrowPosition;       // Manual X, Y, Z
        public Vector3 ArrowRotation;       // Manual Euler Angles (Rx, Ry, Rz)

        public bool IsCheatModeStep;
        public bool IsPracticeIntro;
        public bool HideArrow;
    }
}