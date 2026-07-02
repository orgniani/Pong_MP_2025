using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Config;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Managers.Network
{
    public sealed class DedicatedServerMatchFlow : INetworkRunnerCallbacks
    {
        private readonly MatchRulesConfig _matchRulesConfig;
        private readonly TaskCompletionSource<ShutdownReason> _shutdownTcs;
        private readonly LobbyAutoStartCoordinator _lobbyAutoStartCoordinator;

        public DedicatedServerMatchFlow(
            MatchRulesConfig matchRulesConfig,
            TaskCompletionSource<ShutdownReason> shutdownTcs = null,
            LobbyAutoStartCoordinator lobbyAutoStartCoordinator = null)
        {
            _matchRulesConfig = matchRulesConfig;
            _shutdownTcs = shutdownTcs;
            _lobbyAutoStartCoordinator = lobbyAutoStartCoordinator ?? new LobbyAutoStartCoordinator();
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            TryStartMatch(runner);
        }

        private void TryStartMatch(NetworkRunner runner)
        {
            if (!runner.IsServer)
            {
                return;
            }

            var minPlayersToStart = ResolveMinPlayersToStart();
            if (minPlayersToStart < 1 || runner.ActivePlayers.Count() < minPlayersToStart)
            {
                return;
            }

            var matchSessionState = runner.GetComponent<MatchSessionState>();
            if (matchSessionState != null && !matchSessionState.CanStartMatch(runner.ActivePlayers.Count(), minPlayersToStart))
            {
                return;
            }

            var gameSceneIndex = SceneCatalog.GetGameIndex();
            if (gameSceneIndex < 0)
            {
                Debug.LogError("[DedicatedServerMatchFlow] Could not resolve game scene index from SceneCatalog.");
                return;
            }

            matchSessionState?.MarkMatchStarted();
            runner.LoadScene(SceneRef.FromIndex(gameSceneIndex));
        }

        private int ResolveMinPlayersToStart()
        {
            if (_matchRulesConfig == null)
            {
                Debug.LogError("[DedicatedServerMatchFlow] MatchRulesConfig is missing. Cannot evaluate match start condition.");
                return -1;
            }

            return _matchRulesConfig.ResolveMinPlayersToStart();
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            _lobbyAutoStartCoordinator.Cancel(runner);
            _shutdownTcs?.TrySetResult(shutdownReason);
        }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner)
        {
            if (!runner.IsServer)
                return;

            var lobbySceneIndex = SceneCatalog.GetLobbyIndex();
            if (lobbySceneIndex < 0)
            {
                Debug.LogError("[DedicatedServerMatchFlow] Could not resolve lobby scene index from SceneCatalog.");
                return;
            }

            if (SceneManager.GetActiveScene().buildIndex != lobbySceneIndex)
            {
                _lobbyAutoStartCoordinator.Cancel(runner);
                return;
            }

            var matchSessionState = runner.GetComponent<MatchSessionState>();
            if (matchSessionState?.MatchInProgress == true || matchSessionState?.IsPostGameCleanup == true)
                return;

            _lobbyAutoStartCoordinator.Schedule(runner, matchSessionState, () => TryStartMatch(runner));
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
            _lobbyAutoStartCoordinator.Cancel(runner);
        }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    }
}
