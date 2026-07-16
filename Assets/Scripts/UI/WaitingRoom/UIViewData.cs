using System;
using UnityEngine;

namespace UI.WaitingRoom
{
    public sealed class UIViewData
    {
        public readonly struct PlayerRowViewData
        {
            public PlayerRowViewData(bool hasValue, int playerId, string username, int teamId, int laneId, int colorId, Color displayColor, bool isReady, bool isLocalPlayer, bool canUseReadyAction, bool canUseColorAction)
            {
                HasValue = hasValue;
                PlayerId = playerId;
                Username = username ?? string.Empty;
                TeamId = teamId;
                LaneId = laneId;
                ColorId = colorId;
                DisplayColor = displayColor;
                IsReady = isReady;
                IsLocalPlayer = isLocalPlayer;
                CanUseReadyAction = canUseReadyAction;
                CanUseColorAction = canUseColorAction;
            }

            public bool HasValue { get; }
            public int PlayerId { get; }
            public string Username { get; }
            public int TeamId { get; }
            public int LaneId { get; }
            public int ColorId { get; }
            public Color DisplayColor { get; }
            public bool IsReady { get; }
            public bool IsLocalPlayer { get; }
            public bool CanUseReadyAction { get; }
            public bool CanUseColorAction { get; }
        }

        public readonly struct ColorOptionViewData
        {
            public ColorOptionViewData(int colorId, Color displayColor, bool isClaimed, int claimedByPlayerId, bool isAvailableForLocalPlayer)
            {
                ColorId = colorId;
                DisplayColor = displayColor;
                IsClaimed = isClaimed;
                ClaimedByPlayerId = claimedByPlayerId;
                IsAvailableForLocalPlayer = isAvailableForLocalPlayer;
            }

            public int ColorId { get; }
            public Color DisplayColor { get; }
            public bool IsClaimed { get; }
            public int ClaimedByPlayerId { get; }
            public bool IsAvailableForLocalPlayer { get; }
        }

        private static readonly PlayerRowViewData EmptyPlayerRow = new(false, -1, string.Empty, 0, 0, -1, Color.white, false, false, false, false);

        public UIViewData(PlayerRowViewData[] allRows, PlayerRowViewData[] leftTeamRows, PlayerRowViewData[] rightTeamRows, ColorOptionViewData[] colorOptions, int localPlayerId, bool isLocalPlayerReady, int currentPlayerCount, int targetPlayerCapacity)
        {
            AllRows = allRows;
            LeftTeamRows = leftTeamRows;
            RightTeamRows = rightTeamRows;
            ColorOptions = colorOptions;
            LocalPlayerId = localPlayerId;
            IsLocalPlayerReady = isLocalPlayerReady;
            CurrentPlayerCount = currentPlayerCount;
            TargetPlayerCapacity = targetPlayerCapacity;
        }

        public PlayerRowViewData[] AllRows { get; }
        public PlayerRowViewData[] LeftTeamRows { get; }
        public PlayerRowViewData[] RightTeamRows { get; }
        public ColorOptionViewData[] ColorOptions { get; }
        public int LocalPlayerId { get; }
        public bool IsLocalPlayerReady { get; }
        public int CurrentPlayerCount { get; }
        public int TargetPlayerCapacity { get; }
        public bool HasPlayers => AllRows.Length > 0;

        public PlayerRowViewData LocalPlayerRow
        {
            get
            {
                return TryGetRow(LocalPlayerId, out var row) ? row : EmptyPlayerRow;
            }
        }

        public bool TryGetRow(int playerId, out PlayerRowViewData row)
        {
            for (var i = 0; i < AllRows.Length; i++)
            {
                if (AllRows[i].PlayerId == playerId)
                {
                    row = AllRows[i];
                    return true;
                }
            }

            row = EmptyPlayerRow;
            return false;
        }

        public bool TryGetColorOption(int colorId, out ColorOptionViewData option)
        {
            for (var i = 0; i < ColorOptions.Length; i++)
            {
                if (ColorOptions[i].ColorId == colorId)
                {
                    option = ColorOptions[i];
                    return true;
                }
            }

            option = default;
            return false;
        }

        public static UIViewData CreateEmpty()
        {
            return new UIViewData(
                allRows: Array.Empty<PlayerRowViewData>(),
                leftTeamRows: Array.Empty<PlayerRowViewData>(),
                rightTeamRows: Array.Empty<PlayerRowViewData>(),
                colorOptions: Array.Empty<ColorOptionViewData>(),
                localPlayerId: -1,
                isLocalPlayerReady: false,
                currentPlayerCount: 0,
                targetPlayerCapacity: 0);
        }
    }
}
