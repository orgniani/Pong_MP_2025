using UnityEngine;

namespace Config
{
    public static class SceneCatalog
    {
        public static int GetMainMenuIndex(int fallbackIndex)
        {
            return ResolveIndex(catalog => catalog.MainMenuBuildIndex, fallbackIndex, "MainMenu");
        }

        public static int GetMainMenuIndex()
        {
            return ResolveRequiredIndex(catalog => catalog.MainMenuBuildIndex, "MainMenu");
        }

        public static int GetLobbyIndex(int fallbackIndex)
        {
            return ResolveIndex(catalog => catalog.LobbyBuildIndex, fallbackIndex, "Lobby");
        }

        public static int GetLobbyIndex()
        {
            return ResolveRequiredIndex(catalog => catalog.LobbyBuildIndex, "Lobby");
        }

        public static int GetGameIndex(int fallbackIndex)
        {
            return ResolveIndex(catalog => catalog.GameBuildIndex, fallbackIndex, "Game");
        }

        public static int GetGameIndex()
        {
            return ResolveRequiredIndex(catalog => catalog.GameBuildIndex, "Game");
        }

        private static int ResolveIndex(System.Func<SceneIndexCatalog, int> selector, int fallbackIndex, string label)
        {
            var catalog = GetRegisteredCatalog();
            if (catalog == null)
            {
                Debug.LogWarning($"[SceneCatalog] Catalog is not initialized. Using fallback index {fallbackIndex} for {label}. Add SceneCatalogBootstrap in startup flow.");
                return fallbackIndex;
            }

            var configuredIndex = selector(catalog);
            if (configuredIndex >= 0)
            {
                WarnIfMissingInBuildSettings(configuredIndex, label);
                return configuredIndex;
            }

            Debug.LogWarning($"[SceneCatalog] Invalid index for {label} in {nameof(SceneIndexCatalog)}. Using fallback index {fallbackIndex}.");
            return fallbackIndex;
        }

        private static int ResolveRequiredIndex(System.Func<SceneIndexCatalog, int> selector, string label)
        {
            var catalog = GetRegisteredCatalog();
            if (catalog == null)
            {
                Debug.LogError($"[SceneCatalog] Catalog is not initialized. Cannot resolve required index for {label}. Add SceneCatalogBootstrap in startup flow.");
                return -1;
            }

            var configuredIndex = selector(catalog);
            if (configuredIndex >= 0)
            {
                WarnIfMissingInBuildSettings(configuredIndex, label);
                return configuredIndex;
            }

            Debug.LogError($"[SceneCatalog] Invalid index for {label} in {nameof(SceneIndexCatalog)}. Configure a non-negative value.");
            return -1;
        }

        private static SceneIndexCatalog GetRegisteredCatalog()
        {
            if (!SceneCatalogRegistry.TryGetCatalog(out var catalog) || catalog == null)
            {
                if (!SceneCatalogRegistry.IsInitialized)
                {
                    Debug.LogError("[SceneCatalog] SceneCatalogRegistry is not initialized. SceneCatalogBootstrap must run before scene resolution.");
                }
                else
                {
                    Debug.LogError("[SceneCatalog] SceneCatalogRegistry provider returned a null catalog.");
                }

                return null;
            }

            return catalog;
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
