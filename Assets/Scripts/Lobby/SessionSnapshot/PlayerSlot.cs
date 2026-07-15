namespace Lobby.SessionSnapshot
{
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
}
