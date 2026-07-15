using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class UIPlayerEntryLocal : UIPlayerEntry
    {
        [Header("Buttons")]
        [SerializeField] private Button readyButton;

        [Header("Text")]
        [SerializeField] private TMP_Text readyButtonText;

        [Header("References")]
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
            _selectedColorId = row.ColorId;
            _selectedDisplayColor = row.DisplayColor;

            if (readyButton != null)
                readyButton.interactable = row.CanUseReadyAction;

            if (readyButtonText != null)
                readyButtonText.text = row.IsReady ? readyLockedLabel : readyLabel;

            if (colorDropdown != null)
            {
                colorDropdown.BindOptions(_colorOptions, _selectedColorId, _selectedDisplayColor, HandleColorSelected);
                colorDropdown.interactable = row.CanUseColorAction;
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
