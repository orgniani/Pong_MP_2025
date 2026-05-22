using System.Collections;
using System;
using Config;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boot
{
    public class BootSceneRouter : MonoBehaviour
    {
        [SerializeField] private float delaySeconds;

        private IEnumerator Start()
        {
            var targetSceneBuildIndex = HasDedicatedFlag()
                ? SceneCatalog.GetLobbyIndex()
                : SceneCatalog.GetMainMenuIndex();

            if (targetSceneBuildIndex < 0)
            {
                Debug.LogWarning("BootSceneRouter could not resolve target scene build index.");
                yield break;
            }

            if (SceneManager.GetActiveScene().buildIndex == targetSceneBuildIndex)
            {
                yield break;
            }

            if (delaySeconds > 0f)
            {
                yield return new WaitForSecondsRealtime(delaySeconds);
            }

            SceneManager.LoadScene(targetSceneBuildIndex);
        }

        private static bool HasDedicatedFlag()
        {
            var args = Environment.GetCommandLineArgs();
            return Array.Exists(args, arg =>
                string.Equals(arg, "-dedicated", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-dedicatedServer", StringComparison.OrdinalIgnoreCase));
        }
    }
}
