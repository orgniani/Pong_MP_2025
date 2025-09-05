using Fusion;
using Managers;
using System.Text;
using TMPro;

namespace UI
{
    public class UIRacePositions
    {
        private RacePositionManager _racePositionManager;
        private TMP_Text _racePositionsText;
        private TMP_Text _winnersText;

        public UIRacePositions(RacePositionManager racePositionManager, TMP_Text racePositionsText, TMP_Text winnersText)
        {
            _racePositionManager = racePositionManager;
            _racePositionsText = racePositionsText;
            _winnersText = winnersText;
        }

        public void UpdateRacePositions()
        {
            if (_racePositionManager == null || _racePositionManager.Object == null)
                return;


            var playerOrder = _racePositionManager.GetCurrentPlayerOrder();
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
            if (_racePositionManager == null || _racePositionManager.Object == null)
                return;

            var winners = _racePositionManager.GetWinnersOrder();
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