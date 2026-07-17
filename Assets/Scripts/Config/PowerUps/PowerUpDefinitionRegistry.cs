using System.Collections.Generic;
using UnityEngine;

namespace Config
{
    public static class PowerUpDefinitionRegistry
    {
        private static IPowerUpDefinitionProvider _provider;
        private static bool _missingProviderLogged;
        private static readonly HashSet<int> MissingDefinitionIds = new HashSet<int>();

        public static bool IsInitialized => _provider != null;

        public static void RegisterProvider(IPowerUpDefinitionProvider provider, Object source)
        {
            if (provider == null)
            {
                Debug.LogError("[PowerUpDefinitionRegistry] Cannot register null provider.");
                return;
            }

            if (_provider != null && !ReferenceEquals(_provider, provider))
            {
                var sourceName = source != null ? source.name : "Unknown";
                Debug.LogWarning($"[PowerUpDefinitionRegistry] Provider was already registered. Overriding with provider from '{sourceName}'.");
            }

            _provider = provider;
        }

        public static PowerUpDefinition GetById(int definitionId)
        {
            TryGetDefinition(definitionId, out var definition);
            return definition;
        }

        public static bool TryGetDefinition(int definitionId, out PowerUpDefinition definition)
        {
            definition = null;

            if (definitionId < 0)
                return false;

            if (_provider == null)
            {
                if (!_missingProviderLogged)
                {
                    Debug.LogError("[PowerUpDefinitionRegistry] Definition lookup requested before provider registration. Ensure PowerUpSpawner registers its explicit PowerUpDefinition references before any PowerUp visual resolution.");
                    _missingProviderLogged = true;
                }

                return false;
            }

            if (_provider.TryGetDefinition(definitionId, out definition))
            {
                MissingDefinitionIds.Remove(definitionId);
                return true;
            }

            if (MissingDefinitionIds.Add(definitionId))
            {
                Debug.LogError($"[PowerUpDefinitionRegistry] No PowerUpDefinition is registered for definitionId {definitionId}.");
            }

            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _provider = null;
            _missingProviderLogged = false;
            MissingDefinitionIds.Clear();
        }
    }
}
