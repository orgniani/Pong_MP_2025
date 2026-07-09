using System.Collections;
using UnityEngine;
using Fusion;
using Managers.Network;

namespace Managers
{
    public class GameOverManager : NetworkBehaviour
    {
        [SerializeField] private float resultDisplaySeconds = 4f;
        [SerializeField] private bool enableLogs = false;

        [Networked]
        private bool _isGameOver { get; set; }

        private ScoreManager _scoreManager;
        private TimerManager _timerManager;
        private Balls.Ball _ball;

        public bool IsGameOver => _isGameOver;

        public override void Spawned()
        {
            _scoreManager = FindFirstObjectByType<ScoreManager>();
            _timerManager = FindFirstObjectByType<TimerManager>();
            _ball = FindFirstObjectByType<Balls.Ball>();

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

            _ball ??= FindFirstObjectByType<Balls.Ball>();
            _ball?.StopImmediately();

            Log($"Game over triggered! Reason: {reason}");

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

        private void Log(string message)
        {
            if (!enableLogs)
                return;

            Debug.Log($"[{GetType().Name}] {message}", this);
        }
    }
}
