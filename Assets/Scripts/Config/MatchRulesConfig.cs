using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "MatchRulesConfig", menuName = "Config/Match Rules Config")]
    public class MatchRulesConfig : ScriptableObject
    {
        [SerializeField] private int minPlayersToStart = 2;
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private float countdownSeconds = 3f;

        public int MinPlayersToStart => ClampMinPlayers(minPlayersToStart);
        public int MaxPlayers => ClampMaxPlayers(maxPlayers, MinPlayersToStart);
        public float CountdownSeconds => ClampCountdownSeconds(countdownSeconds);

        public int ResolveMinPlayersToStart() => MinPlayersToStart;

        public int ResolveMaxPlayers() => MaxPlayers;

        private void OnValidate()
        {
            minPlayersToStart = ClampMinPlayers(minPlayersToStart);
            maxPlayers = ClampMaxPlayers(maxPlayers, minPlayersToStart);
            countdownSeconds = ClampCountdownSeconds(countdownSeconds);
        }

        private static int ClampMinPlayers(int value)
        {
            return Mathf.Max(1, value);
        }

        private static int ClampMaxPlayers(int value, int minValue)
        {
            return Mathf.Max(minValue, value);
        }

        private static float ClampCountdownSeconds(float value)
        {
            return Mathf.Max(0f, value);
        }
    }
}
