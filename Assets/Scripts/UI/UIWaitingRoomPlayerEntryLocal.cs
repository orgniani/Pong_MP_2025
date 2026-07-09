using TMPro;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class UIWaitingRoomPlayerEntryLocal : UIWaitingRoomPlayerEntry
    {
        [Header("Local References")]
        [SerializeField] private Button readyButton;
        [SerializeField] private TMP_Text readyButtonText;
        [SerializeField] private TMP_Dropdown colorDropdown;
        [SerializeField] private Transform dropdownTemplateContentParent;
        [SerializeField] private UIWaitingRoomColorItem colorItemPrefab;

        private UIWaitingRoomViewData.ColorOptionViewData[] _colorOptions = Array.Empty<UIWaitingRoomViewData.ColorOptionViewData>();
        private int _selectedColorId = -1;
        private bool _suppressDropdownCallbacks;
        private Transform _runtimeDropdownContentParent;
        private int _runtimeDropdownItemCount = -1;

        private void OnEnable()
        {
            if (readyButton != null)
                readyButton.onClick.AddListener(HandleReadyClicked);

            if (colorDropdown != null)
                colorDropdown.onValueChanged.AddListener(HandleColorDropdownValueChanged);
        }

        private void OnDisable()
        {
            if (readyButton != null)
                readyButton.onClick.RemoveListener(HandleReadyClicked);

            if (colorDropdown != null)
                colorDropdown.onValueChanged.RemoveListener(HandleColorDropdownValueChanged);

            _runtimeDropdownContentParent = null;
            _runtimeDropdownItemCount = -1;
        }

        private void LateUpdate()
        {
            if (colorDropdown == null || _colorOptions.Length == 0)
                return;

            var runtimeContentParent = FindRuntimeDropdownContentParent();
            var runtimeChildCount = runtimeContentParent != null ? runtimeContentParent.childCount : -1;
            if (ReferenceEquals(runtimeContentParent, _runtimeDropdownContentParent) && runtimeChildCount == _runtimeDropdownItemCount)
                return;

            _runtimeDropdownContentParent = runtimeContentParent;
            _runtimeDropdownItemCount = runtimeChildCount;
            RefreshRuntimeColorItems();
        }

        protected override void BindRow(UIWaitingRoomViewData.PlayerRowViewData row, UIWaitingRoomViewData.ColorOptionViewData[] colorOptions, string readyLabel, string readyLockedLabel)
        {
            _selectedColorId = row.colorId;
            _colorOptions = colorOptions ?? Array.Empty<UIWaitingRoomViewData.ColorOptionViewData>();

            if (readyButton != null)
                readyButton.interactable = row.canUseReadyAction;

            if (readyButtonText != null)
                readyButtonText.text = row.isReady ? readyLockedLabel : readyLabel;

            if (colorDropdown != null)
            {
                colorDropdown.interactable = row.canUseColorAction;
                RebuildDropdownOptions(_colorOptions, row.colorId);
            }

            _runtimeDropdownContentParent = null;
            _runtimeDropdownItemCount = -1;
            RefreshRuntimeColorItems();
        }

        public void RebuildDropdownOptions(UIWaitingRoomViewData.ColorOptionViewData[] colorOptions, int selectedColorId)
        {
            if (colorDropdown == null)
                return;

            _colorOptions = colorOptions ?? Array.Empty<UIWaitingRoomViewData.ColorOptionViewData>();
            _selectedColorId = selectedColorId;

            EnsureTemplateColorItems();

            var options = new List<TMP_Dropdown.OptionData>(_colorOptions.Length);
            for (var i = 0; i < _colorOptions.Length; i++)
                options.Add(new TMP_Dropdown.OptionData(string.Empty));

            _suppressDropdownCallbacks = true;
            colorDropdown.options = options;

            if (_selectedColorId >= 0 && _selectedColorId < _colorOptions.Length)
                colorDropdown.SetValueWithoutNotify(_selectedColorId);
            else if (_colorOptions.Length > 0)
                colorDropdown.SetValueWithoutNotify(0);

            colorDropdown.RefreshShownValue();
            _suppressDropdownCallbacks = false;

            RefreshRuntimeColorItems();
        }

        private void EnsureTemplateColorItems()
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

            BindColorItems(dropdownTemplateContentParent);
        }

        private void HandleReadyClicked()
        {
            if (waitingRoom == null)
                return;

            waitingRoom.TryRequestReadyForPlayer(playerId);
        }

        private void HandleColorDropdownValueChanged(int optionIndex)
        {
            if (_suppressDropdownCallbacks || waitingRoom == null)
                return;

            if (!TryGetColorOptionByIndex(optionIndex, out var option) || !option.isAvailableForLocalPlayer)
            {
                RestoreSelectedDropdownValue();
                return;
            }

            if (!waitingRoom.TryRequestLocalPlayerColorChange(option.colorId))
            {
                RestoreSelectedDropdownValue();
                return;
            }

            RestoreSelectedDropdownValue();
        }

        private void RestoreSelectedDropdownValue()
        {
            if (colorDropdown == null)
                return;

            _suppressDropdownCallbacks = true;
            if (_selectedColorId >= 0 && _selectedColorId < colorDropdown.options.Count)
                colorDropdown.SetValueWithoutNotify(_selectedColorId);

            colorDropdown.RefreshShownValue();
            _suppressDropdownCallbacks = false;
            RefreshRuntimeColorItems();
        }

        private bool TryGetColorOptionByIndex(int optionIndex, out UIWaitingRoomViewData.ColorOptionViewData option)
        {
            if (optionIndex >= 0 && optionIndex < _colorOptions.Length)
            {
                option = _colorOptions[optionIndex];
                return true;
            }

            option = default;
            return false;
        }

        private void RefreshRuntimeColorItems()
        {
            if (_colorOptions.Length == 0)
                return;

            BindColorItems(dropdownTemplateContentParent);
            BindColorItems(FindRuntimeDropdownContentParent());
        }

        private void BindColorItems(Transform contentParent)
        {
            if (contentParent == null)
                return;

            var itemIndex = 0;
            for (var i = 0; i < contentParent.childCount && itemIndex < _colorOptions.Length; i++)
            {
                var child = contentParent.GetChild(i);
                var colorItem = child.GetComponent<UIWaitingRoomColorItem>();
                if (colorItem == null)
                    continue;

                colorItem.Bind(_colorOptions[itemIndex]);
                itemIndex++;
            }
        }

        private Transform FindRuntimeDropdownContentParent()
        {
            if (colorDropdown == null)
                return null;

            var rootCanvas = colorDropdown.GetComponentInParent<Canvas>();
            if (rootCanvas == null || rootCanvas.rootCanvas == null)
                return null;

            var dropdownList = FindDescendantByName(rootCanvas.rootCanvas.transform, "Dropdown List");
            if (dropdownList == null)
                return null;

            return dropdownList.Find("Viewport/Content");
        }
    }
}
