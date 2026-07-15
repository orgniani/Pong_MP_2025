using System;
using System.Collections.Generic;
using Common;

namespace Managers.Network
{
    public enum TeamSide
    {
        Left = 0,
        Right = 1
    }

    public readonly struct TeamLaneAssignment
    {
        public TeamLaneAssignment(int teamId, int laneId)
        {
            TeamId = teamId;
            LaneId = laneId;
        }

        public int TeamId { get; }
        public int LaneId { get; }
    }

    public static class TeamLaneAssignmentUtility
    {
        private static readonly TeamLaneAssignment[] OneVsOneAssignments =
        {
            new TeamLaneAssignment((int)TeamSide.Left, 0),
            new TeamLaneAssignment((int)TeamSide.Right, 0)
        };

        private static readonly TeamLaneAssignment[] TwoVsTwoAssignments =
        {
            new TeamLaneAssignment((int)TeamSide.Left, 0),
            new TeamLaneAssignment((int)TeamSide.Right, 0),
            new TeamLaneAssignment((int)TeamSide.Left, 1),
            new TeamLaneAssignment((int)TeamSide.Right, 1)
        };

        public static MatchMode ResolveMode(int targetPlayerCapacity)
        {
            return targetPlayerCapacity >= MatchModeExtensions.TwoVsTwoMaxPlayers
                ? MatchMode.TwoVsTwo
                : MatchMode.OneVsOne;
        }

        public static TeamLaneAssignment ResolveAssignment(MatchMode mode, int slotIndex)
        {
            var assignments = GetAssignments(mode);
            if (assignments.Length == 0)
                return new TeamLaneAssignment((int)TeamSide.Left, 0);

            var normalizedIndex = slotIndex;
            if (normalizedIndex < 0)
                normalizedIndex = 0;
            else if (normalizedIndex >= assignments.Length)
                normalizedIndex = assignments.Length - 1;

            return assignments[normalizedIndex];
        }

        public static int ResolveSpawnLayoutIndex(MatchMode mode, int teamId, int laneId)
        {
            var assignments = GetAssignments(mode);
            for (var i = 0; i < assignments.Length; i++)
            {
                if (assignments[i].TeamId == teamId && assignments[i].LaneId == laneId)
                    return i;
            }

            return -1;
        }

        public static string FormatAssignmentLabel(int teamId, int laneId)
        {
            return $"Team {FormatTeamLabel(teamId)} - Lane {Math.Max(0, laneId)}";
        }

        public static string FormatTeamLabel(int teamId)
        {
            return teamId == (int)TeamSide.Right ? "Right" : "Left";
        }

        private static TeamLaneAssignment[] GetAssignments(MatchMode mode)
        {
            return mode == MatchMode.TwoVsTwo ? TwoVsTwoAssignments : OneVsOneAssignments;
        }
    }

    public readonly struct LobbySessionSnapshot
    {
        private static readonly string[] EmptyUsernames = Array.Empty<string>();
        private static readonly int[] EmptyPlayerIds = Array.Empty<int>();
        private static readonly bool[] EmptyReadyStates = Array.Empty<bool>();
        private static readonly int[] EmptyTeamIds = Array.Empty<int>();
        private static readonly int[] EmptyLaneIds = Array.Empty<int>();
        private static readonly int[] EmptyColorIds = Array.Empty<int>();

        public readonly struct PlayerSlot
        {
            public PlayerSlot(int playerId, string username, bool isReady, int teamId, int laneId, int colorId)
            {
                PlayerId = playerId;
                Username = username ?? string.Empty;
                IsReady = isReady;
                TeamId = teamId;
                LaneId = laneId;
                ColorId = colorId;
            }

            public int PlayerId { get; }
            public string Username { get; }
            public bool IsReady { get; }
            public int TeamId { get; }
            public int LaneId { get; }
            public int ColorId { get; }
        }

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
