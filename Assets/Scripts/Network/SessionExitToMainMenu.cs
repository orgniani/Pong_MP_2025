using Config;
using Fusion;
using Helpers;
using Lobby;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Network
{
    public static class SessionExitToMainMenu
    {
        private static bool _isExecuting;

        public static async void Execute(string logPrefix)
        {
            if (_isExecuting)
                return;

            _isExecuting = true;

            try
            {
                PlayerNameLookup.ResetCachedSideNames();
                var lobbySessionState = LobbySessionState.ActiveInstance;
                lobbySessionState?.ResetState();
                await CleanupLocalRunnersAsync(logPrefix);

                var index = SceneCatalog.GetMainMenuIndex(-1);
                if (index < 0)
                {
                    Debug.LogError($"{logPrefix} Could not resolve MainMenu scene index.");
                    return;
                }

                SceneManager.LoadScene(index);
            }
            finally
            {
                _isExecuting = false;
            }
        }

        private static async Task CleanupLocalRunnersAsync(string logPrefix)
        {
            var activeRunner = LobbySessionState.ActiveInstance?.Runner;
            var runners = UnityEngine.Object.FindObjectsByType<NetworkRunner>(FindObjectsSortMode.InstanceID)
                .Where(runner => runner != null && (!runner.IsServer || !Application.isBatchMode))
                .OrderByDescending(runner => runner == activeRunner)
                .ThenByDescending(runner => runner.IsRunning)
                .ToArray();

            foreach (var runner in runners)
            {
                try
                {
                    if (runner.IsRunning)
                        await runner.Shutdown();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"{logPrefix} Runner shutdown before main-menu return failed: {ex.Message}");
                }

                if (runner != null && runner.gameObject != null)
                    UnityEngine.Object.Destroy(runner.gameObject);
            }
        }
    }
}
