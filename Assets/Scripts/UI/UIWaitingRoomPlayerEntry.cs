using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UIWaitingRoomPlayerEntry : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private Button readyButton;
        [SerializeField] private TMP_Text readyButtonText;
        [SerializeField] private Graphic[] colorSurfaces;

        private UIWaitingRoom _waitingRoom;
        private int _playerId = -1;

        private void OnEnable()
        {
            if (readyButton != null)
                readyButton.onClick.AddListener(HandleReadyClicked);
        }

        private void OnDisable()
        {
            if (readyButton != null)
                readyButton.onClick.RemoveListener(HandleReadyClicked);
        }

        public void Bind(UIWaitingRoom waitingRoom, UIWaitingRoomViewData.PlayerRowViewData row, string readyLabel, string readyLockedLabel)
        {
            _waitingRoom = waitingRoom;
            _playerId = row.playerId;

            if (playerNameText != null)
                playerNameText.text = row.username;

            var readyText = row.isReady ? readyLockedLabel : readyLabel;

            if (readyButton != null)
                readyButton.interactable = row.canUseReadyAction;

            if (readyButtonText != null)
                readyButtonText.text = readyText;

            if (colorSurfaces != null)
            {
                for (var i = 0; i < colorSurfaces.Length; i++)
                {
                    if (colorSurfaces[i] != null)
                        colorSurfaces[i].color = row.displayColor;
                }
            }
        }

        private void HandleReadyClicked()
        {
            if (_waitingRoom == null)
                return;

            _waitingRoom.TryRequestReadyForPlayer(_playerId);
        }
    }
}
