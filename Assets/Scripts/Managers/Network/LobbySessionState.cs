using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fusion;
using UI;
using UnityEngine;

namespace Managers.Network
{
    public sealed class LobbySessionState : MonoBehaviour
    {
        private const string UsernameTokenPrefix = "lobby-username:";
        private const string FallbackUsername = "Player";
        private static LobbySessionState _activeInstance;
        private static int _activationSequence;
        private NetworkRunner _runner;
        private int _activationOrder;
        private LobbyRosterState _rosterState;

        private readonly Dictionary<PlayerRef, string> _waitingUsernames = new();

        public event Action<LobbySessionSnapshot> SnapshotChanged;

        public LobbySessionSnapshot CurrentSnapshot =>
            _rosterState != null ? _rosterState.BuildSnapshot() : new LobbySessionSnapshot(Array.Empty<string>(), 0, 0);

        public NetworkRunner Runner => _runner != null ? _runner : (_runner = GetComponent<NetworkRunner>());

        public static LobbySessionState EnsureOnRunner(NetworkRunner runner)
        {
            if (runner == null)
                return null;

            var state = runner.GetComponent<LobbySessionState>() ?? runner.gameObject.AddComponent<LobbySessionState>();
            state.MarkAsActive();
            return state;
        }

        public static LobbySessionState FindRunnerOwnedInstance()
        {
            if (IsUsable(_activeInstance))
                return _activeInstance;

            var resolved = FindObjectsByType<LobbySessionState>(FindObjectsSortMode.InstanceID)
                .Where(IsUsable)
                .OrderByDescending(state => state.Runner.IsRunning)
                .ThenByDescending(state => state._activationOrder)
                .FirstOrDefault();

            if (resolved != null)
                resolved.MarkAsActive();

            return resolved;
        }

        private void Awake()
        {
            MarkAsActive();
        }

        private void OnEnable()
        {
            MarkAsActive();
        }

        private void Update()
        {
            if (_rosterState != null)
                return;

            var resolved = LobbyRosterState.ActiveInstance;
            if (resolved == null)
                return;

            _rosterState = resolved;
            _rosterState.SnapshotChanged += HandleRosterSnapshotChanged;
            HandleRosterSnapshotChanged(_rosterState.BuildSnapshot());
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(_activeInstance, this))
                _activeInstance = null;

            if (_rosterState != null)
                _rosterState.SnapshotChanged -= HandleRosterSnapshotChanged;
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
            PublishRoster(runner);
        }

        public void HandlePlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null || !runner.IsServer)
                return;

            _waitingUsernames.Remove(player);
            PublishRoster(runner);
        }

        public void RefreshFromRunner(NetworkRunner runner)
        {
            if (runner == null || !runner.IsServer)
                return;

            SynchronizeServerRoster(runner);
            PublishRoster(runner);
        }

        public void PublishAuthoritativeSnapshot(NetworkRunner runner)
        {
            RefreshFromRunner(runner);
        }

        public void ResetState()
        {
            _waitingUsernames.Clear();

            if (_runner != null && _runner.IsServer)
                LobbyRosterState.ActiveInstance?.SetRoster(Array.Empty<string>(), 0, 0);
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

        private void SynchronizeServerRoster(NetworkRunner runner)
        {
            _waitingUsernames.Clear();
            foreach (var player in runner.ActivePlayers.OrderBy(activePlayer => activePlayer.PlayerId))
                _waitingUsernames[player] = ResolveUsernameFromToken(runner.GetPlayerConnectionToken(player), player);
        }

        private void PublishRoster(NetworkRunner runner)
        {
            var rosterState = LobbyRosterState.ActiveInstance;
            if (rosterState == null)
                return;

            var orderedUsernames = _waitingUsernames
                .OrderBy(entry => entry.Key.PlayerId)
                .Select(entry => NormalizeUsername(entry.Value))
                .ToArray();

            rosterState.SetRoster(orderedUsernames, runner.ActivePlayers.Count(), ResolveTargetPlayerCapacity(runner));
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
