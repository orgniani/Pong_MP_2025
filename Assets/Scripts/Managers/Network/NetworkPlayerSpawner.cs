using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Managers.Network
{
    public class NetworkPlayerSpawner
    {
        private readonly NetworkPrefabRef _playerPrefab;

        private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

        public int SpawnedPlayerCount => _spawnedPlayers.Count;

        public bool IsSpawned(PlayerRef player) => _spawnedPlayers.ContainsKey(player);

        public NetworkPlayerSpawner(NetworkPrefabRef playerPrefab)
        {
            _playerPrefab = playerPrefab;
        }

        public void SpawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            if (runner == null)
                throw new ArgumentNullException(nameof(runner));

            var manager = Managers.NetworkManager.Instance;
            if (manager == null || !manager.TryResolveSpawnAssignment(runner, player, out var spawnIndex, out var teamId, out var laneId, out var spawnPoint))
            {
                Debug.LogError($"[NetworkPlayerSpawner] Failed to resolve spawn assignment for player {player.PlayerId}.");
                return;
            }

            NetworkObject playerObj = runner.Spawn(
                _playerPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                player,
                onBeforeSpawned: (r, obj) =>
                {
                    var spawnedPlayer = obj.GetComponent<Players.Player>();
                    spawnedPlayer.SpawnPointIndex = spawnIndex;
                    spawnedPlayer.SetTeamLane(teamId, laneId);
                }
            );

            _spawnedPlayers.Add(player, playerObj);
        }

        public void DespawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            if (_spawnedPlayers.TryGetValue(player, out NetworkObject obj))
            {
                runner.Despawn(obj);
                _spawnedPlayers.Remove(player);
            }
        }

        public void ClearAll()
        {
            _spawnedPlayers.Clear();
        }

        public void DespawnAll(NetworkRunner runner)
        {
            foreach (var obj in _spawnedPlayers.Values)
            {
                runner.Despawn(obj);
            }

            _spawnedPlayers.Clear();
        }

        public int ResolvePlayerSlotIndex(NetworkRunner runner, PlayerRef player)
        {
            return runner.ActivePlayers
                .OrderBy(activePlayer => activePlayer.PlayerId)
                .ToList()
                .FindIndex(activePlayer => activePlayer == player);
        }
    }
}
