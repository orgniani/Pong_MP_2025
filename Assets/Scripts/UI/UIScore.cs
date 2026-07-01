using Helpers;
using Managers;
using TMPro;

namespace UI
{
    public class UIScore
    {
        private ScoreManager _scoreManager;
        private TMP_Text _scoreText;

        private string _leftNames = "LEFT";
        private string _rightNames = "RIGHT";

        public UIScore(ScoreManager scoreManager, TMP_Text scoreText)
        {
            _scoreManager = scoreManager;
            _scoreText = scoreText;
        }

        public void RefreshNames()
        {
            (_leftNames, _rightNames) = PlayerNameLookup.GetSideNames();
        }

        public void UpdateScore()
        {
            if (_scoreManager == null || _scoreManager.Object == null)
                return;

            _scoreText.text = $"{_leftNames} {_scoreManager.LeftScore} - {_scoreManager.RightScore} {_rightNames}";
        }
    }
}
