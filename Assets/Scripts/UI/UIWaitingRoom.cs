using Fusion.Sockets;
using Fusion;
using UnityEngine;
using System.Linq;
using Config;


namespace UI
{
    public class UIWaitingRoom : MonoBehaviour, INetworkRunnerCallbacks
    {
        [SerializeField] private MatchRulesConfig matchRulesConfig;

        private NetworkRunner _runner;

        private void Start()
        {
            _runner = FindFirstObjectByType<NetworkRunner>();
            if (_runner != null)
                _runner.AddCallbacks(this);
        }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"Player {player.PlayerId} joined. Total: {runner.ActivePlayers.Count()}");

            var minPlayersToStart = ResolveMinPlayersToStart();
            if (minPlayersToStart < 1)
            {
                return;
            }

            if (runner.IsServer && runner.ActivePlayers.Count() >= minPlayersToStart)
            {
                Debug.Log("Min players reached, starting game!");
                var resolvedGameSceneIndex = SceneCatalog.GetGameIndex();
                if (resolvedGameSceneIndex < 0)
                {
                    Debug.LogError("Could not resolve game scene index from SceneIndexCatalog.");
                    return;
                }

                runner.LoadScene(SceneRef.FromIndex(resolvedGameSceneIndex));
            }
        }

        private int ResolveMinPlayersToStart()
        {
            if (matchRulesConfig == null)
            {
                Debug.LogError("[UIWaitingRoom] MatchRulesConfig is missing. Cannot evaluate start condition.");
                return -1;
            }

            return matchRulesConfig.ResolveMinPlayersToStart();
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    }
}
