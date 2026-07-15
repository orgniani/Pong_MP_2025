using Common;

namespace Managers.Network
{
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

        private static TeamLaneAssignment[] GetAssignments(MatchMode mode)
        {
            return mode == MatchMode.TwoVsTwo ? TwoVsTwoAssignments : OneVsOneAssignments;
        }
    }
}
