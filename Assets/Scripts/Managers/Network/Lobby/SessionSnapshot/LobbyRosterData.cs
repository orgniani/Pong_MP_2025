using System;
using System.Collections.Generic;

namespace Managers.Network
{
    public readonly struct LobbyRosterData
    {
        public LobbyRosterData(IReadOnlyList<string> usernames, IReadOnlyList<int> playerIds, IReadOnlyList<bool> readyStates, IReadOnlyList<int> teamIds, IReadOnlyList<int> laneIds, IReadOnlyList<int> colorIds, int currentPlayerCount, int targetPlayerCapacity)
        {
            Usernames = usernames;
            PlayerIds = playerIds;
            ReadyStates = readyStates;
            TeamIds = teamIds;
            LaneIds = laneIds;
            ColorIds = colorIds;
            CurrentPlayerCount = currentPlayerCount;
            TargetPlayerCapacity = targetPlayerCapacity;
        }

        public static LobbyRosterData Empty { get; } = new(
            usernames: Array.Empty<string>(),
            playerIds: Array.Empty<int>(),
            readyStates: Array.Empty<bool>(),
            teamIds: Array.Empty<int>(),
            laneIds: Array.Empty<int>(),
            colorIds: Array.Empty<int>(),
            currentPlayerCount: 0,
            targetPlayerCapacity: 0);

        public IReadOnlyList<string> Usernames { get; }
        public IReadOnlyList<int> PlayerIds { get; }
        public IReadOnlyList<bool> ReadyStates { get; }
        public IReadOnlyList<int> TeamIds { get; }
        public IReadOnlyList<int> LaneIds { get; }
        public IReadOnlyList<int> ColorIds { get; }
        public int CurrentPlayerCount { get; }
        public int TargetPlayerCapacity { get; }
    }
}
