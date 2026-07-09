using Managers.Network;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace UI
{
    public class UIWaitingRoom : MonoBehaviour
    {
        public event Action Enabled;

        [Header("References")]
        [SerializeField] private TMP_Text rosterText;
        [SerializeField] private TMP_Text waitingStatusText;
        [SerializeField] private Button leaveButton;
        [SerializeField] private Button readyButton;
        [SerializeField] private TMP_Text readyButtonText;

        [Header("Content")]
        [SerializeField] private string waitingStatusPrefix = "Waiting for more players";
        [SerializeField] private string readyButtonLabel = "Ready";
        [SerializeField] private string readyLockedButtonLabel = "Ready Locked";

        private LobbySessionState _lobbySessionState;

        private void OnEnable()
        {
            if (leaveButton != null)
                leaveButton.onClick.AddListener(HandleLeaveClicked);

            if (readyButton != null)
                readyButton.onClick.AddListener(HandleReadyClicked);

            Enabled?.Invoke();
        }

        private void OnDisable()
        {
            if (leaveButton != null)
                leaveButton.onClick.RemoveListener(HandleLeaveClicked);

            if (readyButton != null)
                readyButton.onClick.RemoveListener(HandleReadyClicked);

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

        private void RefreshView(LobbySessionSnapshot snapshot)
        {
            if (rosterText != null)
                rosterText.text = snapshot.WaitingUsernames != null && snapshot.WaitingUsernames.Length > 0
                    ? BuildRosterText(snapshot)
                    : string.Empty;

            if (waitingStatusText != null)
            {
                var counter = $"{snapshot.CurrentPlayerCount}/{snapshot.TargetPlayerCapacity}";
                waitingStatusText.text = string.IsNullOrWhiteSpace(waitingStatusPrefix)
                    ? counter
                    : $"{waitingStatusPrefix.Trim()} ({counter})";
            }

            if (readyButton != null)
                readyButton.interactable = !snapshot.IsLocalPlayerReady;

            if (readyButtonText != null)
                readyButtonText.text = snapshot.IsLocalPlayerReady ? readyLockedButtonLabel : readyButtonLabel;
        }

        private void HandleLeaveClicked()
        {
            SessionExitToMainMenu.Execute("[UIWaitingRoom]");
        }

        private void HandleReadyClicked()
        {
            _lobbySessionState?.RequestLocalPlayerReadyLock();
        }

        private static LobbySessionSnapshot CreateEmptySnapshot()
        {
            return new LobbySessionSnapshot(Array.Empty<string>(), Array.Empty<bool>(), Array.Empty<int>(), Array.Empty<int>(), false, 0, 0);
        }

        private static string BuildRosterText(LobbySessionSnapshot snapshot)
        {
            var lines = new string[snapshot.WaitingUsernames.Length];

            for (var i = 0; i < snapshot.WaitingUsernames.Length; i++)
            {
                var isReady = snapshot.ReadyStates != null && i < snapshot.ReadyStates.Length && snapshot.ReadyStates[i];
                var username = (snapshot.WaitingUsernames[i] ?? string.Empty).ToUpperInvariant();
                var teamId = snapshot.TeamIds != null && i < snapshot.TeamIds.Length ? snapshot.TeamIds[i] : 0;
                var laneId = snapshot.LaneIds != null && i < snapshot.LaneIds.Length ? snapshot.LaneIds[i] : 0;
                var assignmentLabel = TeamLaneAssignmentUtility.FormatAssignmentLabel(teamId, laneId).ToUpperInvariant();
                var readySuffix = isReady ? " (READY)" : string.Empty;
                lines[i] = $"{username} - {assignmentLabel}{readySuffix}";
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}
