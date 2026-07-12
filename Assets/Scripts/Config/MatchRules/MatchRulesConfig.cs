using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "MatchRulesConfig", menuName = "Config/Match Rules Config")]
    public class MatchRulesConfig : ScriptableObject
    {
        [SerializeField] private float countdownSeconds = 3f;
        [SerializeField] private int dedicatedServerSlots = 1;

        public float CountdownSeconds => ClampCountdownSeconds(countdownSeconds);
        public int DedicatedServerSlots => ClampDedicatedServerSlots(dedicatedServerSlots);

        private void OnValidate()
        {
            countdownSeconds = ClampCountdownSeconds(countdownSeconds);
            dedicatedServerSlots = ClampDedicatedServerSlots(dedicatedServerSlots);
        }

        private static float ClampCountdownSeconds(float value)
        {
            return Mathf.Max(0f, value);
        }

        private static int ClampDedicatedServerSlots(int value)
        {
            return Mathf.Max(0, value);
        }
    }
}
