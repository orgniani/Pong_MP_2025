using System.Collections.Generic;
using Players;
using UnityEngine;

namespace Helpers
{
    public static class PlayerNameLookup
    {
        public static (string left, string right) GetSideNames()
        {
            var players = Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
            var left = new List<string>();
            var right = new List<string>();

            foreach (var p in players)
            {
                var name = p.Username.ToString();
                if (string.IsNullOrWhiteSpace(name))
                    name = $"PLAYER {p.spawnPointIndex + 1}";

                var side = p.transform.position.x < 0f ? left : right;
                side.Add(name.ToUpperInvariant());
            }

            return (
                left.Count > 0 ? string.Join(" & ", left) : "LEFT",
                right.Count > 0 ? string.Join(" & ", right) : "RIGHT"
            );
        }
    }
}
