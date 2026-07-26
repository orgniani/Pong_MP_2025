using Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Boot
{
    public static class DedicatedServerUiSuppressor
    {
        private static bool _installed;

        public static void Install()
        {
            if (_installed)
            {
                return;
            }

            if (!DedicatedServerEnvironment.HasDedicatedFlag() || !DedicatedServerEnvironment.IsHeadless)
            {
                return;
            }

            _installed = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SuppressAllCanvases();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SuppressAllCanvases();
        }

        private static void SuppressAllCanvases()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                canvas.enabled = false;
            }
        }
    }
}
