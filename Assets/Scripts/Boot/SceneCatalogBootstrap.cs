using Config;
using Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boot
{
    [DefaultExecutionOrder(-1000)]
    public class SceneCatalogBootstrap : MonoBehaviour
    {
        [SerializeField] private SceneIndexCatalog sceneIndexCatalog;

        private void Awake()
        {
            if (!ReferenceValidator.Validate(sceneIndexCatalog, nameof(sceneIndexCatalog), this))
                return;

            SceneCatalogRegistry.RegisterProvider(new SceneCatalogProvider(sceneIndexCatalog), this);
            ValidateConfiguredBuildIndexes(sceneIndexCatalog);
        }

        private static void ValidateConfiguredBuildIndexes(SceneIndexCatalog catalog)
        {
            ValidateBuildIndex(catalog.MainMenuBuildIndex, nameof(catalog.MainMenuBuildIndex));
            ValidateBuildIndex(catalog.LobbyBuildIndex, nameof(catalog.LobbyBuildIndex));
            ValidateBuildIndex(catalog.GameBuildIndex, nameof(catalog.GameBuildIndex));
        }

        private static void ValidateBuildIndex(int buildIndex, string label)
        {
            if (buildIndex < 0)
            {
                Debug.LogError($"[SceneCatalogBootstrap] {label} is negative ({buildIndex}). Configure a non-negative Build Settings index.");
                return;
            }

            var scenePath = SceneUtility.GetScenePathByBuildIndex(buildIndex);
            if (string.IsNullOrEmpty(scenePath))
            {
                Debug.LogWarning($"[SceneCatalogBootstrap] {label} points to build index {buildIndex}, which is not present in Build Settings.");
            }
        }
    }
}
