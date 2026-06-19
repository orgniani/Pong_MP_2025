namespace UI
{
    public enum UIGameModeFilter
    {
        OneVsOne,
        TwoVsTwo
    }

    public static class UIGameModeFilterExtensions
    {
        private const int OneVsOneMaxPlayers = 2;
        private const int TwoVsTwoMaxPlayers = 4;
        private const int DedicatedServerSlot = 1;

        public static int ToMaxPlayers(this UIGameModeFilter mode)
        {
            return mode switch
            {
                UIGameModeFilter.OneVsOne => OneVsOneMaxPlayers,
                UIGameModeFilter.TwoVsTwo => TwoVsTwoMaxPlayers,
                _ => OneVsOneMaxPlayers
            };
        }

        public static int ToSessionMaxPlayers(this UIGameModeFilter mode)
        {
            return mode.ToMaxPlayers() + DedicatedServerSlot;
        }

        public static int ToGamePlayerCount(int sessionPlayerCount)
        {
            var gamePlayers = sessionPlayerCount - DedicatedServerSlot;
            return gamePlayers < 0 ? 0 : gamePlayers;
        }

        public static string ToDisplayLabel(this UIGameModeFilter mode)
        {
            return mode switch
            {
                UIGameModeFilter.OneVsOne => "1v1",
                UIGameModeFilter.TwoVsTwo => "2v2",
                _ => "?"
            };
        }
    }
}
