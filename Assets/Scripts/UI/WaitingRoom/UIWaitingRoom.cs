using Managers.Network;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Config;


namespace UI
{
    public class UIWaitingRoom : MonoBehaviour
    {
        public event Action Enabled;
        public event Action<UIViewData> ViewDataChanged;

        [Header("References")]
        [SerializeField] private UIPlayerEntry playerEntryLocalPrefab;
        [SerializeField] private UIPlayerEntry playerEntryDisplayPrefab;
        [SerializeField] private Transform leftTeamParent;
        [SerializeField] private Transform rightTeamParent;
        [SerializeField] private TMP_Text waitingStatusText;
        [SerializeField] private Button leaveButton;

        [Header("Content")]
        [SerializeField] private string waitingStatusPrefix = "Waiting for more players";
        [SerializeField] private string readyButtonLabel = "Ready";
        [SerializeField] private string readyLockedButtonLabel = "Ready Locked";

        private static readonly UIViewData EmptyViewData = UIViewData.CreateEmpty();
        private LobbySessionState _lobbySessionState;
        private readonly Dictionary<int, PlayerEntryInstance> _playerEntries = new();
        private PaddleColorPalette _paddleColorPalette;

        private sealed class PlayerEntryInstance
        {
            public UIPlayerEntry entry;
            public bool usesLocalPrefab;
        }

        public UIViewData CurrentViewData { get; private set; } = EmptyViewData;

        private void OnEnable()
        {
            if (leaveButton != null)
                leaveButton.onClick.AddListener(HandleLeaveClicked);

            Enabled?.Invoke();
        }

        private void OnDisable()
        {
            if (leaveButton != null)
                leaveButton.onClick.RemoveListener(HandleLeaveClicked);

            Unbind();
        }

        public void Bind(LobbySessionState lobbySessionState)
        {
            if (ReferenceEquals(_lobbySessionState, lobbySessionState))
            {
                RefreshView(_lobbySessionState != null ? _lobbySessionState.CurrentSnapshot : CreateEmptySnapshot());
                return;
            }

            Unbind();
            _lobbySessionState = lobbySessionState;

            if (_lobbySessionState != null)
            {
                _lobbySessionState.SnapshotChanged += RefreshView;
                RefreshView(_lobbySessionState.CurrentSnapshot);
                return;
            }

            RefreshView(CreateEmptySnapshot());
        }

        public void Configure(PaddleColorPalette paddleColorPalette)
        {
            _paddleColorPalette = paddleColorPalette;
            RefreshView(_lobbySessionState != null ? _lobbySessionState.CurrentSnapshot : CreateEmptySnapshot());
        }

        public void Unbind()
        {
            if (_lobbySessionState != null)
                _lobbySessionState.SnapshotChanged -= RefreshView;

            _lobbySessionState = null;
            RefreshView(CreateEmptySnapshot());
        }

        private void RefreshView(LobbySessionSnapshot snapshot)
        {
            CurrentViewData = BuildViewData(snapshot);
            ViewDataChanged?.Invoke(CurrentViewData);

            if (waitingStatusText != null)
            {
                var counter = $"{CurrentViewData.CurrentPlayerCount}/{CurrentViewData.TargetPlayerCapacity}";
                waitingStatusText.text = string.IsNullOrWhiteSpace(waitingStatusPrefix)
                    ? counter
                    : $"{waitingStatusPrefix.Trim()} ({counter})";
            }

            RefreshPlayerEntries(CurrentViewData);
        }

        private void HandleLeaveClicked()
        {
            SessionExitToMainMenu.Execute("[UIWaitingRoom]");
        }

        private static LobbySessionSnapshot CreateEmptySnapshot()
        {
            return new LobbySessionSnapshot(Array.Empty<string>(), Array.Empty<int>(), Array.Empty<bool>(), Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>(), -1, false, 0, 0);
        }

        public bool TryRequestReadyForPlayer(int playerId)
        {
            if (_lobbySessionState == null)
                return false;

            var localRow = CurrentViewData.LocalPlayerRow;
            if (!localRow.hasValue || localRow.playerId != playerId || !localRow.canUseReadyAction)
                return false;

            _lobbySessionState.RequestLocalPlayerReadyLock();
            return true;
        }

        public bool TryRequestLocalPlayerColorChange(int colorId)
        {
            if (_lobbySessionState == null)
                return false;

            var localRow = CurrentViewData.LocalPlayerRow;
            if (!localRow.hasValue || !localRow.canUseColorAction)
                return false;

            if (localRow.colorId == colorId)
                return false;

            if (!CurrentViewData.TryGetColorOption(colorId, out var option) || !option.isAvailableForLocalPlayer)
                return false;

            _lobbySessionState.RequestLocalPlayerColorChange(colorId);
            return true;
        }

        public bool TryGetRowViewData(int playerId, out UIViewData.PlayerRowViewData row)
        {
            return CurrentViewData.TryGetRow(playerId, out row);
        }

