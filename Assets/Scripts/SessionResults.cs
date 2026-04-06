using System.Collections.Generic;

namespace TaiChi
{
    public class SessionResult
    {
        public float Duration;
        public float AverageOverall;
        public float Variance;
        public Dictionary<string, float> JointAverages;
        public float PeakScore;
        public float PeakScoreTime;
    }
}