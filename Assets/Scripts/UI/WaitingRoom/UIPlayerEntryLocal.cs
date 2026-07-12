using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class UIPlayerEntryLocal : UIPlayerEntry
    {
        [Header("Local References")]
        [SerializeField] private Button readyButton;
        [SerializeField] private TMP_Text readyButtonText;
        [SerializeField] private UIColorDropdown colorDropdown;

        private UIViewData.ColorOptionViewData[] _colorOptions = Array.Empty<UIViewData.ColorOptionViewData>();
        private int _selectedColorId = -1;
        private Color _selectedDisplayColor = Color.white;

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

        public void BindLocal(UIWaitingRoom waitingRoom, UIViewData.PlayerRowViewData row, UIViewData.ColorOptionViewData[] colorOptions, string readyLabel, string readyLockedLabel)
        {
            _colorOptions = colorOptions ?? Array.Empty<UIViewData.ColorOptionViewData>();
            Bind(waitingRoom, row, readyLabel, readyLockedLabel);
        }

        protected override void BindRow(UIViewData.PlayerRowViewData row, string readyLabel, string readyLockedLabel)
        {
            _selectedColorId = row.colorId;
            _selectedDisplayColor = row.displayColor;

            if (readyButton != null)
                readyButton.interactable = row.canUseReadyAction;

            if (readyButtonText != null)
                readyButtonText.text = row.isReady ? readyLockedLabel : readyLabel;

            if (colorDropdown != null)
            {
                colorDropdown.BindOptions(_colorOptions, _selectedColorId, _selectedDisplayColor, HandleColorSelected);
                colorDropdown.interactable = row.canUseColorAction;
                colorDropdown.RefreshVisibility();
            }
        }

        private void HandleReadyClicked()
        {
            if (waitingRoom == null)
                return;

            waitingRoom.TryRequestReadyForPlayer(playerId);
        }

        private void HandleColorSelected(int colorId)
        {
            if (waitingRoom == null)
                return;

            if (!waitingRoom.TryRequestLocalPlayerColorChange(colorId))
            {
                RefreshColorDropdown();
                return;
            }

            if (colorDropdown != null)
                colorDropdown.Hide();
        }

        private void RefreshColorDropdown()
        {
            if (colorDropdown == null)
                return;

            colorDropdown.BindOptions(_colorOptions, _selectedColorId, _selectedDisplayColor, HandleColorSelected);
        }
    }
}
