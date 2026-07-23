using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Common.Teams;
using Config;
using Fusion;
using Lobby.SessionSnapshot;
using Network;
using UnityEngine;

namespace Lobby
{
    public sealed class LobbySessionState : MonoBehaviour
    {
        private static LobbySessionState _activeInstance;
        private static int _activationSequence;
        private NetworkRunner _runner;
        private int _activationOrder;
        private LobbyRosterState _rosterState;

        private readonly Dictionary<PlayerRef, string> _lobbyMemberUsernames = new();
        private readonly Dictionary<PlayerRef, bool> _readyPlayers = new();
        private readonly Dictionary<PlayerRef, int> _colorIds = new();
        private readonly HashSet<PlayerRef> _playersPendingDisconnect = new();
        private PaddleColorPalette _paddleColorPalette;
        private LobbyColorAssignmentCoordinator _colorAssignmentCoordinator;

        public event Action<LobbySessionSnapshot> SnapshotChanged;

        public static LobbySessionState ActiveInstance => ResolvePreferredInstance();

        public LobbySessionSnapshot CurrentSnapshot =>
            _rosterState != null ? _rosterState.BuildSnapshot() : LobbySessionSnapshot.Empty;

        public NetworkRunner Runner => _runner != null ? _runner : (_runner = GetComponent<NetworkRunner>());

        private LobbyColorAssignmentCoordinator ColorAssignmentCoordinator =>
            _colorAssignmentCoordinator ??= new LobbyColorAssignmentCoordinator(_colorIds, IsLobbyMember, () => _paddleColorPalette);

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
                .Where(state => state != null && state.Runner == runner)
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

        public void HandlePlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null || !runner.IsServer)
                return;

            RegisterLobbyMember(runner, player);

            if (!ColorAssignmentCoordinator.TryAssignColorForLobbyMember(runner, player))
            {
                DisconnectLobbyMember(runner, player, removeLobbyStateImmediately: true);
                return;
            }

