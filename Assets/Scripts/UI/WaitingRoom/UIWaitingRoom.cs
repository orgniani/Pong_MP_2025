using Helpers;
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

        [Header("Prefabs")]
        [SerializeField] private UIPlayerEntry playerEntryLocalPrefab;
        [SerializeField] private UIPlayerEntry playerEntryDisplayPrefab;

        [Header("References")]
        [SerializeField] private Transform leftTeamParent;
        [SerializeField] private Transform rightTeamParent;

        [Header("Text")]
        [SerializeField] private TMP_Text waitingStatusText;

        [Header("Buttons")]
        [SerializeField] private Button leaveButton;

        [Header("Config")]
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

        private void Awake()
        {
            ReferenceValidator.ValidateOptional(playerEntryLocalPrefab, nameof(playerEntryLocalPrefab), this);
            ReferenceValidator.ValidateOptional(playerEntryDisplayPrefab, nameof(playerEntryDisplayPrefab), this);
            ReferenceValidator.ValidateOptional(leftTeamParent, nameof(leftTeamParent), this);
            ReferenceValidator.ValidateOptional(rightTeamParent, nameof(rightTeamParent), this);
            ReferenceValidator.ValidateOptional(waitingStatusText, nameof(waitingStatusText), this);
            ReferenceValidator.ValidateOptional(leaveButton, nameof(leaveButton), this);
        }

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
                RefreshView(ResolveCurrentSnapshot());
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

            RefreshView(LobbySessionSnapshot.Empty);
        }

        public void Configure(PaddleColorPalette paddleColorPalette)
        {
            _paddleColorPalette = paddleColorPalette;
            RefreshView(ResolveCurrentSnapshot());
        }

        public void Unbind()
        {
            if (_lobbySessionState != null)
                _lobbySessionState.SnapshotChanged -= RefreshView;

            _lobbySessionState = null;
            RefreshView(LobbySessionSnapshot.Empty);
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

        private LobbySessionSnapshot ResolveCurrentSnapshot()
        {
            return _lobbySessionState != null ? _lobbySessionState.CurrentSnapshot : LobbySessionSnapshot.Empty;
        }

        public bool TryRequestReadyForPlayer(int playerId)
        {
            if (_lobbySessionState == null)
                return false;

            var localRow = CurrentViewData.LocalPlayerRow;
            if (!localRow.HasValue || localRow.PlayerId != playerId || !localRow.CanUseReadyAction)
                return false;

            _lobbySessionState.RequestLocalPlayerReadyLock();
            return true;
        }

        public bool TryRequestLocalPlayerColorChange(int colorId)
        {
            if (_lobbySessionState == null)
                return false;

            var localRow = CurrentViewData.LocalPlayerRow;
            if (!localRow.HasValue || !localRow.CanUseColorAction)
                return false;

            if (localRow.ColorId == colorId)
                return false;

            if (!CurrentViewData.TryGetColorOption(colorId, out var option) || !option.IsAvailableForLocalPlayer)
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
            var rowCount = snapshot.PlayerCount;
            var allRows = new UIViewData.PlayerRowViewData[rowCount];
            var leftRows = new List<UIViewData.PlayerRowViewData>(rowCount);
            var rightRows = new List<UIViewData.PlayerRowViewData>(rowCount);
            var claimedColors = new Dictionary<int, int>();
            var localPlayerId = snapshot.LocalPlayerId;
            var localPlayerColorId = -1;

            for (var i = 0; i < rowCount; i++)
            {
                var player = snapshot.GetPlayerSlot(i);
                var isLocalPlayer = player.PlayerId == localPlayerId && player.PlayerId >= 0;
                var row = new UIViewData.PlayerRowViewData(
                    true,
                    player.PlayerId,
                    player.Username,
                    player.TeamId,
                    player.LaneId,
                    player.ColorId,
                    ResolveDisplayColor(player.ColorId),
                    player.IsReady,
                    isLocalPlayer,
                    isLocalPlayer && !player.IsReady,
                    isLocalPlayer);

                allRows[i] = row;
                if (player.TeamId == (int)TeamSide.Right)
                    rightRows.Add(row);
                else
                    leftRows.Add(row);

                if (player.ColorId >= 0 && !claimedColors.ContainsKey(player.ColorId))
                    claimedColors.Add(player.ColorId, player.PlayerId);

                if (isLocalPlayer)
                    localPlayerColorId = player.ColorId;
            }

            var colorOptions = BuildColorOptions(claimedColors, localPlayerColorId);
            return new UIViewData(
                allRows: allRows,
                leftTeamRows: leftRows.ToArray(),
                rightTeamRows: rightRows.ToArray(),
                colorOptions: colorOptions,
                localPlayerId: localPlayerId,
                isLocalPlayerReady: snapshot.IsLocalPlayerReady,
                currentPlayerCount: snapshot.CurrentPlayerCount,
                targetPlayerCapacity: snapshot.TargetPlayerCapacity);
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
            RefreshTeamEntries(viewData.LeftTeamRows, viewData.ColorOptions, leftTeamParent, activePlayerIds);
            RefreshTeamEntries(viewData.RightTeamRows, viewData.ColorOptions, rightTeamParent, activePlayerIds);
            RemoveStaleEntries(activePlayerIds);
        }

        private void RefreshTeamEntries(UIViewData.PlayerRowViewData[] rows, UIViewData.ColorOptionViewData[] colorOptions, Transform parent, ISet<int> activePlayerIds)
        {
            var resolvedParent = parent != null ? parent : transform;
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
                    localEntry.BindLocal(this, row, colorOptions, readyButtonLabel, readyLockedButtonLabel);
                else
                    entry.Bind(this, row, readyButtonLabel, readyLockedButtonLabel);

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
