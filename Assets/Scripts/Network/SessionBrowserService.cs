using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using Lobby;
using UnityEngine;

namespace Network
{
    public sealed class SessionBrowserService : MonoBehaviour, INetworkRunnerCallbacks
    {
        public event Action<IReadOnlyList<SessionInfo>> OnSessionsUpdated;
        public event Action<bool> OnLobbyJoined;

        private readonly NetworkSessionHandler _sessionHandler = new NetworkSessionHandler();
        private readonly List<SessionInfo> _sessions = new List<SessionInfo>();
        private NetworkRunner _lobbyRunner;
        private bool _isBusy;

        public IReadOnlyList<SessionInfo> Sessions => _sessions;

        public async Task<bool> JoinLobbyAsync(SessionLobby lobby = SessionLobby.ClientServer)
        {
            if (_isBusy)
            {
                return false;
            }

            _isBusy = true;
            try
            {
                await ShutdownLobbyRunnerAsync();

                _lobbyRunner = CreateRunner("LobbyBrowserRunner");
                _lobbyRunner.AddCallbacks(this);

                var ok = await _sessionHandler.JoinLobbyAsync(_lobbyRunner, lobby);

                if (!ok)
                {
                    await ShutdownLobbyRunnerAsync();
                }

                OnLobbyJoined?.Invoke(ok);
                return ok;
            }
            finally
            {
                _isBusy = false;
            }
        }

        public async Task<bool> JoinSessionAsync(string sessionName)
        {
            if (string.IsNullOrWhiteSpace(sessionName))
            {
                Debug.LogError("[SessionBrowserService] JoinSession called with an empty session name.", this);
                return false;
            }

            if (_isBusy)
            {
                return false;
            }

            _isBusy = true;
            try
            {
                var gameRunner = _lobbyRunner != null ? _lobbyRunner : CreateRunner("NetworkRunner");
                if (_lobbyRunner != null)
                {
                    _lobbyRunner.RemoveCallbacks(this);
                }
                _lobbyRunner = null;
                LobbyRunnerCallbacks.EnsureOnRunner(gameRunner);

                var ok = await _sessionHandler.StartClient(gameRunner, sessionName);

                if (!ok && gameRunner != null && gameRunner.gameObject != null)
                {
                    Destroy(gameRunner.gameObject);
                }

                return ok;
            }
            finally
            {
                _isBusy = false;
            }
        }

        public void LeaveLobby()
        {
            _ = ShutdownLobbyRunnerAsync();
        }

        private NetworkRunner CreateRunner(string runnerName)
        {
            var runnerObject = new GameObject(runnerName, typeof(NetworkRunner));
            return runnerObject.GetComponent<NetworkRunner>();
        }

        private async Task ShutdownLobbyRunnerAsync()
        {
            if (_lobbyRunner == null)
            {
                return;
            }

            var runner = _lobbyRunner;
            _lobbyRunner = null;
            _sessions.Clear();

            runner.RemoveCallbacks(this);

            if (runner.IsRunning)
            {
                await runner.Shutdown();
            }
            else if (runner != null && runner.gameObject != null)
            {
                Destroy(runner.gameObject);
            }
        }

        private void OnDestroy()
        {
            OnSessionsUpdated = null;
            OnLobbyJoined = null;

            if (_lobbyRunner == null)
            {
                return;
            }

            _lobbyRunner.RemoveCallbacks(this);
            if (_lobbyRunner.IsRunning)
            {
                _ = _lobbyRunner.Shutdown();
            }
            _lobbyRunner = null;
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            _sessions.Clear();
            if (sessionList != null)
            {
                _sessions.AddRange(sessionList);
            }

            OnSessionsUpdated?.Invoke(_sessions);
        }

        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            if (runner != _lobbyRunner)
                return;

            _ = ShutdownLobbyRunnerAsync();
            OnSessionsUpdated?.Invoke(_sessions);
        }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    }
}
