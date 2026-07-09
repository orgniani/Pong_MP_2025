using System.Collections.Generic;
using Players;
using UnityEngine;

namespace Helpers
{
    public static class PlayerNameLookup
    {
        private static string _cachedLeftName = "LEFT";
        private static string _cachedRightName = "RIGHT";
        private static bool _cachedSideNamesFrozen;

        public static (string left, string right) GetSideNames()
        {
            if (_cachedSideNamesFrozen)
                return (_cachedLeftName, _cachedRightName);

            var players = Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
            var left = new List<string>();
            var right = new List<string>();

            foreach (var p in players)
            {
                if (p.Object == null || !p.Object.IsValid)
                    continue;

                var name = p.Username;
                if (string.IsNullOrWhiteSpace(name))
                    name = $"PLAYER {p.SpawnPointIndex + 1}";

                var side = p.transform.position.x < 0f ? left : right;
                side.Add(name.ToUpperInvariant());
            }

            if (left.Count > 0)
                _cachedLeftName = string.Join(" & ", left);

            if (right.Count > 0)
                _cachedRightName = string.Join(" & ", right);

            return (
                _cachedLeftName,
                _cachedRightName
            );
        }

        public static void FreezeCachedSideNames()
        {
            if (_cachedSideNamesFrozen)
                return;

            GetSideNames();
            _cachedSideNamesFrozen = true;
        }

        public static void ResetCachedSideNames()
        {
            _cachedLeftName = "LEFT";
            _cachedRightName = "RIGHT";
            _cachedSideNamesFrozen = false;
        }
    }
}
