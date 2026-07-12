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
        [SerializeField] private Transform[] oneVsOneSpawnPoints;
        [SerializeField] private Transform[] twoVsTwoSpawnPoints;

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
            ReferenceValidator.ValidateOptional(matchRulesConfig, nameof(matchRulesConfig), this);
            if (matchRulesConfig != null)
                MatchRulesRegistry.RegisterProvider(new MatchRulesProvider(matchRulesConfig), this);
            ReferenceValidator.ValidateOptional(oneVsOneSpawnPoints, nameof(oneVsOneSpawnPoints), this);
            ReferenceValidator.ValidateOptional(twoVsTwoSpawnPoints, nameof(twoVsTwoSpawnPoints), this);

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
            RefreshLaneVisibilityForCurrentMode(_networkRunner);
            return true;
        }

        public Transform GetSpawnPoint(int index)
        {
            TryGetSpawnPoint(index, out Transform spawnPoint);
            return spawnPoint;
        }

        public bool TryGetSpawnPoint(int index, out Transform spawnPoint)
        {
            return TryGetSpawnPoint(ResolveCurrentMode(), index, out spawnPoint);
        }

        public bool TryResolveSpawnAssignment(NetworkRunner runner, PlayerRef player, out int spawnPointIndex, out int teamId, out int laneId, out Transform spawnPoint)
        {
            spawnPointIndex = -1;
            teamId = 0;
            laneId = 0;
            spawnPoint = null;

            if (runner == null)
                return false;

            _playerSpawner ??= new NetworkPlayerSpawner(playerPrefab);

            var playerSlotIndex = _playerSpawner.ResolvePlayerSlotIndex(runner, player);
            if (playerSlotIndex < 0)
                return false;

            var mode = TeamLaneAssignmentUtility.ResolveMode(MatchModeExtensions.ToGamePlayerCount(runner.SessionInfo.MaxPlayers));
            var assignment = TeamLaneAssignmentUtility.ResolveAssignment(mode, playerSlotIndex);
            var layoutIndex = TeamLaneAssignmentUtility.ResolveSpawnLayoutIndex(mode, assignment.TeamId, assignment.LaneId);
            if (layoutIndex < 0)
                return false;

            var configuredSpawnPoints = ResolveSpawnPointLayout(mode);
            if (configuredSpawnPoints == null || layoutIndex >= configuredSpawnPoints.Length)
                return false;

            spawnPointIndex = layoutIndex;
            teamId = assignment.TeamId;
            laneId = assignment.LaneId;
            spawnPoint = configuredSpawnPoints[layoutIndex];
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

                _playerSpawner ??= new NetworkPlayerSpawner(playerPrefab);
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
                    && runner.ActivePlayers.Count() < ResolveRequiredPlayersForActiveMode(runner))
                {
                    _gameOverManager.TriggerForfeit("Opponent disconnected");
                }

                if (runner.ActivePlayers.Count() == 0)
                {
                    ResetToWaitingState(runner, matchSessionState);
                }
                else if (matchSessionState != null && !matchSessionState.MatchInProgress
                         && !matchSessionState.IsPostGameCleanup
                         && runner.ActivePlayers.Count() < ResolveRequiredPlayersForActiveMode(runner))
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
            RefreshLaneVisibilityForCurrentMode(runner);

            if (!runner.IsServer) return;

            var matchSessionState = runner.GetComponent<MatchSessionState>();

            if (!IsGameSceneActive())
            {
                PrepareForLobbyState();

                if (matchSessionState != null && !matchSessionState.IsPostGameCleanup
                    && runner.ActivePlayers.Count() < ResolveRequiredPlayersForActiveMode(runner))
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

            _playerSpawner ??= new NetworkPlayerSpawner(playerPrefab);

            foreach (var player in runner.ActivePlayers)
            {
                SpawnMissingPlayer(runner, player);
            }

            EnsureGameplayManagers(runner);

            if (runner.ActivePlayers.Count() < ResolveRequiredPlayersForActiveMode(runner)
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

            ball.OnLeftGoal += _scoreManager.RegisterRightGoal;
            ball.OnRightGoal += _scoreManager.RegisterLeftGoal;
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
                _boundBall.OnLeftGoal -= _scoreManager.RegisterRightGoal;
                _boundBall.OnRightGoal -= _scoreManager.RegisterLeftGoal;
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

        private static int ResolveRequiredPlayersForActiveMode(NetworkRunner runner)
        {
            if (runner == null)
                return 1;

            var requiredPlayers = MatchModeExtensions.ToGamePlayerCount(runner.SessionInfo.MaxPlayers);
            return Mathf.Max(1, requiredPlayers);
        }

        private bool TryGetSpawnPoint(MatchMode mode, int index, out Transform spawnPoint)
        {
            spawnPoint = null;

            var spawnPoints = ResolveSpawnPointLayout(mode);
            if (spawnPoints == null || spawnPoints.Length == 0)
                return false;

            int normalizedIndex = ((index % spawnPoints.Length) + spawnPoints.Length) % spawnPoints.Length;
            spawnPoint = spawnPoints[normalizedIndex];
            return spawnPoint != null;
        }

        private Transform[] ResolveSpawnPointLayout(MatchMode mode)
        {
            if (mode == MatchMode.TwoVsTwo)
                return twoVsTwoSpawnPoints != null && twoVsTwoSpawnPoints.Length >= MatchModeExtensions.TwoVsTwoMaxPlayers
                    ? twoVsTwoSpawnPoints
                    : null;

            return oneVsOneSpawnPoints != null && oneVsOneSpawnPoints.Length >= 2
                ? oneVsOneSpawnPoints
                : null;
        }

        private void RefreshLaneVisibilityForCurrentMode(NetworkRunner runner)
        {
            if (runner == null || !IsGameSceneActive())
                return;

            var mode = TeamLaneAssignmentUtility.ResolveMode(MatchModeExtensions.ToGamePlayerCount(runner.SessionInfo.MaxPlayers));
            ApplyLaneVisibility(mode);
        }

        private void ApplyLaneVisibility(MatchMode mode)
        {
            var activeLayout = ResolveSpawnPointLayout(mode);
            if (activeLayout == null || activeLayout.Length == 0)
                return;

            var activeSpawnPoints = new HashSet<Transform>();
            foreach (var spawnPoint in activeLayout)
            {
                if (spawnPoint != null)
                    activeSpawnPoints.Add(spawnPoint);
            }

            var processedRenderers = new HashSet<SpriteRenderer>();
            foreach (var spawnPoint in EnumerateConfiguredSpawnPoints())
            {
                if (spawnPoint == null)
                    continue;

                var laneRenderer = spawnPoint.GetComponent<SpriteRenderer>();
                if (laneRenderer == null || !processedRenderers.Add(laneRenderer))
                    continue;

                laneRenderer.enabled = activeSpawnPoints.Contains(spawnPoint);
            }
        }

        private IEnumerable<Transform> EnumerateConfiguredSpawnPoints()
        {
            if (oneVsOneSpawnPoints != null)
            {
                foreach (var spawnPoint in oneVsOneSpawnPoints)
                    yield return spawnPoint;
            }

            if (twoVsTwoSpawnPoints != null)
            {
                foreach (var spawnPoint in twoVsTwoSpawnPoints)
                    yield return spawnPoint;
            }
        }

        private MatchMode ResolveCurrentMode()
        {
            if (_networkRunner != null)
                return TeamLaneAssignmentUtility.ResolveMode(MatchModeExtensions.ToGamePlayerCount(_networkRunner.SessionInfo.MaxPlayers));

            return twoVsTwoSpawnPoints != null && twoVsTwoSpawnPoints.Length >= MatchModeExtensions.TwoVsTwoMaxPlayers
                ? MatchMode.TwoVsTwo
                : MatchMode.OneVsOne;
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
