using UnityEngine;

namespace Config
{
    public static class MatchRulesRegistry
    {
        private static IMatchRulesProvider _provider;

        public static bool IsInitialized => _provider != null;

        public static void RegisterProvider(IMatchRulesProvider provider, Object source)
        {
            if (provider == null)
            {
                Debug.LogError("[MatchRulesRegistry] Cannot register null provider.");
                return;
            }

            if (_provider != null && !ReferenceEquals(_provider, provider))
            {
                var sourceName = source != null ? source.name : "Unknown";
                Debug.LogWarning($"[MatchRulesRegistry] Provider was already registered. Overriding with provider from '{sourceName}'.");
            }

            _provider = provider;
        }

        public static bool TryGetConfig(out MatchRulesConfig config)
        {
            if (_provider == null)
            {
                config = null;
                return false;
            }

            return _provider.TryGetConfig(out config);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _provider = null;
        }
    }
}
