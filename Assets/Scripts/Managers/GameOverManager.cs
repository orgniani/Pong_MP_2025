using UnityEngine;
using Fusion;

namespace Managers
{
    public class GameOverManager : NetworkBehaviour
    {
        [Networked]
        private bool _isGameOver { get; set; }

        private RacePositionManager _racePositionManager;
        private TimerManager _timerManager;

        public bool IsGameOver => _isGameOver;

        public override void Spawned()
        {
            _racePositionManager = FindFirstObjectByType<RacePositionManager>();
            _timerManager = FindFirstObjectByType<TimerManager>();

            Debug.Log("<color=green>NetworkGameOverManager spawned.</color>");
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || _isGameOver)
                return;

            if (_timerManager != null && _timerManager.RemainingTime <= 0f)
            {
                TriggerGameOver("Timer ran out");
                return;
            }

            if (_racePositionManager != null && !_racePositionManager.AreAnyActiveNonWinners())
            {
                TriggerGameOver("All players finished or left");
            }
        }

        private void TriggerGameOver(string reason)
        {
            _isGameOver = true;

            if (_timerManager != null)
                _timerManager.StopTimer();

            Debug.Log($"<color=red>Game over triggered! Reason: {reason}</color>");

            if (Object.HasStateAuthority)
                NetworkManager.Instance.Shutdown();
        }
    }
}