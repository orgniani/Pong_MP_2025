using System.Collections;
using UnityEngine;
using Fusion;
using Managers.Network;

namespace Managers
{
    public class GameOverManager : NetworkBehaviour
    {
        [SerializeField] private float resultDisplaySeconds = 4f;

        [Networked]
        private bool _isGameOver { get; set; }

        private ScoreManager _scoreManager;
        private TimerManager _timerManager;

        public bool IsGameOver => _isGameOver;

        public override void Spawned()
        {
            _scoreManager = FindFirstObjectByType<ScoreManager>();
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

            if (_scoreManager != null && _scoreManager.HasWinner(out string winner))
            {
                TriggerGameOver(winner);
            }
        }

        public void TriggerForfeit(string reason)
        {
            TriggerGameOver(reason);
        }

        private void TriggerGameOver(string reason)
        {
            if (_isGameOver)
                return;

            _isGameOver = true;

            if (_timerManager != null)
                _timerManager.StopTimer();

            Debug.Log($"<color=red>Game over triggered! Reason: {reason}</color>");

            if (Object.HasStateAuthority)
                StartCoroutine(ReturnToLobbyAfterDelay());
        }

        private IEnumerator ReturnToLobbyAfterDelay()
        {
            yield return new WaitForSeconds(resultDisplaySeconds);

            NetworkManager.Instance?.PlayerSpawner?.DespawnAll(Runner);

            if (_timerManager != null)
                Runner.Despawn(_timerManager.Object);

            if (_scoreManager != null)
                Runner.Despawn(_scoreManager.Object);

            Runner.GetComponent<MatchSessionState>()?.MarkMatchEnded();
            NetworkManager.Instance?.PrepareForLobbyState();
            NetworkManager.Instance?.ForceDisconnectAllPlayers(Runner);

            Runner.Despawn(Object);
        }
    }
}
