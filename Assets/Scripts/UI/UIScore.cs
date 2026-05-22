using Fusion;
using Managers;
using TMPro;

namespace UI
{
    public class UIScore
    {
        private ScoreManager _scoreManager;
        private TMP_Text _racePositionsText;
        private TMP_Text _winnersText;

        public UIScore(ScoreManager scoreManager, TMP_Text racePositionsText, TMP_Text winnersText)
        {
            _scoreManager = scoreManager;
            _racePositionsText = racePositionsText;
            _winnersText = winnersText;
        }

        public void UpdateRacePositions()
        {
            if (_scoreManager == null || _scoreManager.Object == null)
                return;

            _racePositionsText.text = $"LEFT {_scoreManager.LeftScore} - {_scoreManager.RightScore} RIGHT";
        }

        public void UpdateWinners()
        {
            if (_scoreManager == null || _scoreManager.Object == null)
                return;

            if (_scoreManager.HasWinner(out var winnerLabel))
            {
                _winnersText.text = winnerLabel;
                return;
            }

            _winnersText.text = "Playing";
        }
    }
}
