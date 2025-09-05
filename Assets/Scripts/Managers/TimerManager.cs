using Fusion;
using System;
using System.Collections;
using UnityEngine;

namespace Managers
{
    public class TimerManager : NetworkBehaviour
    {
        [Networked] public float RemainingTime { get; private set; } = 120f;
        [Networked] public bool TimerRunning { get; private set; } = false;

        private Coroutine _countdownCoroutine;

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority || !TimerRunning) return;

            RemainingTime -= Runner.DeltaTime;
            if (RemainingTime <= 0f)
            {
                RemainingTime = 0f;
                TimerRunning = false;
                Debug.Log("Time ended!");
            }
        }

        public void StartRaceCountdown(BlockerManager blockerManager)
        {
            if (!HasStateAuthority || TimerRunning || _countdownCoroutine != null)
                return;

            _countdownCoroutine = StartCoroutine(CountdownCoroutine(blockerManager));
        }

        private IEnumerator CountdownCoroutine(BlockerManager blockerManager)
        {
            yield return new WaitForSeconds(3f);

            blockerManager.Rpc_RemoveStartBlocker();
            TimerRunning = true;
        }

        public void StopTimer()
        {
            TimerRunning = false;
        }
    }
}