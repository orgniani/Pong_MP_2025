using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Managers
{
    public class ScoreManager : NetworkBehaviour
    {
        [Header("Pong score")]
        [SerializeField] private int pointsToWin = 5;

        private Transform _finishLine;

        [Networked] public int LeftScore { get; private set; }
        [Networked] public int RightScore { get; private set; }

        [Networked, Capacity(4)]
        private NetworkArray<PlayerRef> _playerOrder => default;

        [Networked, Capacity(4)]
        private NetworkArray<PlayerRef> _winnersOrder => default;

        private readonly Dictionary<PlayerRef, Transform> _playerTransforms = new();
        private readonly HashSet<PlayerRef> _playersFinished = new();

        public event Action<PlayerRef> OnPlayerFinished;

        public event Action<int, int> OnScoreChanged;

        public void SetFinishLine(Transform finishLine)
        {
            _finishLine = finishLine;
        }

        public void RegisterPlayer(PlayerRef player, Transform playerTransform)
        {
            if (!_playerTransforms.ContainsKey(player))
                _playerTransforms.Add(player, playerTransform);
        }

        public void UnregisterPlayer(PlayerRef player)
        {
            _playerTransforms.Remove(player);
            _playersFinished.Remove(player);
        }

        public List<PlayerRef> GetCurrentPlayerScore()
        {
            var players = new List<PlayerRef>();
            for (int i = 0; i < _playerOrder.Length; i++)
            {
                var player = _playerOrder.Get(i);
                if (player != default)
                    players.Add(player);
            }
            return players;
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority) return;

            UpdateWinners();
            UpdateScore();
        }

        public void RegisterLeftGoal()
        {
            if (!HasStateAuthority)
                return;

            LeftScore++;
            OnScoreChanged?.Invoke(LeftScore, RightScore);
        }

        public void RegisterRightGoal()
        {
            if (!HasStateAuthority)
                return;

            RightScore++;
            OnScoreChanged?.Invoke(LeftScore, RightScore);
        }

        public bool HasWinner(out string winnerLabel)
        {
            if (LeftScore >= pointsToWin && LeftScore > RightScore)
            {
                winnerLabel = "Left Player Wins";
                return true;
            }

            if (RightScore >= pointsToWin && RightScore > LeftScore)
            {
                winnerLabel = "Right Player Wins";
                return true;
            }

            winnerLabel = string.Empty;
            return false;
        }

        public string GetMatchResultLabel()
        {
            if (LeftScore == RightScore)
                return "Draw";

            return LeftScore > RightScore ? "Left Player Wins" : "Right Player Wins";
        }

        private void UpdateWinners()
        {
            foreach (var kvp in _playerTransforms)
            {
                var player = kvp.Key;
                var transform = kvp.Value;

                if (_playersFinished.Contains(player) || transform == null)
                    continue;

                float distanceToFinishZ = Mathf.Abs(transform.position.z - _finishLine.position.z);
                if (distanceToFinishZ < 1f)
                {
                    _playersFinished.Add(player);

                    for (int i = 0; i < _winnersOrder.Length; i++)
                    {
                        if (_winnersOrder.Get(i) == default)
                        {
                            _winnersOrder.Set(i, player);
                            OnPlayerFinished?.Invoke(player);
                            break;
                        }
                    }

                    Debug.Log($"{player} finished!");
                }
            }
        }

        public List<PlayerRef> GetWinnersOrder()
        {
            var winners = new List<PlayerRef>();
            for (int i = 0; i < _winnersOrder.Length; i++)
            {
                var player = _winnersOrder.Get(i);
                if (player != default)
                    winners.Add(player);
            }
            return winners;
        }

        private void UpdateScore()
        {
            var activePlayers = new List<(PlayerRef player, float distance)>();

            foreach (var kvp in _playerTransforms)
            {
                var player = kvp.Key;
                var transform = kvp.Value;

                if (_playersFinished.Contains(player) || transform == null)
                    continue;

                float distanceToFinishZ = Mathf.Abs(transform.position.z - _finishLine.position.z);
                activePlayers.Add((player, distanceToFinishZ));
            }

            var finalOrder = new List<PlayerRef>(_playersFinished);

            activePlayers.Sort((a, b) => a.distance.CompareTo(b.distance));
            foreach (var (player, _) in activePlayers)
                finalOrder.Add(player);

            for (int i = 0; i < _playerOrder.Length; i++)
                _playerOrder.Set(i, i < finalOrder.Count ? finalOrder[i] : default);
        }

        public bool AreAnyActiveNonWinners()
        {
            foreach (var kvp in _playerTransforms)
            {
                var player = kvp.Key;
                var transform = kvp.Value;

                if (!_playersFinished.Contains(player) && transform != null)
                    return true;
            }

            return false;
        }
    }
}
