using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class UIWaitingRoomColorItem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image swatchGraphic;
        [SerializeField] private GameObject selectedCheckmark;
        [SerializeField] private GameObject blockedCross;
        [SerializeField] private Toggle optionToggle;

        private Action<int> _selectionRequested;
        private int _colorId = -1;
        private bool _isSelectedByLocalPlayer;

        private void OnDisable()
        {
            if (optionToggle != null)
                optionToggle.onValueChanged.RemoveListener(HandleToggleValueChanged);
        }

        public void Bind(UIWaitingRoomViewData.ColorOptionViewData option, Action<int> selectionRequested)
        {
            _colorId = option.colorId;
            _isSelectedByLocalPlayer = option.isSelectedByLocalPlayer;
            _selectionRequested = selectionRequested;

            if (swatchGraphic != null)
                swatchGraphic.color = option.displayColor;

            if (selectedCheckmark != null)
                selectedCheckmark.SetActive(option.isSelectedByLocalPlayer);

            if (blockedCross != null)
                blockedCross.SetActive(option.isClaimed && !option.isSelectedByLocalPlayer);

            if (optionToggle != null)
            {
                optionToggle.onValueChanged.RemoveListener(HandleToggleValueChanged);
                optionToggle.SetIsOnWithoutNotify(option.isSelectedByLocalPlayer);
                optionToggle.interactable = option.isAvailableForLocalPlayer;
                optionToggle.onValueChanged.AddListener(HandleToggleValueChanged);
            }
        }

        private void HandleToggleValueChanged(bool isOn)
        {
            if (optionToggle == null)
                return;

            if (!optionToggle.interactable)
            {
                optionToggle.SetIsOnWithoutNotify(_isSelectedByLocalPlayer);
                return;
            }

            if (!isOn)
            {
                if (_isSelectedByLocalPlayer)
                    optionToggle.SetIsOnWithoutNotify(true);

                return;
            }

            if (_isSelectedByLocalPlayer)
            {
                optionToggle.SetIsOnWithoutNotify(true);
                return;
            }

            _selectionRequested?.Invoke(_colorId);
        }
    }
}
