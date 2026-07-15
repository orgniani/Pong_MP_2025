using System.Collections;
using System.Collections.Generic;
using Common;
using Fusion;
using Helpers;
using Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class UISessionBrowser : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SessionBrowserService browserService;
        [SerializeField] private Transform contentRoot;

        [Header("Prefabs")]
        [SerializeField] private UISessionEntry entryPrefab;

        [Header("Text")]
        [SerializeField] private TMP_Text headerText;
        [SerializeField] private TMP_Text emptyStateText;

        [Header("Game objects")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject loadingPanel;

        [Header("Buttons")]
        [SerializeField] private Button refreshButton;
        [SerializeField] private Button backButton;

        [Header("Config")]
        [SerializeField] private bool enableLogs = false;

        private readonly List<UISessionEntry> _spawnedEntries = new List<UISessionEntry>();
        private MatchMode _mode;
        private bool _subscribed;

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            refreshButton.onClick.AddListener(Refresh);
            backButton.onClick.AddListener(Close);

            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void Open(MatchMode mode)
        {
            _mode = mode;

            if (panelRoot != null) panelRoot.SetActive(true);
            if (headerText != null)
            {
                headerText.text = _mode == MatchMode.OneVsOne
                    ? "Available 1 vs 1 matches"
                    : "Available 2 vs 2 matches";
            }

            Subscribe();
            Refresh();
        }

        public void Close()
        {
            Unsubscribe();
            ClearEntries();
            if (browserService != null) browserService.LeaveLobby();
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void Refresh()
        {
            StartCoroutine(JoinLobbyRoutine());
        }

        private IEnumerator JoinLobbyRoutine()
        {
            SetLoading(true);

            var task = browserService.JoinLobbyAsync();
            while (!task.IsCompleted)
            {
                yield return null;
            }

            SetLoading(false);

            if (task.Exception != null)
            {
                Debug.LogError(task.Exception, this);
            }
        }

        private void HandleSessionsUpdated(IReadOnlyList<SessionInfo> sessions)
        {
            ClearEntries();

            var targetMaxPlayers = _mode.ToSessionMaxPlayers();
            var shown = 0;

            for (var i = 0; i < sessions.Count; i++)
            {
                var info = sessions[i];
                if (!info.IsValid || info.MaxPlayers != targetMaxPlayers)
                {
                    continue;
                }

                var entry = Instantiate(entryPrefab, contentRoot);
                entry.Bind(info, _mode, OnEntryJoinClicked);
                _spawnedEntries.Add(entry);
                shown++;
            }

            Log($"Rendered {shown} session(s) for mode {_mode}.");

            var isEmpty = shown == 0;
            if (emptyStateText != null)
            {
                emptyStateText.gameObject.SetActive(isEmpty);
                if (isEmpty)
                {
                    emptyStateText.text = $"No {_mode.ToDisplayLabel()} matches available. Start a server.";
                }
            }
        }

        private void OnEntryJoinClicked(string sessionName)
        {
            StartCoroutine(JoinSessionRoutine(sessionName));
        }

        private IEnumerator JoinSessionRoutine(string sessionName)
        {
            SetLoading(true);

            var task = browserService.JoinSessionAsync(sessionName);
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.Exception != null)
            {
                Debug.LogError(task.Exception, this);
            }

            var joined = task.Exception == null && task.Result;
            if (!joined)
            {
                SetLoading(false);
            }
        }

        private void Subscribe()
        {
            if (_subscribed || browserService == null) return;
            browserService.OnSessionsUpdated += HandleSessionsUpdated;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed || browserService == null) return;
            browserService.OnSessionsUpdated -= HandleSessionsUpdated;
            _subscribed = false;
        }

        private void ClearEntries()
        {
            for (var i = 0; i < _spawnedEntries.Count; i++)
            {
                if (_spawnedEntries[i] != null)
                {
                    Destroy(_spawnedEntries[i].gameObject);
                }
            }

            _spawnedEntries.Clear();
        }

        private void SetLoading(bool isLoading)
        {
            if (loadingPanel != null) loadingPanel.SetActive(isLoading);
        }

        private bool ValidateReferences()
        {
            var ok = ReferenceValidator.Validate(browserService, nameof(browserService), this)
                    & ReferenceValidator.Validate(contentRoot, nameof(contentRoot), this)
                    & ReferenceValidator.Validate(entryPrefab, nameof(entryPrefab), this)
                    & ReferenceValidator.Validate(refreshButton, nameof(refreshButton), this)
                    & ReferenceValidator.Validate(backButton, nameof(backButton), this);

            ReferenceValidator.ValidateOptional(panelRoot, nameof(panelRoot), this);
            ReferenceValidator.ValidateOptional(headerText, nameof(headerText), this);
            ReferenceValidator.ValidateOptional(emptyStateText, nameof(emptyStateText), this);
            ReferenceValidator.ValidateOptional(loadingPanel, nameof(loadingPanel), this);

            return ok;
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (refreshButton != null) refreshButton.onClick.RemoveListener(Refresh);
            if (backButton != null) backButton.onClick.RemoveListener(Close);
        }

        private void Log(string message)
        {
            if (!enableLogs) return;
            Debug.Log($"[{GetType().Name}] {message}", this);
        }
    }
}
