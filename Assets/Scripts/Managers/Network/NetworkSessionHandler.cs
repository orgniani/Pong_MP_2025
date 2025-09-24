using Fusion;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Managers.Network
{
    public class NetworkSessionHandler
    {
        public async Task<bool> StartServer(NetworkRunner runner, int maxPlayers, int buildIndex)
        {
            var args = new StartGameArgs()
            {
                GameMode = GameMode.Server,
                PlayerCount = maxPlayers,
                Scene = SceneRef.FromIndex(buildIndex),
                SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                SessionName = "PongServer"
            };

            var result = await runner.StartGame(args);
            Debug.Log($"Server started? {result.Ok}");
            return result.Ok;
        }

        public async Task<bool> StartClient(NetworkRunner runner, string serverName, int buildIndex)
        {
            var args = new StartGameArgs()
            {
                GameMode = GameMode.Client,
                Scene = SceneRef.FromIndex(buildIndex),
                SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                SessionName = serverName
            };

            var result = await runner.StartGame(args);
            Debug.Log($"Client joined? {result.Ok}");
            return result.Ok;
        }
    }
}