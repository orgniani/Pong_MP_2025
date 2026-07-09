using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using Common;
using Config;
using Managers.Network;
using System.Linq;
using Balls;
using Helpers;
using UnityEngine.SceneManagement;

namespace Managers
{
    public class NetworkManager : MonoBehaviourSingleton<NetworkManager>, INetworkRunnerCallbacks
    {
        private const string LogPrefix = "[NetLifecycle]";

        [Header("References")]
        [SerializeField] private Transform[] spawnPositions;

        [Header("Prefabs")]
        [SerializeField] private NetworkPrefabRef playerPrefab;
        [SerializeField] private NetworkPrefabRef timerManagerPrefab;
        [SerializeField] private NetworkPrefabRef scoreManagerPrefab;
        [SerializeField] private NetworkPrefabRef gameOverManagerPrefab;

        [Header("Settings")]
        [SerializeField] private MatchRulesConfig matchRulesConfig;
        [SerializeField] private bool enableLogs = false;

        private ScoreManager _scoreManager;
        private TimerManager _timerManager;
        private GameOverManager _gameOverManager;
        
        private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new Dictionary<PlayerRef, NetworkObject>();

        private NetworkRunner _networkRunner;
        private NetworkPlayerSpawner _playerSpawner;
        private NetworkInputHandler _inputHandler;
        private Ball _boundBall;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action OnRosterChanged;

        public NetworkPlayerSpawner PlayerSpawner => _playerSpawner;
        public GameOverManager GameOverManager => _gameOverManager;
        public bool IsServer => _networkRunner != null && _networkRunner.IsServer;

        private void Awake()
        {
            var runner = FindFirstObjectByType<NetworkRunner>();
            if (runner == null)
                return;

            BindRunner(runner);
        }

        private void OnDestroy()
        {
            _networkRunner?.RemoveCallbacks(this);
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
            if (!_networkRunner)
                return;

            _networkRunner.Shutdown();
        }

        public bool BindRunner(NetworkRunner runner)
        {
            if (runner == null)
                return false;

            if (_networkRunner == runner)
                return false;

            _networkRunner?.RemoveCallbacks(this);
            _networkRunner = runner;
            _networkRunner.AddCallbacks(this);
            return true;
        }

        public Transform GetSpawnPoint(int index)
        {
            TryGetSpawnPoint(index, out Transform spawnPoint);
            return spawnPoint;
        }

        public bool TryGetSpawnPoint(int index, out Transform spawnPoint)
        {
            spawnPoint = null;

            if (spawnPositions == null || spawnPositions.Length == 0)
                return false;

            int normalizedIndex = ((index % spawnPositions.Length) + spawnPositions.Length) % spawnPositions.Length;
            spawnPoint = spawnPositions[normalizedIndex];

            return spawnPoint != null;
        }

        public Vector3 GetRespawnPoint(PlayerRef player)
        {
            return TryGetSpawnPoint(player.PlayerId, out Transform spawnPoint)
                ? spawnPoint.position
                : Vector3.zero;
        }

        void INetworkRunnerCallbacks.OnShutdown (NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (shutdownReason == ShutdownReason.GameNotFound)
                return;

            if (_networkRunner != null && _networkRunner.IsServer)
                _spawnedPlayers.Clear();

            _networkRunner = null;

            if (runner != null && runner.gameObject != null)
                Destroy(runner.gameObject);

            OnDisconnected?.Invoke();
        }

        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
            {
                var matchSessionState = runner.GetComponent<MatchSessionState>();
                if (matchSessionState != null && matchSessionState.IsPostGameCleanup)
                {
                    Log($"cleanup join rejected: player={player.PlayerId}, session='{runner.SessionInfo.Name}'");
                    runner.Disconnect(player, null);
                    return;
                }

                _playerSpawner ??= new NetworkPlayerSpawner(spawnPositions, playerPrefab);
                if (IsGameSceneActive() && matchSessionState?.MatchInProgress == true)
                {
                    SpawnMissingPlayer(runner, player);
                    EnsureGameplayManagers(runner);
                }
            }

