using System;

namespace Managers.Network
{
    public readonly struct LobbySessionSnapshot
    {
        private static readonly string[] EmptyUsernames = Array.Empty<string>();

        public LobbySessionSnapshot(string[] waitingUsernames, int currentPlayerCount, int targetPlayerCapacity)
        {
            WaitingUsernames = waitingUsernames ?? EmptyUsernames;
            CurrentPlayerCount = Math.Max(0, currentPlayerCount);
            TargetPlayerCapacity = Math.Max(0, targetPlayerCapacity);
        }

        public string[] WaitingUsernames { get; }
        public int CurrentPlayerCount { get; }
        public int TargetPlayerCapacity { get; }
    }
}
