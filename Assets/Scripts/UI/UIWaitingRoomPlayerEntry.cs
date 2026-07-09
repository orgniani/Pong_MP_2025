using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public abstract class UIWaitingRoomPlayerEntry : MonoBehaviour
    {
        [Header("Shared References")]
        [SerializeField] private TMPro.TMP_Text playerNameText;
        [SerializeField] private Image selectedColorImage;

        protected UIWaitingRoom waitingRoom { get; private set; }
        protected int playerId { get; private set; } = -1;

        public void Bind(UIWaitingRoom waitingRoom, UIWaitingRoomViewData.PlayerRowViewData row, UIWaitingRoomViewData.ColorOptionViewData[] colorOptions, string readyLabel, string readyLockedLabel)
        {
            this.waitingRoom = waitingRoom;
            playerId = row.playerId;

            if (playerNameText != null)
                playerNameText.text = row.username;

            if (selectedColorImage != null)
                selectedColorImage.color = row.displayColor;

            BindRow(row, colorOptions, readyLabel, readyLockedLabel);
        }

        protected abstract void BindRow(UIWaitingRoomViewData.PlayerRowViewData row, UIWaitingRoomViewData.ColorOptionViewData[] colorOptions, string readyLabel, string readyLockedLabel);

        protected static Transform FindDescendantByName(Transform root, string targetName)
        {
            if (root == null)
                return null;

            if (root.name == targetName)
                return root;

            for (var i = 0; i < root.childCount; i++)
            {
                var match = FindDescendantByName(root.GetChild(i), targetName);
                if (match != null)
                    return match;
            }

            return null;
        }
    }
}
