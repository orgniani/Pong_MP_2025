using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using Common;
using Managers.Network;
using System.Linq;
using static Unity.Collections.Unicode;

namespace Managers
{
    public class NetworkManager : MonoBehaviourSingleton<NetworkManager>, INetworkRunnerCallbacks
    {
        [Header("References")]
        [SerializeField] private Transform finishLine;
        [SerializeField] private BlockerManager blockerManager;
        [SerializeField] private Transform[] spawnPositions;

        [Header("Prefabs")]
        [SerializeField] private NetworkPrefabRef playerPrefab;
        [SerializeField] private NetworkPrefabRef timerManagerPrefab;
        [SerializeField] private NetworkPrefabRef racePositionManagerPrefab;
        [SerializeField] private NetworkPrefabRef gameOverManagerPrefab;

        [Header("Settings")]
        [SerializeField] private int minPlayers = 2;

        private RacePositionManager _racePositionManager;
        private TimerManager _timerManager;
        private GameOverManager _gameOverManager;
        
        private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

        private NetworkRunner _networkRunner;
        private NetworkPlayerSpawner _playerSpawner;
        private NetworkInputHandler _inputHandler;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnNewPlayerJoined;
        public event Action<string> OnJoinedPlayerLeft;

        public NetworkPlayerSpawner PlayerSpawner => _playerSpawner;
        public GameOverManager GameOverManager => _gameOverManager;
        //public NetworkPlayerSetup LocalPlayer { get; set; }

        private void Awake()
        {
            _networkRunner = FindFirstObjectByType<NetworkRunner>();
            if (_networkRunner == null)
            {
                Debug.LogWarning("No NetworkRunner found in scene!");
                return;
            }

            _networkRunner.AddCallbacks(this);
        }
        void OnApplicationQuit ()
        {
            if (_networkRunner)
            {
                _networkRunner.Shutdown();
                _networkRunner = null;
            }
        }

        public void Shutdown ()
        {
            if (_networkRunner)
                _networkRunner.Shutdown();
        }

        public Vector3 GetRespawnPoint(PlayerRef player)
        {
            int index = player.PlayerId % spawnPositions.Length;
            return spawnPositions[index].position;
        }

        //public void RegisterLocalPlayerInput(NetworkPlayerSetup localPlayer)
        //{
        //    LocalPlayer = localPlayer;
        //    _inputHandler = new NetworkInputHandler(localPlayer);
        //}

        void INetworkRunnerCallbacks.OnShutdown (NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (shutdownReason == ShutdownReason.GameNotFound)
                return;

            if (_networkRunner.IsServer)
                _spawnedPlayers.Clear();

            _networkRunner = null;

            OnDisconnected?.Invoke();
        }

        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
            {
                _racePositionManager = FindFirstObjectByType<RacePositionManager>();
                if (_racePositionManager == null)
                {
                    _racePositionManager = runner.Spawn(racePositionManagerPrefab, Vector3.zero, Quaternion.identity)
                                                .GetComponent<RacePositionManager>();
                }

                _playerSpawner ??= new NetworkPlayerSpawner(spawnPositions, playerPrefab, _racePositionManager, finishLine);
                _playerSpawner.SpawnPlayer(runner, player);

                if (runner.ActivePlayers.Count() >= minPlayers)
                {
                    var timerObj = runner.Spawn(timerManagerPrefab, Vector3.zero, Quaternion.identity);
                    _timerManager = timerObj.GetComponent<TimerManager>();
                    _timerManager.StartRaceCountdown(blockerManager);

                    var gameOverManagerObj = runner.Spawn(gameOverManagerPrefab, Vector3.zero, Quaternion.identity);
                    _gameOverManager = gameOverManagerObj.GetComponent<GameOverManager>();
                }
            }

            OnNewPlayerJoined?.Invoke("Player_" + player.PlayerId);
        }

        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
            {
                _playerSpawner.DespawnPlayer(runner, player);

                if (_playerSpawner.SpawnedPlayerCount == 0)
                    Shutdown();
            }

            OnJoinedPlayerLeft?.Invoke("Player_" + player.PlayerId);
        }

        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
        {
            Debug.Log("Connected to server!");

            if (_networkRunner.IsClient)
                OnConnected?.Invoke();
        }

        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.LogWarning($"Disconnected from server: {reason}");

            if (_networkRunner.IsClient)
                Shutdown();
        }

        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
        {
            //_inputHandler?.CollectInput(input);
        }

        void INetworkRunnerCallbacks.OnInputMissing (NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        void INetworkRunnerCallbacks.OnConnectRequest (NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        void INetworkRunnerCallbacks.OnConnectFailed (NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        void INetworkRunnerCallbacks.OnUserSimulationMessage (NetworkRunner runner, SimulationMessagePtr message) { }
        void INetworkRunnerCallbacks.OnSessionListUpdated (NetworkRunner runner, List<SessionInfo> sessionList) { }
        void INetworkRunnerCallbacks.OnCustomAuthenticationResponse (NetworkRunner runner, Dictionary<string, object> data) { }
        void INetworkRunnerCallbacks.OnHostMigration (NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        void INetworkRunnerCallbacks.OnSceneLoadDone (NetworkRunner runner) { }
        void INetworkRunnerCallbacks.OnSceneLoadStart (NetworkRunner runner) { }
        void INetworkRunnerCallbacks.OnObjectExitAOI (NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnObjectEnterAOI (NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnReliableDataReceived (NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        void INetworkRunnerCallbacks.OnReliableDataProgress (NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    }
}