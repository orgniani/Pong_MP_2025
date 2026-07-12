using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Config;
using Fusion;
using UI;
using UnityEngine;

namespace Managers.Network
{
    public sealed class LobbySessionState : MonoBehaviour
    {
        private const string UsernameTokenPrefix = "lobby-username:";
        private const string FallbackUsername = "Player";
        private static readonly LobbySessionSnapshot EmptySnapshot = new(Array.Empty<string>(), Array.Empty<int>(), Array.Empty<bool>(), Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>(), -1, false, 0, 0);
        private static LobbySessionState _activeInstance;
        private static int _activationSequence;
        private NetworkRunner _runner;
        private int _activationOrder;
        private LobbyRosterState _rosterState;

        private readonly Dictionary<PlayerRef, string> _waitingUsernames = new();
        private readonly Dictionary<PlayerRef, bool> _readyPlayers = new();
        private readonly Dictionary<PlayerRef, int> _colorIds = new();
        private PaddleColorPalette _paddleColorPalette;

        public event Action<LobbySessionSnapshot> SnapshotChanged;

        public static LobbySessionState ActiveInstance => ResolvePreferredInstance();

        public LobbySessionSnapshot CurrentSnapshot =>
            _rosterState != null ? _rosterState.BuildSnapshot() : EmptySnapshot;

        public NetworkRunner Runner => _runner != null ? _runner : (_runner = GetComponent<NetworkRunner>());

        public static LobbySessionState EnsureOnRunner(NetworkRunner runner)
        {
            if (runner == null)
                return null;

            var state = runner.GetComponent<LobbySessionState>() ?? runner.gameObject.AddComponent<LobbySessionState>();
            state.MarkAsActive();
            return state;
        }

        public void Configure(PaddleColorPalette paddleColorPalette)
        {
            _paddleColorPalette = paddleColorPalette;
        }

        public static LobbySessionState FindForRunner(NetworkRunner runner)
        {
            if (runner == null)
                return null;

            return FindObjectsByType<LobbySessionState>(FindObjectsSortMode.InstanceID)
                .Where(state => IsUsable(state) && state.Runner == runner)
                .OrderByDescending(state => state.Runner.IsRunning)
                .ThenByDescending(state => state._activationOrder)
                .FirstOrDefault();
        }

        private void Awake()
        {
            MarkAsActive();
        }

        private void OnEnable()
        {
            MarkAsActive();
            LobbyRosterState.ActiveInstanceChanged += RefreshRosterBinding;
            RefreshRosterBinding(LobbyRosterState.ActiveInstance);
        }

        private void OnDisable()
        {
            LobbyRosterState.ActiveInstanceChanged -= RefreshRosterBinding;
            UnbindRosterState();
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(_activeInstance, this))
                _activeInstance = null;

            LobbyRosterState.ActiveInstanceChanged -= RefreshRosterBinding;
            UnbindRosterState();
        }

        public static byte[] CreateConnectionToken(string username)
        {
            return Encoding.UTF8.GetBytes(UsernameTokenPrefix + NormalizeUsername(username));
        }

