using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class UIPlayerEntryDisplay : UIPlayerEntry
    {
        [Header("Display References")]
        [SerializeField] private Image selectedColorImage;
        [SerializeField] private TMP_Text readyStateText;

        protected override void BindRow(UIViewData.PlayerRowViewData row, string readyLabel, string readyLockedLabel)
        {
            if (selectedColorImage != null)
                selectedColorImage.color = row.displayColor;

            if (readyStateText != null)
                readyStateText.text = row.isReady ? readyLockedLabel : readyLabel;
        }
    }
}
