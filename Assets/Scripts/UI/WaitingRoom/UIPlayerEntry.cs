using UnityEngine;

namespace UI
{
    public abstract class UIPlayerEntry : MonoBehaviour
    {
        [Header("Shared References")]
        [SerializeField] private TMPro.TMP_Text playerNameText;

        protected UIWaitingRoom waitingRoom { get; private set; }
        protected int playerId { get; private set; } = -1;

        public void Bind(UIWaitingRoom waitingRoom, UIViewData.PlayerRowViewData row, string readyLabel, string readyLockedLabel)
        {
            this.waitingRoom = waitingRoom;
            playerId = row.playerId;

            if (playerNameText != null)
                playerNameText.text = row.username;

            BindRow(row, readyLabel, readyLockedLabel);
        }

        protected abstract void BindRow(UIViewData.PlayerRowViewData row, string readyLabel, string readyLockedLabel);
    }
}
