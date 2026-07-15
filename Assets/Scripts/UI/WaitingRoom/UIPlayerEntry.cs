using Helpers;
using UnityEngine;

namespace UI.WaitingRoom
{
    public abstract class UIPlayerEntry : MonoBehaviour
    {
        [SerializeField] private TMPro.TMP_Text playerNameText;

        protected UIWaitingRoom waitingRoom { get; private set; }
        protected int playerId { get; private set; } = -1;

        protected virtual void Awake()
        {
            ReferenceValidator.ValidateOptional(playerNameText, nameof(playerNameText), this);
        }

        public void Bind(UIWaitingRoom waitingRoom, UIViewData.PlayerRowViewData row, string readyLabel, string readyLockedLabel)
        {
            this.waitingRoom = waitingRoom;
            playerId = row.PlayerId;

            if (playerNameText != null)
                playerNameText.text = row.Username;

            BindRow(row, readyLabel, readyLockedLabel);
        }

        protected abstract void BindRow(UIViewData.PlayerRowViewData row, string readyLabel, string readyLockedLabel);
    }
}
