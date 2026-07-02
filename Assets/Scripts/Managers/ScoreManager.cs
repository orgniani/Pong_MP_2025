using System;
using Fusion;
using Helpers;
using UnityEngine;

namespace Managers
{
    public class ScoreManager : NetworkBehaviour
    {
        [Header("Pong score")]
        [SerializeField] private int pointsToWin = 5;

        [Networked] public int LeftScore { get; private set; }
        [Networked] public int RightScore { get; private set; }

        private ChangeDetector _changes;

        public event Action<int, int> OnScoreChanged;

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);
        }

        public override void Render()
        {
            foreach (var change in _changes.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(LeftScore):
                    case nameof(RightScore):
                        OnScoreChanged?.Invoke(LeftScore, RightScore);
                        break;
                }
            }
        }

        public void ResetScore()
        {
            if (!HasStateAuthority)
                return;

            LeftScore = 0;
            RightScore = 0;
        }

        public void RegisterLeftGoal()
        {
            if (!HasStateAuthority)
                return;

            LeftScore++;
        }

        public void RegisterRightGoal()
        {
            if (!HasStateAuthority)
                return;

            RightScore++;
        }

        public bool HasWinner(out string winnerLabel)
        {
            var (left, right) = PlayerNameLookup.GetSideNames();

            if (LeftScore >= pointsToWin && LeftScore > RightScore)
            {
                winnerLabel = $"{left} WINS";
                return true;
            }

            if (RightScore >= pointsToWin && RightScore > LeftScore)
            {
                winnerLabel = $"{right} WINS";
                return true;
            }

            winnerLabel = string.Empty;
            return false;
        }

        public string GetMatchResultLabel()
        {
            if (LeftScore == RightScore)
                return "DRAW";

            var (left, right) = PlayerNameLookup.GetSideNames();
            return LeftScore > RightScore ? $"{left} WINS" : $"{right} WINS";
        }
    }
}
