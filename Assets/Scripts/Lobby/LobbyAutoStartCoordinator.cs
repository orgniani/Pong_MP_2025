using System;
using System.Collections;
using Network;
using UnityEngine;

namespace Lobby
{
    public sealed class LobbyAutoStartCoordinator
    {
        private readonly float _rearmDelaySeconds;
        private Coroutine _pendingRetry;

        public LobbyAutoStartCoordinator(float rearmDelaySeconds = 0f)
        {
            _rearmDelaySeconds = Mathf.Max(0f, rearmDelaySeconds);
        }

        public void Schedule(MonoBehaviour coroutineHost, MatchSessionState matchSessionState, Action retryStart)
        {
            if (coroutineHost == null)
                return;

            Cancel(coroutineHost);
            _pendingRetry = coroutineHost.StartCoroutine(RearmAndRetry(matchSessionState, retryStart));
        }

        public void Cancel(MonoBehaviour coroutineHost)
        {
            if (coroutineHost == null || _pendingRetry == null)
                return;

            coroutineHost.StopCoroutine(_pendingRetry);
            _pendingRetry = null;
        }

        private IEnumerator RearmAndRetry(MatchSessionState matchSessionState, Action retryStart)
        {
            if (_rearmDelaySeconds > 0f)
                yield return new WaitForSecondsRealtime(_rearmDelaySeconds);
            else
                yield return null;

            _pendingRetry = null;
            matchSessionState?.RearmAutoStart();
            retryStart?.Invoke();
        }
    }
}