            OnRosterChanged?.Invoke();
        }

        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner.IsServer)
            {
                if (_playerSpawner != null && _playerSpawner.IsSpawned(player))
                    _playerSpawner.DespawnPlayer(runner, player);

                var matchSessionState = runner.GetComponent<MatchSessionState>();
                if (matchSessionState != null && matchSessionState.MatchInProgress
                    && _gameOverManager != null && !_gameOverManager.IsGameOver
                    && runner.ActivePlayers.Count() < ResolveMinPlayersToStart())
                {
                    _gameOverManager.TriggerForfeit("Opponent disconnected");
                }

                if (runner.ActivePlayers.Count() == 0)
                {
                    ResetToWaitingState(runner, matchSessionState);
                }
                else if (matchSessionState != null && !matchSessionState.MatchInProgress
                         && !matchSessionState.IsPostGameCleanup
                         && runner.ActivePlayers.Count() < ResolveMinPlayersToStart())
                {
                    matchSessionState.RearmAutoStart();
                }
            }

            OnRosterChanged?.Invoke();
        }

        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
        {
            Log($"connected: mode={runner.GameMode}, session='{runner.SessionInfo.Name}'");

            if (_networkRunner.IsClient)
                OnConnected?.Invoke();
        }

        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.LogWarning($"{LogPrefix} disconnect reason: mode={runner.GameMode}, reason={reason}, session='{runner.SessionInfo.Name}'");

            if (_networkRunner != null && _networkRunner.IsClient)
                Shutdown();
        }

        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
        {
            if (!runner.IsClient || !runner.IsPlayerValid(runner.LocalPlayer))
            {
                return;
            }

            input.Set(new Players.PlayerInputData
            {
                MoveY = Input.GetAxisRaw("Vertical")
            });
        }

        void INetworkRunnerCallbacks.OnInputMissing (NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        void INetworkRunnerCallbacks.OnConnectRequest (NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        void INetworkRunnerCallbacks.OnConnectFailed (NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        void INetworkRunnerCallbacks.OnUserSimulationMessage (NetworkRunner runner, SimulationMessagePtr message) { }
        void INetworkRunnerCallbacks.OnSessionListUpdated (NetworkRunner runner, List<SessionInfo> sessionList) { }
        void INetworkRunnerCallbacks.OnCustomAuthenticationResponse (NetworkRunner runner, Dictionary<string, object> data) { }
        void INetworkRunnerCallbacks.OnHostMigration (NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
        {
            if (!runner.IsServer) return;

            var matchSessionState = runner.GetComponent<MatchSessionState>();

            if (!IsGameSceneActive())
            {
                PrepareForLobbyState();

                if (matchSessionState != null && !matchSessionState.IsPostGameCleanup
                    && runner.ActivePlayers.Count() < ResolveMinPlayersToStart())
                    matchSessionState?.RearmAutoStart();

                return;
            }

            if (runner.ActivePlayers.Count() == 0)
            {
                ResetToWaitingState(runner, matchSessionState);
                return;
            }

            if (matchSessionState?.MatchInProgress != true)
                return;

            _playerSpawner ??= new NetworkPlayerSpawner(spawnPositions, playerPrefab);

            foreach (var player in runner.ActivePlayers)
            {
                SpawnMissingPlayer(runner, player);
            }

            EnsureGameplayManagers(runner);

            if (runner.ActivePlayers.Count() < ResolveMinPlayersToStart()
                && _gameOverManager != null && !_gameOverManager.IsGameOver)
            {
                _gameOverManager.TriggerForfeit("Opponent disconnected");
            }
        }
        void INetworkRunnerCallbacks.OnSceneLoadStart (NetworkRunner runner) { }
        void INetworkRunnerCallbacks.OnObjectExitAOI (NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnObjectEnterAOI (NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnReliableDataReceived (NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        void INetworkRunnerCallbacks.OnReliableDataProgress (NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

        private void TryBindBallGoalCallbacks()
        {
            if (_scoreManager == null)
                return;

            var ball = FindFirstObjectByType<Ball>();
            if (ball == null)
                return;

            if (_boundBall == ball)
                return;

            ClearBallGoalCallbacks();

            ball.onLeftGoal.AddListener(_scoreManager.RegisterRightGoal);
            ball.onRightGoal.AddListener(_scoreManager.RegisterLeftGoal);
            _boundBall = ball;
        }

        public void PrepareForLobbyState()
        {
            PlayerNameLookup.ResetCachedSideNames();
            ClearBallGoalCallbacks();
            _playerSpawner?.ClearAll();
            _scoreManager = null;
            _timerManager = null;
            _gameOverManager = null;
        }

        public void ForceDisconnectAllPlayers(NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer)
                return;

            var playersToDisconnect = runner.ActivePlayers.ToArray();
            foreach (var player in playersToDisconnect)
            {
                Log($"disconnecting player={player.PlayerId} during post-game cleanup");
                runner.Disconnect(player, null);
            }
        }

        private void ClearBallGoalCallbacks()
        {
            if (_boundBall != null && _scoreManager != null)
            {
                _boundBall.onLeftGoal.RemoveListener(_scoreManager.RegisterRightGoal);
                _boundBall.onRightGoal.RemoveListener(_scoreManager.RegisterLeftGoal);
            }

            _boundBall = null;
        }

        private void EnsureGameplayManagers(NetworkRunner runner)
        {
            _scoreManager = FindFirstObjectByType<ScoreManager>()
                            ?? runner.Spawn(scoreManagerPrefab, Vector3.zero, Quaternion.identity).GetComponent<ScoreManager>();

            if (_timerManager == null)
            {
                _timerManager = FindFirstObjectByType<TimerManager>();
                if (_timerManager == null)
                {
                    var timerObj = runner.Spawn(timerManagerPrefab, Vector3.zero, Quaternion.identity);
                    _timerManager = timerObj.GetComponent<TimerManager>();
                    _timerManager.ResetTimer();
                    _timerManager.StartMatchCountdown();
                }
            }

            _gameOverManager = FindFirstObjectByType<GameOverManager>()
                               ?? runner.Spawn(gameOverManagerPrefab, Vector3.zero, Quaternion.identity).GetComponent<GameOverManager>();

            TryBindBallGoalCallbacks();
        }

        private void SpawnMissingPlayer(NetworkRunner runner, PlayerRef player)
        {
            if (_playerSpawner.IsSpawned(player))
                return;

            _playerSpawner.SpawnPlayer(runner, player);
        }

        private void ResetToWaitingState(NetworkRunner runner, MatchSessionState matchSessionState)
        {
            matchSessionState?.ResetToWaitingForPlayers();
            PrepareForLobbyState();

            var lobbySceneIndex = SceneCatalog.GetLobbyIndex();
            if (lobbySceneIndex < 0)
            {
                Debug.LogError("[NetworkManager] Could not resolve Lobby scene index from SceneCatalog.");
                return;
            }

            if (SceneManager.GetActiveScene().buildIndex == lobbySceneIndex)
                return;

            runner.LoadScene(SceneRef.FromIndex(lobbySceneIndex));
        }

        private int ResolveMinPlayersToStart()
        {
            return matchRulesConfig != null ? matchRulesConfig.ResolveMinPlayersToStart() : 1;
        }

        private void Log(string message)
        {
            if (!enableLogs)
                return;

            Debug.Log($"{LogPrefix} {message}", this);
        }

        private static bool IsGameSceneActive()
        {
            return SceneManager.GetActiveScene().buildIndex == SceneCatalog.GetGameIndex();
        }
    }
}
