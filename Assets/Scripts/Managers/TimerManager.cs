using Config;
using Fusion;
using System;
using System.Collections;
using UnityEngine;

namespace Managers
{
    public class TimerManager : NetworkBehaviour
    {
        [Networked] private float _remainingTime { get; set; }
        [Networked] private bool _timerRunning { get; set; } = false;

        public float RemainingTime => _remainingTime;
        public bool TimerRunning => _timerRunning;

        private Coroutine _countdownCoroutine;

        public override void Spawned()
        {
            if (!HasStateAuthority)
                return;

            _remainingTime = MatchRules.GetMatchDurationSeconds();
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || !_timerRunning) return;

            _remainingTime -= Runner.DeltaTime;
            if (_remainingTime <= 0f)
            {
                _remainingTime = 0f;
                _timerRunning = false;
            }
        }

        public void StartMatchCountdown()
        {
            if (!HasStateAuthority || _timerRunning || _countdownCoroutine != null)
                return;

            _countdownCoroutine = StartCoroutine(CountdownCoroutine());
        }

        private IEnumerator CountdownCoroutine()
        {
            yield return new WaitForSeconds(MatchRules.GetCountdownSeconds());
            _timerRunning = true;
            _countdownCoroutine = null;
        }

        public void StopTimer()
        {
            _timerRunning = false;
        }

        public void ResetTimer()
        {
            if (!HasStateAuthority)
                return;

            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }

            _timerRunning = false;
            _remainingTime = MatchRules.GetMatchDurationSeconds();
        }
    }
}
