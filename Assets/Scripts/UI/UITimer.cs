using Managers;
using TMPro;
using UnityEngine;

namespace UI
{
    public class UITimer
    {
        private TimerManager _timerManager;
        private TMP_Text _timerText;

        public UITimer(TimerManager timerManager, TMP_Text timerText)
        {
            _timerManager = timerManager;
            this._timerText = timerText;
        }

        public void UpdateTimer()
        {
            if (_timerManager == null || _timerManager.Object == null)
                return;

            int minutes = Mathf.FloorToInt(_timerManager.RemainingTime / 60f);
            int seconds = Mathf.FloorToInt(_timerManager.RemainingTime % 60f);
            _timerText.text = $"TIME LEFT: {minutes:00}:{seconds:00}";
        }
    }
}