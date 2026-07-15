using UnityEngine;

namespace Config
{
    public static class SceneCatalog
    {
        public static int GetMainMenuIndex(int fallbackIndex)
        {
            return GetIndexOrFallback(fallbackIndex, "MainMenu", catalog => catalog.MainMenuBuildIndex);
        }

        public static int GetMainMenuIndex()
        {
            return GetRequiredIndex("MainMenu", catalog => catalog.MainMenuBuildIndex);
        }

        public static int GetLobbyIndex(int fallbackIndex)
        {
            return GetIndexOrFallback(fallbackIndex, "Lobby", catalog => catalog.LobbyBuildIndex);
        }

        public static int GetLobbyIndex()
        {
            return GetRequiredIndex("Lobby", catalog => catalog.LobbyBuildIndex);
        }

        public static int GetGameIndex(int fallbackIndex)
        {
            return GetIndexOrFallback(fallbackIndex, "Game", catalog => catalog.GameBuildIndex);
        }

        public static int GetGameIndex()
        {
            return GetRequiredIndex("Game", catalog => catalog.GameBuildIndex);
        }

        private static int GetIndexOrFallback(int fallbackIndex, string label, System.Func<SceneIndexCatalog, int> selector)
        {
            if (!TryGetCatalog(out var catalog))
            {
                Debug.LogWarning($"[SceneCatalog] Catalog is not initialized. Using fallback index {fallbackIndex} for {label}. Add SceneCatalogBootstrap in startup flow.");
                return fallbackIndex;
            }

            return ResolveConfiguredIndex(label, selector(catalog), fallbackIndex);
        }

        private static int GetRequiredIndex(string label, System.Func<SceneIndexCatalog, int> selector)
        {
            if (!TryGetCatalog(out var catalog))
            {
                Debug.LogError($"[SceneCatalog] Catalog is not initialized. Cannot resolve required index for {label}. Add SceneCatalogBootstrap in startup flow.");
                return -1;
            }

            return ResolveConfiguredIndex(label, selector(catalog), -1);
        }

        private static int ResolveConfiguredIndex(string label, int configuredIndex, int fallbackIndex)
        {
            if (configuredIndex >= 0)
            {
                WarnIfMissingInBuildSettings(configuredIndex, label);
                return configuredIndex;
            }

            if (fallbackIndex >= 0)
            {
                Debug.LogWarning($"[SceneCatalog] Invalid index for {label} in {nameof(SceneIndexCatalog)}. Using fallback index {fallbackIndex}.");
                return fallbackIndex;
            }

            Debug.LogError($"[SceneCatalog] Invalid index for {label} in {nameof(SceneIndexCatalog)}. Configure a non-negative value.");
            return -1;
        }

        private static bool TryGetCatalog(out SceneIndexCatalog catalog)
        {
            if (!SceneCatalogRegistry.TryGetCatalog(out catalog) || catalog == null)
            {
                if (!SceneCatalogRegistry.IsInitialized)
                {
                    Debug.LogError("[SceneCatalog] SceneCatalogRegistry is not initialized. SceneCatalogBootstrap must run before scene resolution.");
                }
                else
                {
                    Debug.LogError("[SceneCatalog] SceneCatalogRegistry provider returned a null catalog.");
                }

                catalog = null;
                return false;
            }

            return true;
        }

        private static void WarnIfMissingInBuildSettings(int buildIndex, string label)
        {
            var scenePath = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(buildIndex);
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogWarning($"[SceneCatalog] {label} configured with build index {buildIndex}, but that index is not present in Build Settings.");
            }
        }
    }
}
