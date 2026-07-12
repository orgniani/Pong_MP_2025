using System;
using Fusion;
using Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class UISessionEntry : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TMP_Text sessionNameText;
        [SerializeField] private TMP_Text modeText;
        [SerializeField] private TMP_Text occupancyText;

        [Header("Buttons")]
        [SerializeField] private Button joinButton;

        private string _sessionName;
        private Action<string> _onJoin;

        private void Awake()
        {
            ReferenceValidator.ValidateOptional(sessionNameText, nameof(sessionNameText), this);
            ReferenceValidator.ValidateOptional(modeText, nameof(modeText), this);
            ReferenceValidator.ValidateOptional(occupancyText, nameof(occupancyText), this);
            ReferenceValidator.ValidateOptional(joinButton, nameof(joinButton), this);
        }

        public void Bind(SessionInfo info, UIGameModeFilter mode, Action<string> onJoin)
        {
            _sessionName = info.Name;
            _onJoin = onJoin;

            var gamePlayers = UIGameModeFilterExtensions.ToGamePlayerCount(info.PlayerCount);
            var gameCapacity = mode.ToMaxPlayers();

            if (sessionNameText != null) sessionNameText.text = info.Name;
            if (modeText != null) modeText.text = mode.ToDisplayLabel();
            if (occupancyText != null) occupancyText.text = $"{gamePlayers}/{gameCapacity}";

            var isFull = gamePlayers >= gameCapacity;
            if (joinButton != null)
            {
                joinButton.interactable = info.IsOpen && !isFull;
                joinButton.onClick.RemoveListener(HandleJoinClicked);
                joinButton.onClick.AddListener(HandleJoinClicked);
            }
        }

        private void HandleJoinClicked()
        {
            _onJoin?.Invoke(_sessionName);
        }

        private void OnDestroy()
        {
            if (joinButton != null)
            {
                joinButton.onClick.RemoveListener(HandleJoinClicked);
            }
        }
    }
}
