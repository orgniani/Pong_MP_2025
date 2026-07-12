using UnityEngine;

namespace Config
{
    public static class MatchRules
    {
        private const int FallbackDedicatedServerSlots = 1;

        public static int GetDedicatedServerSlots()
        {
            if (!MatchRulesRegistry.TryGetConfig(out var config) || config == null)
            {
                Debug.LogError("[MatchRules] MatchRulesRegistry is not initialized. Using fallback dedicated server slots. Add matchRulesConfig registration in startup flow.");
                return FallbackDedicatedServerSlots;
            }

            return config.DedicatedServerSlots;
        }

        public static int ToGamePlayerCount(int sessionPlayerCount)
        {
            var gamePlayers = sessionPlayerCount - GetDedicatedServerSlots();
            return gamePlayers < 0 ? 0 : gamePlayers;
        }
    }
}
