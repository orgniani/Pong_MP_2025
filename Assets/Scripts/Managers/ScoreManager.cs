using System;
using Fusion;
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
    }
}
