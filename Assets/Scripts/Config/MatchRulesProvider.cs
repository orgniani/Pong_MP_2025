namespace Config
{
    public sealed class MatchRulesProvider : IMatchRulesProvider
    {
        private readonly MatchRulesConfig _config;

        public MatchRulesProvider(MatchRulesConfig config)
        {
            _config = config;
        }

        public bool TryGetConfig(out MatchRulesConfig config)
        {
            config = _config;
            return config != null;
        }
    }
}
