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

        public void Bind(UIWaitingRoomViewData.ColorOptionViewData option)
        {
            if (swatchGraphic != null)
                swatchGraphic.color = option.displayColor;

            if (selectedCheckmark != null)
                selectedCheckmark.SetActive(option.isSelectedByLocalPlayer);

            if (blockedCross != null)
                blockedCross.SetActive(option.isClaimed && !option.isSelectedByLocalPlayer);

            if (optionToggle != null)
                optionToggle.interactable = option.isAvailableForLocalPlayer;

        }
    }
}
