using UnityEngine;

namespace Managers.Network
{
    public class MatchSessionState : MonoBehaviour
    {
        private enum SessionLifecyclePhase
        {
            WaitingForPlayers,
            MatchInProgress,
            PostGameCleanup
        }

        private SessionLifecyclePhase _phase = SessionLifecyclePhase.WaitingForPlayers;

        public bool MatchInProgress => _phase == SessionLifecyclePhase.MatchInProgress;
        public bool IsPostGameCleanup => _phase == SessionLifecyclePhase.PostGameCleanup;
        public bool AutoStartArmed { get; private set; } = true;

        public bool CanStartMatch(int activePlayers, int minPlayersToStart)
        {
            return _phase == SessionLifecyclePhase.WaitingForPlayers
                   && AutoStartArmed
                   && activePlayers >= minPlayersToStart;
        }

        public void MarkMatchStarted()
        {
            _phase = SessionLifecyclePhase.MatchInProgress;
            AutoStartArmed = false;
        }

        public void MarkMatchEnded()
        {
            _phase = SessionLifecyclePhase.PostGameCleanup;
            AutoStartArmed = false;
        }

        public void RearmAutoStart()
        {
            if (_phase != SessionLifecyclePhase.WaitingForPlayers)
                return;

            AutoStartArmed = true;
        }

        public void ResetToWaitingForPlayers()
        {
            _phase = SessionLifecyclePhase.WaitingForPlayers;
            AutoStartArmed = true;
        }
    }
}
