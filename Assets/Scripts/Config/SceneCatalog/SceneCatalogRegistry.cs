using UnityEngine;

namespace Config
{
    public static class SceneCatalogRegistry
    {
        private static ISceneCatalogProvider _provider;

        public static bool IsInitialized => _provider != null;

        public static void RegisterProvider(ISceneCatalogProvider provider, Object source)
        {
            if (provider == null)
            {
                Debug.LogError("[SceneCatalogRegistry] Cannot register null provider.");
                return;
            }

            if (_provider != null && !ReferenceEquals(_provider, provider))
            {
                var sourceName = source != null ? source.name : "Unknown";
                Debug.LogWarning($"[SceneCatalogRegistry] Provider was already registered. Overriding with provider from '{sourceName}'.");
            }

            _provider = provider;
        }

        public static bool TryGetCatalog(out SceneIndexCatalog catalog)
        {
            if (_provider == null)
            {
                catalog = null;
                return false;
            }

            return _provider.TryGetCatalog(out catalog);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            _provider = null;
        }
    }
}
