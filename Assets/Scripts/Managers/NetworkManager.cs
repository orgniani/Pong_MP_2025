using System;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using Common;
using Config;
using Lobby.SessionSnapshot;
using Network;
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
        [SerializeField] private Ball ball;

        [Header("Prefabs")]
        [SerializeField] private NetworkPrefabRef playerPrefab;
        [SerializeField] private NetworkPrefabRef timerManagerPrefab;
        [SerializeField] private NetworkPrefabRef scoreManagerPrefab;
        [SerializeField] private NetworkPrefabRef gameOverManagerPrefab;

        private ScoreManager _scoreManager;
        private TimerManager _timerManager;
        private GameOverManager _gameOverManager;
        
        private NetworkRunner _networkRunner;
        private NetworkPlayerSpawner _playerSpawner;
        private bool _ballBound;

        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action OnRosterChanged;

        public NetworkPlayerSpawner PlayerSpawner => _playerSpawner;
        public GameOverManager GameOverManager => _gameOverManager;
        public bool IsServer => _networkRunner != null && _networkRunner.IsServer;

        private void Awake()
        {
            ReferenceValidator.ValidateOptional(oneVsOneSpawnPoints, nameof(oneVsOneSpawnPoints), this);
            ReferenceValidator.ValidateOptional(twoVsTwoSpawnPoints, nameof(twoVsTwoSpawnPoints), this);
            ReferenceValidator.Validate(ball, nameof(ball), this);

            var runner = FindFirstObjectByType<NetworkRunner>();
            if (runner != null)
                BindRunner(runner);
        }

        private void OnDestroy()
        {
            _networkRunner?.RemoveCallbacks(this);
        }

        void OnApplicationQuit ()
        {
            if (_networkRunner)
                _networkRunner.Shutdown();

            _networkRunner = null;
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

            _networkRunner = null;

            if (runner != null && runner.gameObject != null)
                Destroy(runner.gameObject);

            OnDisconnected?.Invoke();
        }

        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer)
            {
                OnRosterChanged?.Invoke();
                return;
            }

            var matchSessionState = runner.GetComponent<MatchSessionState>();
            if (matchSessionState != null && matchSessionState.IsPostGameCleanup)
            {
                runner.Disconnect(player, null);
                return;
            }

            _playerSpawner ??= new NetworkPlayerSpawner(playerPrefab);
            if (IsGameSceneActive() && matchSessionState?.MatchInProgress == true)
            {
                SpawnMissingPlayer(runner, player);
                EnsureGameplayManagers(runner);
            }

            OnRosterChanged?.Invoke();
        }

        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer)
            {
                OnRosterChanged?.Invoke();
                return;
            }

            if (_playerSpawner != null && _playerSpawner.IsSpawned(player))
                _playerSpawner.DespawnPlayer(runner, player);

            var matchSessionState = runner.GetComponent<MatchSessionState>();
            var activePlayerCount = runner.ActivePlayers.Count();
            var requiredPlayers = ResolveRequiredPlayersForActiveMode(runner);

            if (matchSessionState != null && matchSessionState.MatchInProgress
                && _gameOverManager != null && !_gameOverManager.IsGameOver
                && activePlayerCount < requiredPlayers)
            {
                _gameOverManager.TriggerForfeit(GameOverReason.PlayerDisconnected);
            }

            if (activePlayerCount == 0)
            {
                ResetToWaitingState(runner, matchSessionState);
                OnRosterChanged?.Invoke();
                return;
            }

            if (matchSessionState != null && !matchSessionState.MatchInProgress
                && !matchSessionState.IsPostGameCleanup
                && activePlayerCount < requiredPlayers)
            {
                matchSessionState.RearmAutoStart();
            }

            OnRosterChanged?.Invoke();
        }

        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
        {
            if (_networkRunner.IsClient)
                OnConnected?.Invoke();
        }

        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.LogWarning($"{LogPrefix} disconnect reason: mode={runner.GameMode}, reason={reason}, session='{runner.SessionInfo.Name}'");

            if (_networkRunner == null || !_networkRunner.IsClient)
                return;

            if (_gameOverManager != null && _gameOverManager.IsGameOver)
            {
                Shutdown();
                return;
            }

            DisconnectNotice.MarkUnexpected();
            SessionExitToMainMenu.Execute(LogPrefix);
        }

        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
        {
            if (!runner.IsClient || !runner.IsPlayerValid(runner.LocalPlayer))
                return;

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

            if (!runner.IsServer)
                return;

            var matchSessionState = runner.GetComponent<MatchSessionState>();
            var activePlayerCount = runner.ActivePlayers.Count();
            var requiredPlayers = ResolveRequiredPlayersForActiveMode(runner);

            if (!IsGameSceneActive())
            {
                PrepareForLobbyState();

                if (matchSessionState != null && !matchSessionState.IsPostGameCleanup && activePlayerCount < requiredPlayers)
                    matchSessionState.RearmAutoStart();

                return;
            }

            if (activePlayerCount == 0)
            {
                ResetToWaitingState(runner, matchSessionState);
                return;
            }

            if (matchSessionState?.MatchInProgress != true)
                return;

            _playerSpawner ??= new NetworkPlayerSpawner(playerPrefab);

            foreach (var player in runner.ActivePlayers)
                SpawnMissingPlayer(runner, player);

            EnsureGameplayManagers(runner);

            if (activePlayerCount < requiredPlayers && _gameOverManager != null && !_gameOverManager.IsGameOver)
                _gameOverManager.TriggerForfeit(GameOverReason.PlayerDisconnected);
        }
        void INetworkRunnerCallbacks.OnSceneLoadStart (NetworkRunner runner) { }
        void INetworkRunnerCallbacks.OnObjectExitAOI (NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnObjectEnterAOI (NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnReliableDataReceived (NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        void INetworkRunnerCallbacks.OnReliableDataProgress (NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

        private void TryBindBallGoalCallbacks()
        {
            if (_scoreManager == null || ball == null || _ballBound)
                return;

            ball.OnLeftGoal += _scoreManager.RegisterRightGoal;
            ball.OnRightGoal += _scoreManager.RegisterLeftGoal;
            _ballBound = true;
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

            foreach (var player in runner.ActivePlayers.ToArray())
            {
                runner.Disconnect(player, null);
            }
        }

        private void ClearBallGoalCallbacks()
        {
            if (_ballBound && ball != null && _scoreManager != null)
            {
                ball.OnLeftGoal -= _scoreManager.RegisterRightGoal;
                ball.OnRightGoal -= _scoreManager.RegisterLeftGoal;
            }

            _ballBound = false;
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

        private static bool IsGameSceneActive()
        {
            return SceneManager.GetActiveScene().buildIndex == SceneCatalog.GetGameIndex();
        }
    }
}
