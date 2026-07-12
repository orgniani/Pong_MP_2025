using Config;

namespace Common
{
    public enum MatchMode
    {
        OneVsOne,
        TwoVsTwo
    }

    public static class MatchModeExtensions
    {
        private const int OneVsOneMaxPlayers = 2;
        public const int TwoVsTwoMaxPlayers = 4;

        public static int ToMaxPlayers(this MatchMode mode)
        {
            return mode switch
            {
                MatchMode.OneVsOne => OneVsOneMaxPlayers,
                MatchMode.TwoVsTwo => TwoVsTwoMaxPlayers,
                _ => OneVsOneMaxPlayers
            };
        }

        public static int ToSessionMaxPlayers(this MatchMode mode)
        {
            return mode.ToMaxPlayers() + MatchRules.GetDedicatedServerSlots();
        }

        public static int ToGamePlayerCount(int sessionPlayerCount)
        {
            return MatchRules.ToGamePlayerCount(sessionPlayerCount);
        }

        public static string ToDisplayLabel(this MatchMode mode)
        {
            return mode switch
            {
                MatchMode.OneVsOne => "1v1",
                MatchMode.TwoVsTwo => "2v2",
                _ => "?"
            };
        }
    }
}
