using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class UIWaitingRoomPlayerEntryLocal : UIWaitingRoomPlayerEntry
    {
        [Header("Local References")]
        [SerializeField] private Button readyButton;
        [SerializeField] private TMP_Text readyButtonText;
        [SerializeField] private UIWaitingRoomColorDropdown colorDropdown;
        [SerializeField] private Transform dropdownTemplateContentParent;
        [SerializeField] private UIWaitingRoomColorItem colorItemPrefab;

        private UIWaitingRoomViewData.ColorOptionViewData[] _colorOptions = Array.Empty<UIWaitingRoomViewData.ColorOptionViewData>();

        private void OnEnable()
        {
            if (readyButton != null)
                readyButton.onClick.AddListener(HandleReadyClicked);

            if (colorDropdown != null)
                colorDropdown.OpenStateChanged += HandleColorDropdownOpenStateChanged;
        }

        private void OnDisable()
        {
            if (readyButton != null)
                readyButton.onClick.RemoveListener(HandleReadyClicked);

            if (colorDropdown != null)
                colorDropdown.OpenStateChanged -= HandleColorDropdownOpenStateChanged;
        }

        protected override void BindRow(UIWaitingRoomViewData.PlayerRowViewData row, UIWaitingRoomViewData.ColorOptionViewData[] colorOptions, string readyLabel, string readyLockedLabel)
        {
            _colorOptions = colorOptions ?? Array.Empty<UIWaitingRoomViewData.ColorOptionViewData>();

            if (readyButton != null)
                readyButton.interactable = row.canUseReadyAction;

            if (readyButtonText != null)
                readyButtonText.text = row.isReady ? readyLockedLabel : readyLabel;

            if (colorDropdown != null)
            {
                colorDropdown.interactable = row.canUseColorAction;
                colorDropdown.RefreshVisibility();
            }

            RebuildColorItems();

            if (colorDropdown != null)
                colorDropdown.RefreshLayout();
        }

        private void RebuildColorItems()
        {
            if (dropdownTemplateContentParent == null || colorItemPrefab == null)
                return;

            var existingItems = new List<UIWaitingRoomColorItem>();
            for (var i = 0; i < dropdownTemplateContentParent.childCount; i++)
            {
                var childItem = dropdownTemplateContentParent.GetChild(i).GetComponent<UIWaitingRoomColorItem>();
                if (childItem != null)
                    existingItems.Add(childItem);
            }

            for (var i = existingItems.Count; i < _colorOptions.Length; i++)
            {
                var item = Instantiate(colorItemPrefab, dropdownTemplateContentParent);
                item.name = colorItemPrefab.name;
                existingItems.Add(item);
            }

            for (var i = existingItems.Count - 1; i >= _colorOptions.Length; i--)
                Destroy(existingItems[i].gameObject);

            BindColorItems();
        }

        private void HandleReadyClicked()
        {
            if (waitingRoom == null)
                return;

            waitingRoom.TryRequestReadyForPlayer(playerId);
        }

        private void HandleColorDropdownOpenStateChanged(bool isOpen)
        {
            if (!isOpen)
                return;

            BindColorItems();

            if (colorDropdown != null)
                colorDropdown.RefreshLayout();
        }

        private void HandleColorSelected(int colorId)
        {
            if (waitingRoom == null)
                return;

            if (!TryGetColorOptionById(colorId, out var option) || !option.isAvailableForLocalPlayer)
            {
                BindColorItems();
                return;
            }

            if (option.isSelectedByLocalPlayer)
            {
                BindColorItems();
                return;
            }

            if (!waitingRoom.TryRequestLocalPlayerColorChange(option.colorId))
            {
                BindColorItems();
                return;
            }

            if (colorDropdown != null)
                colorDropdown.Hide();

            BindColorItems();
        }

        private bool TryGetColorOptionById(int colorId, out UIWaitingRoomViewData.ColorOptionViewData option)
        {
            for (var i = 0; i < _colorOptions.Length; i++)
            {
                if (_colorOptions[i].colorId == colorId)
                {
                    option = _colorOptions[i];
                    return true;
                }
            }

            option = default;
            return false;
        }

        private void BindColorItems()
        {
            if (dropdownTemplateContentParent == null)
                return;

            var itemIndex = 0;
            for (var i = 0; i < dropdownTemplateContentParent.childCount && itemIndex < _colorOptions.Length; i++)
            {
                var child = dropdownTemplateContentParent.GetChild(i);
                var colorItem = child.GetComponent<UIWaitingRoomColorItem>();
                if (colorItem == null)
                    continue;

                colorItem.Bind(_colorOptions[itemIndex], HandleColorSelected);
                itemIndex++;
            }
        }
    }
}
