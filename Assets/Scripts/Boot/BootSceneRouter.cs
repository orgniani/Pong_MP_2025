using System.Collections;
using Config;
using Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boot
{
    public class BootSceneRouter : MonoBehaviour
    {
        [SerializeField] private float delaySeconds;

        private IEnumerator Start()
        {
            if (DedicatedServerEnvironment.HasDedicatedFlag())
            {
                yield break;
            }

            var targetSceneBuildIndex = SceneCatalog.GetMainMenuIndex();

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
    }
}