            PublishRoster(runner);
        }

        public void HandlePlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null || !runner.IsServer)
                return;

            RemoveLobbyMember(player);
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
                ClearServerState();

            RefreshRosterBinding(LobbyRosterState.ActiveInstance);

            if (_runner.IsServer)
            {
                RefreshFromRunner(_runner);
                return;
            }

            if (_rosterState == null)
                SnapshotChanged?.Invoke(CurrentSnapshot);
        }

        public void ResetState()
        {
            ClearServerState();
            var rosterState = _rosterState;
            UnbindRosterState();

            SnapshotChanged?.Invoke(LobbySessionSnapshot.Empty);

            if (_runner != null && _runner.IsServer)
                rosterState?.SetRoster(LobbyRosterUpdate.Empty);
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
            if (runner == null || !runner.IsServer || !player.IsRealPlayer || !IsLobbyMember(player))
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
            if (!ColorAssignmentCoordinator.TrySetColor(runner, player, colorId))
                return false;

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

            var activePlayers = GetStartEligiblePlayers(runner);
            if (activePlayers.Length < Math.Max(1, minPlayersToStart))
                return false;

            return activePlayers.All(player => _readyPlayers.TryGetValue(player, out var isReady) && isReady);
        }

        public int CountStartEligiblePlayers(NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer)
                return 0;

            return GetStartEligiblePlayers(runner).Length;
        }

        private void MarkAsActive()
        {
            if (Runner == null)
                return;

            _activationOrder = ++_activationSequence;
            _activeInstance = this;
        }

        private static LobbySessionState ResolvePreferredInstance()
        {
            var resolved = FindObjectsByType<LobbySessionState>(FindObjectsSortMode.InstanceID)
                .Where(state => state != null && state.Runner != null)
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
            var bindingChanged = !ReferenceEquals(_rosterState, rosterState);
            if (bindingChanged)
            {
                UnbindRosterState();
                _rosterState = rosterState;

                if (_rosterState != null)
                    _rosterState.SnapshotChanged += HandleRosterSnapshotChanged;
            }

            if (_rosterState == null)
            {
                SnapshotChanged?.Invoke(CurrentSnapshot);
                return;
            }

            if (bindingChanged)
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
            var activePlayers = runner.ActivePlayers
                .OrderBy(player => player.PlayerId)
                .ToArray();
            var activePlayerSet = new HashSet<PlayerRef>(activePlayers);

            _lobbyMemberUsernames.Clear();

            var staleReadyPlayers = _readyPlayers.Keys
                .Where(player => !activePlayerSet.Contains(player))
                .ToArray();
            foreach (var player in staleReadyPlayers)
                _readyPlayers.Remove(player);

            foreach (var player in activePlayers)
            {
                RegisterLobbyMember(runner, player);
            }

            RequestPendingColorDisconnects(runner, ColorAssignmentCoordinator.SynchronizeColorAssignments(runner));
        }

        private void RegisterLobbyMember(NetworkRunner runner, PlayerRef player)
        {
            _playersPendingDisconnect.Remove(player);
            _lobbyMemberUsernames[player] = LobbyUsernameTokenUtility.ResolveUsernameFromToken(runner.GetPlayerConnectionToken(player), player);

            if (!_readyPlayers.ContainsKey(player))
                _readyPlayers[player] = false;
        }

        private void RemoveLobbyMember(PlayerRef player)
        {
            _playersPendingDisconnect.Remove(player);
            _lobbyMemberUsernames.Remove(player);
            _readyPlayers.Remove(player);
            _colorIds.Remove(player);
        }

        private void RequestPendingColorDisconnects(NetworkRunner runner, LobbyColorSynchronizationResult synchronizationResult)
        {
            if (!synchronizationResult.HasPlayersPendingDisconnect)
                return;

            foreach (var player in synchronizationResult.PlayersPendingDisconnect)
            {
                _playersPendingDisconnect.Add(player);
                DisconnectLobbyMember(runner, player, removeLobbyStateImmediately: false);
            }
        }

        private void DisconnectLobbyMember(NetworkRunner runner, PlayerRef player, bool removeLobbyStateImmediately)
        {
            if (runner == null || !runner.IsServer || !player.IsRealPlayer)
                return;

            if (removeLobbyStateImmediately)
                RemoveLobbyMember(player);

            runner.Disconnect(player, null);
        }

        private bool IsLobbyMember(PlayerRef player)
        {
            return _lobbyMemberUsernames.ContainsKey(player);
        }

        private PlayerRef[] GetStartEligiblePlayers(NetworkRunner runner)
        {
            if (runner == null)
                return Array.Empty<PlayerRef>();

            return runner.ActivePlayers
                .Where(player => !_playersPendingDisconnect.Contains(player))
                .ToArray();
        }

        private void PublishRoster(NetworkRunner runner)
        {
            if (_rosterState == null)
                return;

            var orderedEntries = _lobbyMemberUsernames
                .Where(entry => !_playersPendingDisconnect.Contains(entry.Key))
                .OrderBy(entry => entry.Key.PlayerId);
            var targetPlayerCapacity = ResolveTargetPlayerCapacity(runner);
            var mode = TeamLaneAssignmentUtility.ResolveMode(targetPlayerCapacity);
            var orderedUsernames = new List<string>();
            var orderedPlayerIds = new List<int>();
            var orderedReadyStates = new List<bool>();
            var orderedTeamIds = new List<int>();
            var orderedLaneIds = new List<int>();
            var orderedColorIds = new List<int>();
            var slotIndex = 0;

            foreach (var entry in orderedEntries)
            {
                var assignment = TeamLaneAssignmentUtility.ResolveAssignment(mode, slotIndex);
                orderedUsernames.Add(LobbyUsernameTokenUtility.NormalizeUsername(entry.Value));
                orderedPlayerIds.Add(entry.Key.PlayerId);
                orderedReadyStates.Add(_readyPlayers.TryGetValue(entry.Key, out var isReady) && isReady);
                orderedTeamIds.Add(assignment.TeamId);
                orderedLaneIds.Add(assignment.LaneId);
                orderedColorIds.Add(_colorIds.TryGetValue(entry.Key, out var colorId) ? colorId : -1);
                slotIndex++;
            }

            _rosterState.SetRoster(new LobbyRosterUpdate(
                usernames: orderedUsernames,
                playerIds: orderedPlayerIds,
                readyStates: orderedReadyStates,
                teamIds: orderedTeamIds,
                laneIds: orderedLaneIds,
                colorIds: orderedColorIds,
                currentPlayerCount: orderedPlayerIds.Count,
                targetPlayerCapacity: targetPlayerCapacity));
        }

        private void ClearServerState()
        {
            _playersPendingDisconnect.Clear();
            _lobbyMemberUsernames.Clear();
            _readyPlayers.Clear();
            _colorIds.Clear();
        }

        private void HandleRosterSnapshotChanged(LobbySessionSnapshot snapshot)
        {
            SnapshotChanged?.Invoke(snapshot);
        }

        private static int ResolveTargetPlayerCapacity(NetworkRunner runner)
        {
            if (runner == null)
                return 0;

            return MatchModeExtensions.ToGamePlayerCount(runner.SessionInfo.MaxPlayers);
        }

    }
}
