using Fusion;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Managers.Network
{
    public class NetworkSessionHandler
    {
        public async Task<bool> StartSession(NetworkRunner runner, GameMode gameMode, int maxPlayers)
        {
            StartGameArgs args = new StartGameArgs()
            {
                GameMode = gameMode,
                PlayerCount = maxPlayers,
                SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                SessionName = "RaceGame"
            };

            var result = await runner.StartGame(args);
            Debug.Log($"Session started? {result.Ok}");
            return result.Ok;
        }

        public async Task<bool> JoinOrCreateSession(NetworkRunner runner, int maxPlayers, int buildIndex)
        {
            var args = new StartGameArgs()
            {
                GameMode = GameMode.AutoHostOrClient,
                Scene = SceneRef.FromIndex(buildIndex),
                SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                PlayerCount = maxPlayers
            };

            var result = await runner.StartGame(args);
            Debug.Log($"Start game result: {result.Ok}");
            return result.Ok;
        }

    }
}