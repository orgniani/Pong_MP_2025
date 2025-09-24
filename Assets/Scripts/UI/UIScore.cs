using Fusion;
using Managers;
using System.Text;
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


            var playerOrder = _scoreManager.GetCurrentPlayerScore();
            if (playerOrder.Count == 0) return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Positions:");

            for (int i = 0; i < playerOrder.Count; i++)
            {
                string playerName = GetPlayerName(playerOrder[i]);
                sb.AppendLine($"{i + 1} {playerName}");
            }

            _racePositionsText.text = sb.ToString();
        }

        public void UpdateWinners()
        {
            if (_scoreManager == null || _scoreManager.Object == null)
                return;

            var winners = _scoreManager.GetWinnersOrder();
            if (winners.Count == 0)
            {
                _winnersText.text = "No winners yet!";
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("WINNERS");

            for (int i = 0; i < winners.Count; i++)
            {
                string playerName = GetPlayerName(winners[i]);
                sb.AppendLine($"{i + 1} {playerName}");
            }

            _winnersText.text = sb.ToString();
        }

        private string GetPlayerName(PlayerRef player)
        {
            //if (NetworkPlayerSetup.PlayerNames.TryGetValue(player, out var name))
            //{
            //    if (!string.IsNullOrWhiteSpace(name))
            //        return name;
            //    else
            //        return "Player_" + player.PlayerId;
            //}

            return "Player_" + player.PlayerId;
        }
    }
}