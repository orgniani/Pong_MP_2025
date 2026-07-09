using System;
using UnityEngine;

namespace UI
{
    public sealed class UIWaitingRoomViewData
    {
        public readonly struct PlayerRowViewData
        {
            public PlayerRowViewData(bool hasValue, int playerId, string username, int teamId, int laneId, int colorId, Color displayColor, bool isReady, bool isLocalPlayer, bool canUseReadyAction, bool canUseColorAction)
            {
                this.hasValue = hasValue;
                this.playerId = playerId;
                this.username = username ?? string.Empty;
                this.teamId = teamId;
                this.laneId = laneId;
                this.colorId = colorId;
                this.displayColor = displayColor;
                this.isReady = isReady;
                this.isLocalPlayer = isLocalPlayer;
                this.canUseReadyAction = canUseReadyAction;
                this.canUseColorAction = canUseColorAction;
            }

            public bool hasValue { get; }
            public int playerId { get; }
            public string username { get; }
            public int teamId { get; }
            public int laneId { get; }
            public int colorId { get; }
            public Color displayColor { get; }
            public bool isReady { get; }
            public bool isLocalPlayer { get; }
            public bool canUseReadyAction { get; }
            public bool canUseColorAction { get; }
        }

        public readonly struct ColorOptionViewData
        {
            public ColorOptionViewData(int colorId, Color displayColor, bool isClaimed, int claimedByPlayerId, bool isSelectedByLocalPlayer, bool isAvailableForLocalPlayer)
            {
                this.colorId = colorId;
                this.displayColor = displayColor;
                this.isClaimed = isClaimed;
                this.claimedByPlayerId = claimedByPlayerId;
                this.isSelectedByLocalPlayer = isSelectedByLocalPlayer;
                this.isAvailableForLocalPlayer = isAvailableForLocalPlayer;
            }

            public int colorId { get; }
            public Color displayColor { get; }
            public bool isClaimed { get; }
            public int claimedByPlayerId { get; }
            public bool isSelectedByLocalPlayer { get; }
            public bool isAvailableForLocalPlayer { get; }
        }

        private static readonly PlayerRowViewData EmptyPlayerRow = new(false, -1, string.Empty, 0, 0, -1, Color.white, false, false, false, false);

        public UIWaitingRoomViewData(PlayerRowViewData[] allRows, PlayerRowViewData[] leftTeamRows, PlayerRowViewData[] rightTeamRows, ColorOptionViewData[] colorOptions, int localPlayerId, bool isLocalPlayerReady, int currentPlayerCount, int targetPlayerCapacity)
        {
            this.allRows = allRows ?? Array.Empty<PlayerRowViewData>();
            this.leftTeamRows = leftTeamRows ?? Array.Empty<PlayerRowViewData>();
            this.rightTeamRows = rightTeamRows ?? Array.Empty<PlayerRowViewData>();
            this.colorOptions = colorOptions ?? Array.Empty<ColorOptionViewData>();
            LocalPlayerId = localPlayerId;
            IsLocalPlayerReady = isLocalPlayerReady;
            CurrentPlayerCount = currentPlayerCount;
            TargetPlayerCapacity = targetPlayerCapacity;
        }

        public PlayerRowViewData[] allRows { get; }
        public PlayerRowViewData[] leftTeamRows { get; }
        public PlayerRowViewData[] rightTeamRows { get; }
        public ColorOptionViewData[] colorOptions { get; }
        public int LocalPlayerId { get; }
        public bool IsLocalPlayerReady { get; }
        public int CurrentPlayerCount { get; }
        public int TargetPlayerCapacity { get; }
        public bool HasPlayers => allRows.Length > 0;

        public PlayerRowViewData LocalPlayerRow
        {
            get
            {
                return TryGetRow(LocalPlayerId, out var row) ? row : EmptyPlayerRow;
            }
        }

        public bool TryGetRow(int playerId, out PlayerRowViewData row)
        {
            for (var i = 0; i < allRows.Length; i++)
            {
                if (allRows[i].playerId == playerId)
                {
                    row = allRows[i];
                    return true;
                }
            }

            row = EmptyPlayerRow;
            return false;
        }

        public bool TryGetColorOption(int colorId, out ColorOptionViewData option)
        {
            if (colorOptions != null)
            {
                for (var i = 0; i < colorOptions.Length; i++)
                {
                    if (colorOptions[i].colorId == colorId)
                    {
                        option = colorOptions[i];
                        return true;
                    }
                }
            }

            option = default;
            return false;
        }

        public static UIWaitingRoomViewData CreateEmpty()
        {
            return new UIWaitingRoomViewData(Array.Empty<PlayerRowViewData>(), Array.Empty<PlayerRowViewData>(), Array.Empty<PlayerRowViewData>(), Array.Empty<ColorOptionViewData>(), -1, false, 0, 0);
        }
    }
}
