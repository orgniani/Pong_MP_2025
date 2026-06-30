using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace Managers.Network
{
    public class NetworkPlayerSpawner
    {
        private readonly Transform[] _spawnPoints;
        private readonly NetworkPrefabRef _playerPrefab;

        private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

        public int SpawnedPlayerCount => _spawnedPlayers.Count;

        public bool IsSpawned(PlayerRef player) => _spawnedPlayers.ContainsKey(player);

        public NetworkPlayerSpawner(Transform[] spawnPoints, NetworkPrefabRef playerPrefab)
        {
            _spawnPoints = spawnPoints;
            _playerPrefab = playerPrefab;
        }

        public void SpawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            int spawnIndex = _spawnedPlayers.Count % _spawnPoints.Length;
            Transform spawnPoint = _spawnPoints[spawnIndex];

            NetworkObject playerObj = runner.Spawn(
                _playerPrefab,
                spawnPoint.position,
                spawnPoint.rotation,
                player,
                onBeforeSpawned: (r, obj) =>
                {
                    obj.GetComponent<Players.Player>().spawnPointIndex = spawnIndex;
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
    }
}
