using System;

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

        public static UI.UIGameModeFilter ResolveMode(int targetPlayerCapacity)
        {
            return targetPlayerCapacity >= UI.UIGameModeFilterExtensions.TwoVsTwoMaxPlayers
                ? UI.UIGameModeFilter.TwoVsTwo
                : UI.UIGameModeFilter.OneVsOne;
        }

        public static TeamLaneAssignment ResolveAssignment(UI.UIGameModeFilter mode, int slotIndex)
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

        public static int ResolveSpawnLayoutIndex(UI.UIGameModeFilter mode, int teamId, int laneId)
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

        private static TeamLaneAssignment[] GetAssignments(UI.UIGameModeFilter mode)
        {
            return mode == UI.UIGameModeFilter.TwoVsTwo ? TwoVsTwoAssignments : OneVsOneAssignments;
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

        public LobbySessionSnapshot(string[] waitingUsernames, int[] playerIds, bool[] readyStates, int[] teamIds, int[] laneIds, int[] colorIds, int localPlayerId, bool isLocalPlayerReady, int currentPlayerCount, int targetPlayerCapacity)
        {
            WaitingUsernames = waitingUsernames ?? EmptyUsernames;
            PlayerIds = playerIds ?? EmptyPlayerIds;
            ReadyStates = readyStates ?? EmptyReadyStates;
            TeamIds = teamIds ?? EmptyTeamIds;
            LaneIds = laneIds ?? EmptyLaneIds;
            ColorIds = colorIds ?? EmptyColorIds;
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
    }
}
