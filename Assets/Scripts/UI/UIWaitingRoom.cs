using Managers.Network;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace UI
{
    public class UIWaitingRoom : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TMP_Text rosterText;
        [SerializeField] private TMP_Text waitingStatusText;
        [SerializeField] private Button leaveButton;

        [Header("Content")]
        [SerializeField] private string waitingStatusPrefix = "Waiting for more players";

        private LobbySessionState _lobbySessionState;

        private void OnEnable()
        {
            if (leaveButton != null)
                leaveButton.onClick.AddListener(HandleLeaveClicked);

            TryBindLobbySessionState();
            RefreshView(_lobbySessionState != null ? _lobbySessionState.CurrentSnapshot : new LobbySessionSnapshot(Array.Empty<string>(), 0, 0));
        }

        private void Start()
        {
            if (_lobbySessionState == null)
            {
                TryBindLobbySessionState();
                RefreshView(_lobbySessionState != null ? _lobbySessionState.CurrentSnapshot : new LobbySessionSnapshot(Array.Empty<string>(), 0, 0));
            }
        }

        private void Update()
        {
            if (_lobbySessionState != null)
                return;

            TryBindLobbySessionState();

            if (_lobbySessionState != null)
                RefreshView(_lobbySessionState.CurrentSnapshot);
        }

        private void OnDisable()
        {
            if (_lobbySessionState != null)
                _lobbySessionState.SnapshotChanged -= RefreshView;

            if (leaveButton != null)
                leaveButton.onClick.RemoveListener(HandleLeaveClicked);

            _lobbySessionState = null;
        }

        private void TryBindLobbySessionState()
        {
            if (_lobbySessionState != null)
                return;

            _lobbySessionState = LobbySessionState.FindRunnerOwnedInstance();

            if (_lobbySessionState != null)
                _lobbySessionState.SnapshotChanged += RefreshView;
        }

        private void RefreshView(LobbySessionSnapshot snapshot)
        {
            if (rosterText != null)
                rosterText.text = snapshot.WaitingUsernames != null && snapshot.WaitingUsernames.Length > 0
                    ? string.Join(Environment.NewLine, snapshot.WaitingUsernames)
                    : string.Empty;

            if (waitingStatusText != null)
            {
                var counter = $"{snapshot.CurrentPlayerCount}/{snapshot.TargetPlayerCapacity}";
                waitingStatusText.text = string.IsNullOrWhiteSpace(waitingStatusPrefix)
                    ? counter
                    : $"{waitingStatusPrefix.Trim()} ({counter})";
            }
        }

        private void HandleLeaveClicked()
        {
            SessionExitToMainMenu.Execute("[UIWaitingRoom]");
        }
    }
}
