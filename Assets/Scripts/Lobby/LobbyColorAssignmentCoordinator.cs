using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Fusion;
using UnityEngine;

namespace Lobby
{
    internal sealed class LobbyColorAssignmentCoordinator
    {
        private readonly Dictionary<PlayerRef, int> _colorIds;
        private readonly Func<PlayerRef, bool> _isLobbyMember;
        private readonly Func<PaddleColorPalette> _paletteResolver;

        public LobbyColorAssignmentCoordinator(
            Dictionary<PlayerRef, int> colorIds,
            Func<PlayerRef, bool> isLobbyMember,
            Func<PaddleColorPalette> paletteResolver)
        {
            _colorIds = colorIds ?? throw new ArgumentNullException(nameof(colorIds));
            _isLobbyMember = isLobbyMember ?? throw new ArgumentNullException(nameof(isLobbyMember));
            _paletteResolver = paletteResolver ?? throw new ArgumentNullException(nameof(paletteResolver));
        }

        public bool TrySetColor(NetworkRunner runner, PlayerRef player, int colorId)
        {
            if (runner == null || !runner.IsServer || !player.IsRealPlayer || !_isLobbyMember(player))
                return false;

            var palette = _paletteResolver();
            if (palette == null || !palette.IsValidColorId(colorId))
                return false;

            foreach (var entry in _colorIds)
            {
                if (entry.Key != player && entry.Value == colorId)
                    return false;
            }

            _colorIds[player] = colorId;
            return true;
        }

        public void SynchronizeColorAssignments(NetworkRunner runner)
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

                if (TryAssignColorForLobbyMember(runner, player))
                    continue;

                runner.Disconnect(player, null);
            }
        }

        public bool TryAssignColorForLobbyMember(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null || !runner.IsServer || !_isLobbyMember(player))
                return false;

            var palette = _paletteResolver();
            if (palette == null)
                return false;

            var availableColorIds = GetAvailableColorIds(palette, player);
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

            var palette = _paletteResolver();
            return palette != null && palette.IsValidColorId(colorId);
        }
    }
}
