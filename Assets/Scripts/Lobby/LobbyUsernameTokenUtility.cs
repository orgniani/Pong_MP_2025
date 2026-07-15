using System;
using System.Text;
using Fusion;

namespace Lobby
{
    internal static class LobbyUsernameTokenUtility
    {
        private const string UsernameTokenPrefix = "lobby-username:";
        private const string FallbackUsername = "Player";

        public static byte[] CreateConnectionToken(string username)
        {
            return Encoding.UTF8.GetBytes(UsernameTokenPrefix + NormalizeUsername(username));
        }

        public static string ResolveUsernameFromToken(byte[] token, PlayerRef player)
        {
            if (token == null || token.Length == 0)
                return CreateFallbackPlayerName(player);

            var rawValue = Encoding.UTF8.GetString(token);
            if (rawValue.StartsWith(UsernameTokenPrefix, StringComparison.Ordinal))
                rawValue = rawValue.Substring(UsernameTokenPrefix.Length);

            var normalized = NormalizeUsername(rawValue);
            return string.IsNullOrEmpty(normalized) ? CreateFallbackPlayerName(player) : normalized;
        }

        public static string NormalizeUsername(string username)
        {
            return string.IsNullOrWhiteSpace(username) ? FallbackUsername : username.Trim();
        }

        public static string CreateFallbackPlayerName(PlayerRef player)
        {
            return $"{FallbackUsername}_{player.PlayerId}";
        }
    }
}