        public void HandlePlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null || !runner.IsServer)
                return;

            _waitingUsernames[player] = ResolveUsernameFromToken(runner.GetPlayerConnectionToken(player), player);
            _readyPlayers[player] = false;

            if (!TryAssignRandomAvailableColor(runner, player))
            {
                _waitingUsernames.Remove(player);
                _readyPlayers.Remove(player);
                _colorIds.Remove(player);
                runner.Disconnect(player, null);
                return;
            }

            PublishRoster(runner);
        }

        public void HandlePlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null || !runner.IsServer)
                return;

            _waitingUsernames.Remove(player);
            _readyPlayers.Remove(player);
            _colorIds.Remove(player);
            PublishRoster(runner);
            DedicatedServerMatchFlow.RequestMatchStartEvaluation(runner);
        }

        public void RefreshFromRunner(NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer)
                return;

            SynchronizeServerRoster(runner);
            PublishRoster(runner);
        }

        public void EnterLobby(NetworkRunner runner, NetworkPrefabRef lobbyRosterStatePrefab)
        {
            if (runner == null)
                return;

            _runner = runner;
            MarkAsActive();

            var spawnedFreshLobbyRoster = _runner.IsServer && EnsureRosterSpawned(lobbyRosterStatePrefab);

            if (spawnedFreshLobbyRoster)
            {
                _waitingUsernames.Clear();
                _readyPlayers.Clear();
                _colorIds.Clear();
            }

            RefreshRosterBinding(LobbyRosterState.ActiveInstance);

            if (_runner.IsServer)
                RefreshFromRunner(_runner);
            else if (_rosterState == null)
                SnapshotChanged?.Invoke(CurrentSnapshot);
        }

        public void ResetState()
        {
            _waitingUsernames.Clear();
            _readyPlayers.Clear();
            _colorIds.Clear();
            var rosterState = _rosterState;
            UnbindRosterState();

            SnapshotChanged?.Invoke(EmptySnapshot);

            if (_runner != null && _runner.IsServer)
                rosterState?.SetRoster(Array.Empty<string>(), Array.Empty<int>(), Array.Empty<bool>(), Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>(), 0, 0);
        }

        public void RequestLocalPlayerReadyLock()
        {
            _rosterState?.RequestLocalPlayerReadyLock();
        }

        public void RequestLocalPlayerColorChange(int colorId)
        {
            _rosterState?.RequestLocalPlayerColorChange(colorId);
        }

        public bool TryLockReady(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null || !runner.IsServer || !player.IsRealPlayer || !_waitingUsernames.ContainsKey(player))
                return false;

            if (_readyPlayers.TryGetValue(player, out var alreadyReady) && alreadyReady)
                return false;

            _readyPlayers[player] = true;
            PublishRoster(runner);
            DedicatedServerMatchFlow.RequestMatchStartEvaluation(runner);
            return true;
        }

        public bool TrySetColor(NetworkRunner runner, PlayerRef player, int colorId)
        {
            if (runner == null || !runner.IsServer || !player.IsRealPlayer || !_waitingUsernames.ContainsKey(player))
                return false;

            if (_paddleColorPalette == null || !_paddleColorPalette.IsValidColorId(colorId))
                return false;

            foreach (var entry in _colorIds)
            {
                if (entry.Key != player && entry.Value == colorId)
                    return false;
            }

            _colorIds[player] = colorId;
            PublishRoster(runner);
            return true;
        }

        public bool TryGetColorId(PlayerRef player, out int colorId)
        {
            return _colorIds.TryGetValue(player, out colorId);
        }

        public bool AreAllActivePlayersReady(NetworkRunner runner, int minPlayersToStart)
        {
            if (runner == null || !runner.IsServer)
                return false;

            var activePlayers = runner.ActivePlayers.ToArray();
            if (activePlayers.Length < Math.Max(1, minPlayersToStart))
                return false;

            return activePlayers.All(player => _readyPlayers.TryGetValue(player, out var isReady) && isReady);
        }

        private void MarkAsActive()
        {
            if (Runner == null)
                return;

            _activationOrder = ++_activationSequence;
            _activeInstance = this;
        }

        private static bool IsUsable(LobbySessionState state)
        {
            return state != null && state.Runner != null;
        }

        private static LobbySessionState ResolvePreferredInstance()
        {
            var resolved = FindObjectsByType<LobbySessionState>(FindObjectsSortMode.InstanceID)
                .Where(IsUsable)
                .OrderByDescending(state => state.Runner.IsRunning)
                .ThenByDescending(state => ReferenceEquals(state, _activeInstance))
                .ThenByDescending(state => state._activationOrder)
                .FirstOrDefault();

            if (resolved != null && !ReferenceEquals(_activeInstance, resolved))
                resolved.MarkAsActive();

            return resolved;
        }

        private bool EnsureRosterSpawned(NetworkPrefabRef lobbyRosterStatePrefab)
        {
            if (_runner == null || !_runner.IsServer || LobbyRosterState.FindForRunner(_runner) != null)
                return false;

            if (!lobbyRosterStatePrefab.IsValid)
            {
                Debug.LogError("[LobbySessionState] Lobby roster spawn failed because the lobby roster prefab is missing or invalid. Assign it directly on LobbySceneCompositionRoot in the Lobby scene.", this);
                return false;
            }

            _runner.Spawn(lobbyRosterStatePrefab);
            return true;
        }

        private void RefreshRosterBinding(LobbyRosterState _)
        {
            var rosterState = ResolveRosterStateForRunner();

            if (ReferenceEquals(_rosterState, rosterState))
            {
                if (_rosterState == null)
                    SnapshotChanged?.Invoke(CurrentSnapshot);

                return;
            }

            UnbindRosterState();
            _rosterState = rosterState;

            if (_rosterState == null)
            {
                SnapshotChanged?.Invoke(CurrentSnapshot);
                return;
            }

            _rosterState.SnapshotChanged += HandleRosterSnapshotChanged;
            HandleRosterSnapshotChanged(_rosterState.BuildSnapshot());

            if (_runner != null && _runner.IsServer)
                PublishRoster(_runner);
        }

        private void UnbindRosterState()
        {
            if (_rosterState == null)
                return;

            _rosterState.SnapshotChanged -= HandleRosterSnapshotChanged;
            _rosterState = null;
        }

        private LobbyRosterState ResolveRosterStateForRunner()
        {
            var runner = Runner;
            return runner != null ? LobbyRosterState.FindForRunner(runner) : null;
        }

        private void SynchronizeServerRoster(NetworkRunner runner)
        {
            var synchronizedReadyPlayers = new Dictionary<PlayerRef, bool>();

            _waitingUsernames.Clear();
            foreach (var player in runner.ActivePlayers.OrderBy(activePlayer => activePlayer.PlayerId))
            {
                _waitingUsernames[player] = ResolveUsernameFromToken(runner.GetPlayerConnectionToken(player), player);
                synchronizedReadyPlayers[player] = _readyPlayers.TryGetValue(player, out var isReady) && isReady;
            }

            _readyPlayers.Clear();
            foreach (var entry in synchronizedReadyPlayers)
                _readyPlayers[entry.Key] = entry.Value;

            SynchronizeColorAssignments(runner);
        }

        private void PublishRoster(NetworkRunner runner)
        {
            if (_rosterState == null)
                return;

            var orderedEntries = _waitingUsernames
                .OrderBy(entry => entry.Key.PlayerId)
                .ToArray();

            var orderedUsernames = orderedEntries
                .Select(entry => NormalizeUsername(entry.Value))
                .ToArray();
            var orderedPlayerIds = orderedEntries
                .Select(entry => entry.Key.PlayerId)
                .ToArray();
            var orderedReadyStates = orderedEntries
                .Select(entry => _readyPlayers.TryGetValue(entry.Key, out var isReady) && isReady)
                .ToArray();
            var targetPlayerCapacity = ResolveTargetPlayerCapacity(runner);
            var mode = TeamLaneAssignmentUtility.ResolveMode(targetPlayerCapacity);
            var orderedAssignments = orderedEntries
                .Select((entry, index) => TeamLaneAssignmentUtility.ResolveAssignment(mode, index))
                .ToArray();
            var orderedTeamIds = orderedAssignments
                .Select(assignment => assignment.TeamId)
                .ToArray();
            var orderedLaneIds = orderedAssignments
                .Select(assignment => assignment.LaneId)
                .ToArray();
            var orderedColorIds = orderedEntries
                .Select(entry => _colorIds.TryGetValue(entry.Key, out var colorId) ? colorId : -1)
                .ToArray();

            _rosterState.SetRoster(orderedUsernames, orderedPlayerIds, orderedReadyStates, orderedTeamIds, orderedLaneIds, orderedColorIds, runner.ActivePlayers.Count(), targetPlayerCapacity);
        }

        private void SynchronizeColorAssignments(NetworkRunner runner)
        {
            var activePlayers = runner.ActivePlayers
                .OrderBy(player => player.PlayerId)
                .ToArray();

            var activePlayerSet = new HashSet<PlayerRef>(activePlayers);
            var stalePlayers = _colorIds.Keys
                .Where(player => !activePlayerSet.Contains(player))
                .ToArray();

            foreach (var player in stalePlayers)
                _colorIds.Remove(player);

            var claimedColorIds = new HashSet<int>();
            foreach (var player in activePlayers)
            {
                if (_colorIds.TryGetValue(player, out var colorId)
                    && IsColorClaimValidForPlayer(player, colorId)
                    && claimedColorIds.Add(colorId))
                {
                    continue;
                }

                _colorIds.Remove(player);
            }

            foreach (var player in activePlayers)
            {
                if (_colorIds.ContainsKey(player))
                    continue;

                if (TryAssignRandomAvailableColor(runner, player))
                    continue;

                runner.Disconnect(player, null);
            }
        }

        private bool TryAssignRandomAvailableColor(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null || !runner.IsServer || !_waitingUsernames.ContainsKey(player))
                return false;

            if (_paddleColorPalette == null)
                return false;

            var availableColorIds = GetAvailableColorIds(_paddleColorPalette, player);
            if (availableColorIds.Count == 0)
                return false;

            var selectedIndex = UnityEngine.Random.Range(0, availableColorIds.Count);
            _colorIds[player] = availableColorIds[selectedIndex];
            return true;
        }

        private List<int> GetAvailableColorIds(PaddleColorPalette palette, PlayerRef player)
        {
            var claimedColorIds = new HashSet<int>(_colorIds
                .Where(entry => entry.Key != player)
                .Select(entry => entry.Value));
            var availableColorIds = new List<int>(palette.Count);

            for (var colorId = 0; colorId < palette.Count; colorId++)
            {
                if (!claimedColorIds.Contains(colorId))
                    availableColorIds.Add(colorId);
            }

            return availableColorIds;
        }

        private bool IsColorClaimValidForPlayer(PlayerRef player, int colorId)
        {
            if (!player.IsRealPlayer)
                return false;

            return _paddleColorPalette != null && _paddleColorPalette.IsValidColorId(colorId);
        }

        private void HandleRosterSnapshotChanged(LobbySessionSnapshot snapshot)
        {
            SnapshotChanged?.Invoke(snapshot);
        }

        private static int ResolveTargetPlayerCapacity(NetworkRunner runner)
        {
            if (runner == null)
                return 0;

            return UIGameModeFilterExtensions.ToGamePlayerCount(runner.SessionInfo.MaxPlayers);
        }

        private static string ResolveUsernameFromToken(byte[] token, PlayerRef player)
        {
            if (token == null || token.Length == 0)
                return CreateFallbackPlayerName(player);

            var rawValue = Encoding.UTF8.GetString(token);
            if (rawValue.StartsWith(UsernameTokenPrefix, StringComparison.Ordinal))
                rawValue = rawValue.Substring(UsernameTokenPrefix.Length);

            var normalized = NormalizeUsername(rawValue);
            return string.IsNullOrEmpty(normalized) ? CreateFallbackPlayerName(player) : normalized;
        }

        private static string NormalizeUsername(string username)
        {
            return string.IsNullOrWhiteSpace(username) ? FallbackUsername : username.Trim();
        }

        private static string CreateFallbackPlayerName(PlayerRef player)
        {
            return $"{FallbackUsername}_{player.PlayerId}";
        }
    }
}
