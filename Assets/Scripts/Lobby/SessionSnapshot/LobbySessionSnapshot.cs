using System;

namespace Lobby.SessionSnapshot
{
    public readonly struct LobbySessionSnapshot
    {
        private static readonly string[] EmptyUsernames = Array.Empty<string>();
        private static readonly int[] EmptyPlayerIds = Array.Empty<int>();
        private static readonly bool[] EmptyReadyStates = Array.Empty<bool>();
        private static readonly int[] EmptyTeamIds = Array.Empty<int>();
        private static readonly int[] EmptyLaneIds = Array.Empty<int>();
        private static readonly int[] EmptyColorIds = Array.Empty<int>();

        public static LobbySessionSnapshot Empty { get; } = new(
            waitingUsernames: EmptyUsernames,
            playerIds: EmptyPlayerIds,
            readyStates: EmptyReadyStates,
            teamIds: EmptyTeamIds,
            laneIds: EmptyLaneIds,
            colorIds: EmptyColorIds,
            localPlayerId: -1,
            isLocalPlayerReady: false,
            currentPlayerCount: 0,
            targetPlayerCapacity: 0);

        public LobbySessionSnapshot(string[] waitingUsernames, int[] playerIds, bool[] readyStates, int[] teamIds, int[] laneIds, int[] colorIds, int localPlayerId, bool isLocalPlayerReady, int currentPlayerCount, int targetPlayerCapacity)
        {
            WaitingUsernames = waitingUsernames;
            PlayerIds = playerIds;
            ReadyStates = readyStates;
            TeamIds = teamIds;
            LaneIds = laneIds;
            ColorIds = colorIds;
            LocalPlayerId = localPlayerId;
            IsLocalPlayerReady = isLocalPlayerReady;
            CurrentPlayerCount = Math.Max(0, currentPlayerCount);
            TargetPlayerCapacity = Math.Max(0, targetPlayerCapacity);
        }

        public string[] WaitingUsernames { get; }
        public int[] PlayerIds { get; }
        public bool[] ReadyStates { get; }
        public int[] TeamIds { get; }
        public int[] LaneIds { get; }
        public int[] ColorIds { get; }
        public int LocalPlayerId { get; }
        public bool IsLocalPlayerReady { get; }
        public int CurrentPlayerCount { get; }
        public int TargetPlayerCapacity { get; }
        public int PlayerCount => WaitingUsernames.Length;

        public PlayerSlot GetPlayerSlot(int index)
        {
            if (index < 0 || index >= PlayerCount)
                return default;

            return new PlayerSlot(
                PlayerIds[index],
                WaitingUsernames[index],
                ReadyStates[index],
                TeamIds[index],
                LaneIds[index],
                ColorIds[index]);
        }
    }
}