        private UIViewData BuildViewData(LobbySessionSnapshot snapshot)
        {
            var rowCount = snapshot.WaitingUsernames != null ? snapshot.WaitingUsernames.Length : 0;
            var allRows = new UIViewData.PlayerRowViewData[rowCount];
            var leftRows = new List<UIViewData.PlayerRowViewData>(rowCount);
            var rightRows = new List<UIViewData.PlayerRowViewData>(rowCount);
            var claimedColors = new Dictionary<int, int>();
            var localPlayerId = snapshot.LocalPlayerId;
            var localPlayerColorId = -1;

            for (var i = 0; i < rowCount; i++)
            {
                var playerId = snapshot.PlayerIds != null && i < snapshot.PlayerIds.Length ? snapshot.PlayerIds[i] : -1;
                var username = snapshot.WaitingUsernames[i] ?? string.Empty;
                var isReady = snapshot.ReadyStates != null && i < snapshot.ReadyStates.Length && snapshot.ReadyStates[i];
                var teamId = snapshot.TeamIds != null && i < snapshot.TeamIds.Length ? snapshot.TeamIds[i] : 0;
                var laneId = snapshot.LaneIds != null && i < snapshot.LaneIds.Length ? snapshot.LaneIds[i] : 0;
                var colorId = snapshot.ColorIds != null && i < snapshot.ColorIds.Length ? snapshot.ColorIds[i] : -1;
                var isLocalPlayer = playerId == localPlayerId && playerId >= 0;
                var row = new UIViewData.PlayerRowViewData(
                    true,
                    playerId,
                    username,
                    teamId,
                    laneId,
                    colorId,
                    ResolveDisplayColor(colorId),
                    isReady,
                    isLocalPlayer,
                    isLocalPlayer && !isReady,
                    isLocalPlayer);

                allRows[i] = row;
                if (teamId == (int)TeamSide.Right)
                    rightRows.Add(row);
                else
                    leftRows.Add(row);

                if (colorId >= 0 && !claimedColors.ContainsKey(colorId))
                    claimedColors.Add(colorId, playerId);

                if (isLocalPlayer)
                    localPlayerColorId = colorId;
            }

            var colorOptions = BuildColorOptions(claimedColors, localPlayerColorId);
            return new UIViewData(allRows, leftRows.ToArray(), rightRows.ToArray(), colorOptions, localPlayerId, snapshot.IsLocalPlayerReady, snapshot.CurrentPlayerCount, snapshot.TargetPlayerCapacity);
        }

        private UIViewData.ColorOptionViewData[] BuildColorOptions(IReadOnlyDictionary<int, int> claimedColors, int localPlayerColorId)
        {
            if (_paddleColorPalette == null || _paddleColorPalette.Count <= 0)
                return Array.Empty<UIViewData.ColorOptionViewData>();

            var options = new UIViewData.ColorOptionViewData[_paddleColorPalette.Count];
            for (var colorId = 0; colorId < _paddleColorPalette.Count; colorId++)
            {
                var isClaimed = claimedColors.TryGetValue(colorId, out var claimedByPlayerId);
                options[colorId] = new UIViewData.ColorOptionViewData(
                    colorId,
                    _paddleColorPalette.ResolveColor(colorId),
                    isClaimed,
                    isClaimed ? claimedByPlayerId : -1,
                    !isClaimed || colorId == localPlayerColorId);
            }

            return options;
        }

        private Color ResolveDisplayColor(int colorId)
        {
            if (_paddleColorPalette == null)
                return Color.white;

            return _paddleColorPalette.ResolveColor(colorId);
        }

        private void RefreshPlayerEntries(UIViewData viewData)
        {
            if (playerEntryLocalPrefab == null && playerEntryDisplayPrefab == null)
            {
                ClearPlayerEntries();
                return;
            }

            var activePlayerIds = new HashSet<int>();
            RefreshTeamEntries(viewData.leftTeamRows, viewData.colorOptions, leftTeamParent, activePlayerIds);
            RefreshTeamEntries(viewData.rightTeamRows, viewData.colorOptions, rightTeamParent, activePlayerIds);
            RemoveStaleEntries(activePlayerIds);
        }

        private void RefreshTeamEntries(UIViewData.PlayerRowViewData[] rows, UIViewData.ColorOptionViewData[] colorOptions, Transform parent, ISet<int> activePlayerIds)
        {
            if (rows == null)
                return;

            var resolvedParent = parent != null ? parent : transform;
            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                if (!row.hasValue)
                    continue;

                var entry = GetOrCreatePlayerEntry(row.playerId, row.isLocalPlayer, resolvedParent);
                if (entry == null)
                    continue;

                if (entry.transform.parent != resolvedParent)
                    entry.transform.SetParent(resolvedParent, false);

                entry.transform.SetSiblingIndex(i);
                if (row.isLocalPlayer && entry is UIPlayerEntryLocal localEntry)
                    localEntry.BindLocal(this, row, colorOptions, readyButtonLabel, readyLockedButtonLabel);
                else
                    entry.Bind(this, row, readyButtonLabel, readyLockedButtonLabel);

                activePlayerIds.Add(row.playerId);
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
                    Destroy(entryInstance.entry.gameObject);
                    _playerEntries.Remove(playerId);
                }
            }

            var prefab = requiresLocalPrefab ? playerEntryLocalPrefab : playerEntryDisplayPrefab;
            if (prefab == null)
                return null;

            var entry = Instantiate(prefab, parent != null ? parent : transform);
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
                    Destroy(entry.entry.gameObject);

                _playerEntries.Remove(playerId);
            }
        }

        private void ClearPlayerEntries()
        {
            foreach (var entry in _playerEntries.Values)
            {
                if (entry != null && entry.entry != null)
                    Destroy(entry.entry.gameObject);
            }

            _playerEntries.Clear();
        }
    }
}
