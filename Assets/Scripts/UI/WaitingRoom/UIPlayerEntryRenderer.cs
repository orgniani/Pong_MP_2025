using System.Collections.Generic;
using UnityEngine;

namespace UI.WaitingRoom
{
    internal sealed class UIPlayerEntryRenderer
    {
        private readonly UIWaitingRoom _waitingRoom;
        private readonly UIPlayerEntry _playerEntryLocalPrefab;
        private readonly UIPlayerEntry _playerEntryDisplayPrefab;
        private readonly Transform _fallbackParent;
        private readonly Dictionary<int, PlayerEntryInstance> _playerEntries = new();

        private sealed class PlayerEntryInstance
        {
            public UIPlayerEntry entry;
            public bool usesLocalPrefab;
        }

        public UIPlayerEntryRenderer(UIWaitingRoom waitingRoom, UIPlayerEntry playerEntryLocalPrefab, UIPlayerEntry playerEntryDisplayPrefab, Transform fallbackParent)
        {
            _waitingRoom = waitingRoom;
            _playerEntryLocalPrefab = playerEntryLocalPrefab;
            _playerEntryDisplayPrefab = playerEntryDisplayPrefab;
            _fallbackParent = fallbackParent;
        }

        public void Render(UIViewData viewData, Transform leftTeamParent, Transform rightTeamParent, string readyButtonLabel, string readyLockedButtonLabel)
        {
            if (_playerEntryLocalPrefab == null && _playerEntryDisplayPrefab == null)
            {
                Clear();
                return;
            }

            var activePlayerIds = new HashSet<int>();
            RenderTeamEntries(viewData.LeftTeamRows, viewData.ColorOptions, leftTeamParent, activePlayerIds, readyButtonLabel, readyLockedButtonLabel);
            RenderTeamEntries(viewData.RightTeamRows, viewData.ColorOptions, rightTeamParent, activePlayerIds, readyButtonLabel, readyLockedButtonLabel);
            RemoveStaleEntries(activePlayerIds);
        }

        public void Clear()
        {
            foreach (var entry in _playerEntries.Values)
            {
                if (entry != null && entry.entry != null)
                    Object.Destroy(entry.entry.gameObject);
            }

            _playerEntries.Clear();
        }

        private void RenderTeamEntries(UIViewData.PlayerRowViewData[] rows, UIViewData.ColorOptionViewData[] colorOptions, Transform parent, ISet<int> activePlayerIds, string readyButtonLabel, string readyLockedButtonLabel)
        {
            var resolvedParent = parent != null ? parent : _fallbackParent;
            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (!row.HasValue)
                    continue;

                var entry = GetOrCreatePlayerEntry(row.PlayerId, row.IsLocalPlayer, resolvedParent);
                if (entry == null)
                    continue;

                if (entry.transform.parent != resolvedParent)
                    entry.transform.SetParent(resolvedParent, false);

                entry.transform.SetSiblingIndex(i);
                if (row.IsLocalPlayer && entry is UIPlayerEntryLocal localEntry)
                    localEntry.BindLocal(_waitingRoom, row, colorOptions, readyButtonLabel, readyLockedButtonLabel);
                else
                    entry.Bind(_waitingRoom, row, readyButtonLabel, readyLockedButtonLabel);

                activePlayerIds.Add(row.PlayerId);
            }
        }

        private UIPlayerEntry GetOrCreatePlayerEntry(int playerId, bool requiresLocalPrefab, Transform parent)
        {
            if (_playerEntries.TryGetValue(playerId, out var entryInstance))
            {
                if (entryInstance.entry == null)
                {
                    _playerEntries.Remove(playerId);
                }
                else if (entryInstance.usesLocalPrefab == requiresLocalPrefab)
                {
                    return entryInstance.entry;
                }
                else
                {
                    Object.Destroy(entryInstance.entry.gameObject);
                    _playerEntries.Remove(playerId);
                }
            }

            var prefab = requiresLocalPrefab ? _playerEntryLocalPrefab : _playerEntryDisplayPrefab;
            if (prefab == null)
                return null;

            var entry = Object.Instantiate(prefab, parent != null ? parent : _fallbackParent);
            _playerEntries[playerId] = new PlayerEntryInstance
            {
                entry = entry,
                usesLocalPrefab = requiresLocalPrefab
            };

            return entry;
        }

        private void RemoveStaleEntries(ISet<int> activePlayerIds)
        {
            var stalePlayerIds = new List<int>();
            foreach (var entry in _playerEntries)
            {
                if (!activePlayerIds.Contains(entry.Key) || entry.Value == null || entry.Value.entry == null)
                    stalePlayerIds.Add(entry.Key);
            }

            for (var i = 0; i < stalePlayerIds.Count; i++)
            {
                var playerId = stalePlayerIds[i];
                if (_playerEntries.TryGetValue(playerId, out var entry) && entry != null && entry.entry != null)
                    Object.Destroy(entry.entry.gameObject);

                _playerEntries.Remove(playerId);
            }
        }
    }
}
