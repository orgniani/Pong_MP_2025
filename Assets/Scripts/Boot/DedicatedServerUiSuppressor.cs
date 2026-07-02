using Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boot
{
    public static class DedicatedServerUiSuppressor
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (!DedicatedServerEnvironment.HasDedicatedFlag() || !DedicatedServerEnvironment.IsHeadless)
            {
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    SuppressCanvases(scene);
                }
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SuppressCanvases(scene);
        }

        private static void SuppressCanvases(Scene scene)
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas.gameObject.scene == scene)
                {
                    canvas.enabled = false;
                }
            }
        }
    }
}
