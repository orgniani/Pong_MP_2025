namespace Lobby.SessionSnapshot
{
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
}
