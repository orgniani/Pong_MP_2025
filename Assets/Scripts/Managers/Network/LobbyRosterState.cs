using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UI;
using UnityEngine;

namespace Managers.Network
{
    public sealed class LobbyRosterState : NetworkBehaviour
    {
        public const int MaxRosterSize = UIGameModeFilterExtensions.TwoVsTwoMaxPlayers;

        [Networked, Capacity(MaxRosterSize)]
        private NetworkArray<NetworkString<_16>> Usernames => default;
        [Networked, Capacity(MaxRosterSize)]
        private NetworkArray<int> PlayerIds => default;
        [Networked, Capacity(MaxRosterSize)]
        private NetworkArray<int> ReadyStates => default;
        [Networked, Capacity(MaxRosterSize)]
        private NetworkArray<int> TeamIds => default;
        [Networked, Capacity(MaxRosterSize)]
        private NetworkArray<int> LaneIds => default;
        [Networked, Capacity(MaxRosterSize)]
        private NetworkArray<int> ColorIds => default;

        [Networked] private int _currentPlayerCount { get; set; }
        [Networked] private int _targetPlayerCapacity { get; set; }
        [Networked] private int _snapshotRevision { get; set; }

        private static LobbyRosterState _activeInstance;
        private ChangeDetector _changes;

        public event Action<LobbySessionSnapshot> SnapshotChanged;

        public static event Action<LobbyRosterState> ActiveInstanceChanged;

        public static LobbyRosterState ActiveInstance =>
            _activeInstance != null && _activeInstance.Object != null ? _activeInstance : null;

        public static LobbyRosterState FindForRunner(NetworkRunner runner)
        {
            if (runner == null)
                return null;

            return UnityEngine.Object.FindObjectsByType<LobbyRosterState>(FindObjectsSortMode.InstanceID)
                .Where(state => state != null && state.Object != null && state.Runner == runner)
                .OrderByDescending(state => state.Object.HasStateAuthority)
                .FirstOrDefault();
        }

        public override void Spawned()
        {
            _activeInstance = this;
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
            ActiveInstanceChanged?.Invoke(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (ReferenceEquals(_activeInstance, this))
            {
                _activeInstance = null;
                ActiveInstanceChanged?.Invoke(null);
            }
        }

        public override void Render()
        {
            foreach (var change in _changes.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(Usernames):
                    case nameof(PlayerIds):
                    case nameof(ReadyStates):
                    case nameof(TeamIds):
                    case nameof(LaneIds):
                    case nameof(ColorIds):
                    case nameof(_currentPlayerCount):
                    case nameof(_targetPlayerCapacity):
                    case nameof(_snapshotRevision):
                        SnapshotChanged?.Invoke(BuildSnapshot());
                        break;
                }
            }
        }

        public void SetRoster(IReadOnlyList<string> usernames, IReadOnlyList<int> playerIds, IReadOnlyList<bool> readyStates, IReadOnlyList<int> teamIds, IReadOnlyList<int> laneIds, IReadOnlyList<int> colorIds, int currentPlayerCount, int targetPlayerCapacity)
        {
            if (!Object.HasStateAuthority)
                return;

            var count = usernames != null ? Math.Min(usernames.Count, MaxRosterSize) : 0;

            for (var i = 0; i < MaxRosterSize; i++)
            {
                Usernames.Set(i, i < count ? usernames[i] : default);
                PlayerIds.Set(i, i < count && playerIds != null && i < playerIds.Count ? Mathf.Max(0, playerIds[i]) : default);
                ReadyStates.Set(i, i < count && readyStates != null && i < readyStates.Count && readyStates[i] ? 1 : 0);
                TeamIds.Set(i, i < count && teamIds != null && i < teamIds.Count ? Mathf.Max(0, teamIds[i]) : default);
                LaneIds.Set(i, i < count && laneIds != null && i < laneIds.Count ? Mathf.Max(0, laneIds[i]) : default);
                ColorIds.Set(i, i < count && colorIds != null && i < colorIds.Count ? colorIds[i] : -1);
            }

            _currentPlayerCount = Math.Max(0, currentPlayerCount);
            _targetPlayerCapacity = Math.Max(0, targetPlayerCapacity);
            _snapshotRevision++;
        }

        public void RequestLocalPlayerReadyLock()
        {
            if (Runner == null || Object == null)
                return;

            if (Object.HasStateAuthority)
            {
                LobbySessionState.FindForRunner(Runner)?.TryLockReady(Runner, Runner.LocalPlayer);
                return;
            }

            RPC_RequestReadyLock();
        }

        public LobbySessionSnapshot BuildSnapshot()
        {
            var usernames = new List<string>(MaxRosterSize);
            var playerIds = new List<int>(MaxRosterSize);
            var readyStates = new List<bool>(MaxRosterSize);
            var teamIds = new List<int>(MaxRosterSize);
            var laneIds = new List<int>(MaxRosterSize);
            var colorIds = new List<int>(MaxRosterSize);
            var localPlayerId = Runner != null ? Runner.LocalPlayer.PlayerId : -1;
            var isLocalPlayerReady = false;

            for (var i = 0; i < MaxRosterSize; i++)
            {
                var value = Usernames[i].ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    usernames.Add(value);
                    playerIds.Add(PlayerIds[i]);

                    var isReady = ReadyStates[i] != 0;
                    readyStates.Add(isReady);
                    teamIds.Add(TeamIds[i]);
                    laneIds.Add(LaneIds[i]);
                    colorIds.Add(ColorIds[i]);

                    if (PlayerIds[i] == localPlayerId && isReady)
                        isLocalPlayerReady = true;
                }
            }

            return new LobbySessionSnapshot(usernames.ToArray(), playerIds.ToArray(), readyStates.ToArray(), teamIds.ToArray(), laneIds.ToArray(), colorIds.ToArray(), localPlayerId, isLocalPlayerReady, _currentPlayerCount, _targetPlayerCapacity);
        }

        public void RequestLocalPlayerColorChange(int colorId)
        {
            if (Runner == null || Object == null)
                return;

            if (Object.HasStateAuthority)
            {
                LobbySessionState.FindForRunner(Runner)?.TrySetColor(Runner, Runner.LocalPlayer, colorId);
                return;
            }

            RPC_RequestColorChange(colorId);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestReadyLock(RpcInfo info = default)
        {
            if (!info.Source.IsRealPlayer)
                return;

            LobbySessionState.FindForRunner(Runner)?.TryLockReady(Runner, info.Source);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestColorChange(int colorId, RpcInfo info = default)
        {
            if (!info.Source.IsRealPlayer)
                return;

            LobbySessionState.FindForRunner(Runner)?.TrySetColor(Runner, info.Source, colorId);
        }
    }
}
