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

            if (_lobbySessionState != null)
                RefreshView(_lobbySessionState.CurrentSnapshot);
            else
                TryBindLobbySessionState();
        }

        private void Start()
        {
            if (_lobbySessionState != null)
                return;

            TryBindLobbySessionState();
        }

        private void OnDisable()
        {
            if (leaveButton != null)
                leaveButton.onClick.RemoveListener(HandleLeaveClicked);

            Unbind();
        }

        public void Bind(LobbySessionState lobbySessionState)
        {
            if (ReferenceEquals(_lobbySessionState, lobbySessionState))
            {
                RefreshView(_lobbySessionState != null ? _lobbySessionState.CurrentSnapshot : CreateEmptySnapshot());
                return;
            }

            Unbind();
            _lobbySessionState = lobbySessionState;

            if (_lobbySessionState != null)
            {
                _lobbySessionState.SnapshotChanged += RefreshView;
                RefreshView(_lobbySessionState.CurrentSnapshot);
                return;
            }

            RefreshView(CreateEmptySnapshot());
        }

        public void Unbind()
        {
            if (_lobbySessionState != null)
                _lobbySessionState.SnapshotChanged -= RefreshView;

            _lobbySessionState = null;
            RefreshView(CreateEmptySnapshot());
        }

        private void TryBindLobbySessionState()
        {
            if (_lobbySessionState != null)
                return;

            if (!LobbySceneCompositionRoot.TryBindWaitingRoom(this))
            {
                var resolved = LobbySessionState.ActiveInstance ?? LobbySessionState.FindRunnerOwnedInstance();
                if (resolved != null)
                    Bind(resolved);
            }
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

        private static LobbySessionSnapshot CreateEmptySnapshot()
        {
            return new LobbySessionSnapshot(Array.Empty<string>(), 0, 0);
        }
    }
}
