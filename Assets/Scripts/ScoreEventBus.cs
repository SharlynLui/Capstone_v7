using System.Collections.Generic;

namespace TaiChi
{
    /// <summary>
    /// Central event bus for score data.
    /// Any data source (FakeDataSimulator or MQTTDataRouter) publishes here.
    /// Any listener (OrbManager, ScoreReader) subscribes here.
    /// No dependency between data sources and listeners.
    /// </summary>
    public static class ScoreEventBus
    {
        // Fires whenever new part scores arrive from any source
        public static event System.Action<Dictionary<string, float>> OnScoresUpdated;

        // Called by FakeDataSimulator or MQTTDataRouter to publish scores
        public static void Publish(Dictionary<string, float> scores)
        {
            OnScoresUpdated?.Invoke(scores);
        }
    }
}