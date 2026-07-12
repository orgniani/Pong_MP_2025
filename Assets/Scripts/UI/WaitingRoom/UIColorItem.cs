using System;
using Helpers;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class UIColorItem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image swatchGraphic;
        [SerializeField] private GameObject selectedCheckmark;
        [SerializeField] private GameObject blockedCross;
        [SerializeField] private Toggle optionToggle;

        private Action<int> _selectionRequested;
        private int _colorId = -1;
        private bool _isSelected;

        private void Awake()
        {
            ReferenceValidator.ValidateOptional(swatchGraphic, nameof(swatchGraphic), this);
            ReferenceValidator.ValidateOptional(selectedCheckmark, nameof(selectedCheckmark), this);
            ReferenceValidator.ValidateOptional(blockedCross, nameof(blockedCross), this);
            ReferenceValidator.ValidateOptional(optionToggle, nameof(optionToggle), this);
        }

        private void OnDisable()
        {
            if (optionToggle != null)
                optionToggle.onValueChanged.RemoveListener(HandleToggleValueChanged);
        }

        public void Bind(UIViewData.ColorOptionViewData option, bool isSelected, Action<int> selectionRequested)
        {
            _colorId = option.colorId;
            _isSelected = isSelected;
            _selectionRequested = selectionRequested;

            if (swatchGraphic != null)
                swatchGraphic.color = option.displayColor;

            if (selectedCheckmark != null)
                selectedCheckmark.SetActive(_isSelected);

            if (blockedCross != null)
                blockedCross.SetActive(option.isClaimed && !_isSelected);

            if (optionToggle != null)
            {
                optionToggle.onValueChanged.RemoveListener(HandleToggleValueChanged);
                optionToggle.SetIsOnWithoutNotify(_isSelected);
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
                optionToggle.SetIsOnWithoutNotify(_isSelected);
                return;
            }

            if (!isOn)
            {
                if (_isSelected)
                    optionToggle.SetIsOnWithoutNotify(true);

                return;
            }

            if (_isSelected)
            {
                optionToggle.SetIsOnWithoutNotify(true);
                return;
            }

            _selectionRequested?.Invoke(_colorId);
        }
    }
}
