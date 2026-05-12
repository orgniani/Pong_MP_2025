using Fusion;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Managers.Network
{
    public class NetworkSessionHandler
    {
        public const string DefaultSessionName = "PongServer";

        private const string LogPrefix = "[NetLifecycle]";

        private static INetworkSceneManager GetOrAddSceneManager(NetworkRunner runner)
        {
            var sceneManager = runner.GetComponent<NetworkSceneManagerDefault>();
            if (sceneManager == null)
            {
                sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
            }

            return sceneManager;
        }

        public async Task<bool> StartServer(NetworkRunner runner, int maxPlayers, int buildIndex)
        {
            runner.ProvideInput = false;
            var sessionName = DefaultSessionName;

            Debug.Log($"{LogPrefix} server started: initializing session='{sessionName}', sceneBuildIndex={buildIndex}, maxPlayers={maxPlayers}");

            var args = new StartGameArgs()
            {
                GameMode = GameMode.Server,
                PlayerCount = maxPlayers,
                Scene = SceneRef.FromIndex(buildIndex),
                SceneManager = GetOrAddSceneManager(runner),
                SessionName = sessionName
            };

            var result = await runner.StartGame(args);
            Debug.Log($"{LogPrefix} server started result: ok={result.Ok}, session='{sessionName}'");
            return result.Ok;
        }

        public async Task<bool> StartClient(NetworkRunner runner, string serverName, int buildIndex)
        {
            runner.ProvideInput = true;
            var sessionName = string.IsNullOrWhiteSpace(serverName) ? DefaultSessionName : serverName;

            Debug.Log($"{LogPrefix} client connecting: session='{sessionName}', sceneBuildIndex={buildIndex}");

            var args = new StartGameArgs()
            {
                GameMode = GameMode.Client,
                Scene = SceneRef.FromIndex(buildIndex),
                SceneManager = GetOrAddSceneManager(runner),
                SessionName = sessionName
            };

            var result = await runner.StartGame(args);
            Debug.Log($"{LogPrefix} client connect result: ok={result.Ok}, session='{sessionName}'");
            return result.Ok;
        }
    }
}
