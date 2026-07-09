using System;

namespace Managers.Network
{
    public readonly struct LobbySessionSnapshot
    {
        private static readonly string[] EmptyUsernames = Array.Empty<string>();
        private static readonly bool[] EmptyReadyStates = Array.Empty<bool>();

        public LobbySessionSnapshot(string[] waitingUsernames, bool[] readyStates, bool isLocalPlayerReady, int currentPlayerCount, int targetPlayerCapacity)
        {
            WaitingUsernames = waitingUsernames ?? EmptyUsernames;
            ReadyStates = readyStates ?? EmptyReadyStates;
            IsLocalPlayerReady = isLocalPlayerReady;
            CurrentPlayerCount = Math.Max(0, currentPlayerCount);
            TargetPlayerCapacity = Math.Max(0, targetPlayerCapacity);
        }

        public string[] WaitingUsernames { get; }
        public bool[] ReadyStates { get; }
        public bool IsLocalPlayerReady { get; }
        public int CurrentPlayerCount { get; }
        public int TargetPlayerCapacity { get; }
    }
}
