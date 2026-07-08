using System;
using System.Collections.Generic;
using Fusion;

namespace Managers.Network
{
    public sealed class LobbyRosterState : NetworkBehaviour
    {
        public const int MaxRosterSize = 4;

        [Networked, Capacity(MaxRosterSize)]
        private NetworkArray<NetworkString<_16>> Usernames => default;

        [Networked] private int _currentPlayerCount { get; set; }
        [Networked] private int _targetPlayerCapacity { get; set; }

        private static LobbyRosterState _activeInstance;
        private ChangeDetector _changes;

        public event Action<LobbySessionSnapshot> SnapshotChanged;

        public static LobbyRosterState ActiveInstance =>
            _activeInstance != null && _activeInstance.Object != null ? _activeInstance : null;

        public override void Spawned()
        {
            _activeInstance = this;
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (ReferenceEquals(_activeInstance, this))
                _activeInstance = null;
        }

        public override void Render()
        {
            var changed = false;
            foreach (var _ in _changes.DetectChanges(this))
                changed = true;

            if (changed)
                SnapshotChanged?.Invoke(BuildSnapshot());
        }

        public void SetRoster(IReadOnlyList<string> usernames, int currentPlayerCount, int targetPlayerCapacity)
        {
            if (!Object.HasStateAuthority)
                return;

            var count = usernames != null ? Math.Min(usernames.Count, MaxRosterSize) : 0;

            for (var i = 0; i < MaxRosterSize; i++)
                Usernames.Set(i, i < count ? usernames[i] : default);

            _currentPlayerCount = Math.Max(0, currentPlayerCount);
            _targetPlayerCapacity = Math.Max(0, targetPlayerCapacity);
        }

        public LobbySessionSnapshot BuildSnapshot()
        {
            var usernames = new List<string>(MaxRosterSize);
            for (var i = 0; i < MaxRosterSize; i++)
            {
                var value = Usernames[i].ToString();
                if (!string.IsNullOrEmpty(value))
                    usernames.Add(value);
            }

            return new LobbySessionSnapshot(usernames.ToArray(), _currentPlayerCount, _targetPlayerCapacity);
        }
    }
}
