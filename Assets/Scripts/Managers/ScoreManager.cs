using System;
using Fusion;
using Helpers;
using UnityEngine;

namespace Managers
{
    public class ScoreManager : NetworkBehaviour
    {
        [Networked] private int _leftScore { get; set; }
        [Networked] private int _rightScore { get; set; }

        public int LeftScore => _leftScore;
        public int RightScore => _rightScore;

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
                    case nameof(_leftScore):
                    case nameof(_rightScore):
                        OnScoreChanged?.Invoke(LeftScore, RightScore);
                        break;
                }
            }
        }

        public void ResetScore()
        {
            if (!HasStateAuthority)
                return;

            _leftScore = 0;
            _rightScore = 0;
        }

        public void RegisterLeftGoal()
        {
            if (!HasStateAuthority)
                return;

            _leftScore++;
        }

        public void RegisterRightGoal()
        {
            if (!HasStateAuthority)
                return;

            _rightScore++;
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
