using Managers;
using TMPro;

namespace UI
{
    public class UIScore
    {
        private ScoreManager _scoreManager;
        private TMP_Text _scoreText;

        public UIScore(ScoreManager scoreManager, TMP_Text scoreText)
        {
            _scoreManager = scoreManager;
            _scoreText = scoreText;
        }

        public void UpdateScore()
        {
            if (_scoreManager == null || _scoreManager.Object == null)
                return;

            _scoreText.text = $"LEFT {_scoreManager.LeftScore} - {_scoreManager.RightScore} RIGHT";
        }
    }
}
