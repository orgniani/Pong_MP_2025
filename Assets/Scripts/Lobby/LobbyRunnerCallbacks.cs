using Config;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lobby
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkRunner))]
    public sealed class LobbyRunnerCallbacks : MonoBehaviour, INetworkRunnerCallbacks
    {
        private NetworkRunner _runner;
        private LobbySessionState _lobbySessionState;
        private bool _callbacksRegistered;

        public static LobbyRunnerCallbacks EnsureOnRunner(NetworkRunner runner)
        {
            if (runner == null)
                return null;

            var callbacks = runner.GetComponent<LobbyRunnerCallbacks>() ?? runner.gameObject.AddComponent<LobbyRunnerCallbacks>();
            callbacks.Bind(runner);
            return callbacks;
        }

        private void Awake()
        {
            Bind(GetComponent<NetworkRunner>());
        }

        private void OnEnable()
        {
            Bind(_runner != null ? _runner : GetComponent<NetworkRunner>());
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
        }

        private void Bind(NetworkRunner runner)
        {
            if (runner == null)
                return;

            if (_runner != runner)
            {
                UnregisterCallbacks();
                _runner = runner;
            }

            _lobbySessionState = LobbySessionState.EnsureOnRunner(_runner);

            if (!_callbacksRegistered)
            {
                _runner.AddCallbacks(this);
                _callbacksRegistered = true;
            }

            if (IsLobbySceneActive())
                EnterLobbyState();
        }

        private void EnterLobbyState()
        {
            _lobbySessionState = LobbySessionState.EnsureOnRunner(_runner);

            if (!LobbySceneCompositionRoot.TryGetActive(out var compositionRoot))
            {
                Debug.LogError("[LobbyRunnerCallbacks] Lobby scene is active but LobbySceneCompositionRoot was not found in the active scene.", this);
                return;
            }

            compositionRoot.Compose(_runner);
        }

        private void UnregisterCallbacks()
        {
            if (!_callbacksRegistered || _runner == null)
                return;

            _runner.RemoveCallbacks(this);
            _callbacksRegistered = false;
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            _lobbySessionState?.HandlePlayerJoined(runner, player);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            _lobbySessionState?.HandlePlayerLeft(runner, player);
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            if (IsLobbySceneActive())
                EnterLobbyState();
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            _lobbySessionState?.ResetState();
            UnregisterCallbacks();

            if (_runner == runner)
                _runner = null;
        }

        private static bool IsLobbySceneActive()
        {
            return SceneManager.GetActiveScene().buildIndex == SceneCatalog.GetLobbyIndex();
        }

        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    }
}
