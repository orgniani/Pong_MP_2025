using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace Managers.Network
{
    public class NetworkPlayerSpawner
    {
        private readonly Transform[] _spawnPoints;
        private readonly NetworkPrefabRef _playerPrefab;
        private readonly RacePositionManager _racePositionManager;
        private readonly Transform _finishLine;

        private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

        public int SpawnedPlayerCount => _spawnedPlayers.Count;

        public NetworkPlayerSpawner(Transform[] spawnPoints, NetworkPrefabRef playerPrefab, RacePositionManager raceManager, Transform finishLine)
        {
            _spawnPoints = spawnPoints;
            _playerPrefab = playerPrefab;
            _racePositionManager = raceManager;
            _finishLine = finishLine;
        }

        public void SpawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            Vector3 spawnPos = _spawnPoints[_spawnedPlayers.Count % _spawnPoints.Length].position;
            NetworkObject playerObj = runner.Spawn(_playerPrefab, spawnPos, Quaternion.identity, player);
            _spawnedPlayers.Add(player, playerObj);

            _racePositionManager.RegisterPlayer(player, playerObj.transform);
            _racePositionManager.SetFinishLine(_finishLine);
        }

        public void DespawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            if (_spawnedPlayers.TryGetValue(player, out NetworkObject obj))
            {
                runner.Despawn(obj);
                _racePositionManager.UnregisterPlayer(player);
                _spawnedPlayers.Remove(player);
            }
        }

        public void ClearAll()
        {
            _spawnedPlayers.Clear();
        }
    }
}