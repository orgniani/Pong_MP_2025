namespace Config
{
    public interface IMatchRulesProvider
    {
        bool TryGetConfig(out MatchRulesConfig config);
    }
}
