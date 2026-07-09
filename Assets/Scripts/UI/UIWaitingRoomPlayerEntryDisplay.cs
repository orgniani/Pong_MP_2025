using TMPro;
using UnityEngine;

namespace UI
{
    public sealed class UIWaitingRoomPlayerEntryDisplay : UIWaitingRoomPlayerEntry
    {
        [Header("Display References")]
        [SerializeField] private TMP_Text readyStateText;

        protected override void BindRow(UIWaitingRoomViewData.PlayerRowViewData row, UIWaitingRoomViewData.ColorOptionViewData[] colorOptions, string readyLabel, string readyLockedLabel)
        {
            if (readyStateText != null)
                readyStateText.text = row.isReady ? readyLockedLabel : readyLabel;
        }
    }
}
