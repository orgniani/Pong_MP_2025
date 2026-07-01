using Managers;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Helpers;
using Managers.Network;
using Config;
using Players;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text countdownText;
        [SerializeField] private TMP_Text finalWinnersText;

        [Header("Game objects")]
        [SerializeField] private GameObject countdownCanvas;
        [SerializeField] private GameObject gameOverCanvas;

        [Header("Buttons")]
        [SerializeField] private Button menuButton;

        [Header("Config")]
        [SerializeField] private MatchRulesConfig matchRulesConfig;

        private TimerManager _timerManager;
        private ScoreManager _scoreManager;
        private GameOverManager _gameOverManager;

        private UITimer _uiTimer;
        private UIScore _uiScore;

        private bool _unlockedWithEsc = false;

        private void Awake()
        {
            if (!matchRulesConfig)
            {
                Debug.LogError("[UIManager] MatchRulesConfig is missing.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            menuButton.onClick.AddListener(ReturnToMainMenu);

            countdownCanvas.SetActive(false);
            gameOverCanvas.SetActive(false);

            if (NetworkManager.Instance)
            {
                NetworkManager.Instance.OnDisconnected += TriggerGameOver;
                NetworkManager.Instance.OnNewPlayerJoined += HandleRosterChanged;
                NetworkManager.Instance.OnJoinedPlayerLeft += HandleRosterChanged;
            }

            Player.OnAnyUsernameChanged += HandleUsernameChanged;
        }

        private void OnDisable()
        {
            menuButton.onClick.RemoveListener(ReturnToMainMenu);

            if (NetworkManager.Instance)
            {
                NetworkManager.Instance.OnDisconnected -= TriggerGameOver;
                NetworkManager.Instance.OnNewPlayerJoined -= HandleRosterChanged;
                NetworkManager.Instance.OnJoinedPlayerLeft -= HandleRosterChanged;
            }

            Player.OnAnyUsernameChanged -= HandleUsernameChanged;

            CursorLocker.Unlock();
        }

        private IEnumerator Start()
        {
            while (_timerManager == null)
            {
                _timerManager = FindFirstObjectByType<TimerManager>();
                yield return null;
            }

            while (_scoreManager == null)
            {
                _scoreManager = FindFirstObjectByType<ScoreManager>();
                yield return null;
            }

            while (_gameOverManager == null)
            {
                _gameOverManager = FindFirstObjectByType<GameOverManager>();
                yield return null;
            }

            _uiScore = new UIScore(_scoreManager, scoreText);
            _uiScore.RefreshNames();
            _uiTimer = new UITimer(_timerManager, timerText);

            yield return StartCoroutine(CountdownRoutine());
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CursorLocker.Unlock();
                _unlockedWithEsc = true;
            }

            TryRelockCursorOnClick();

            if (_uiTimer == null || _uiScore == null)
                return;

            _uiTimer.UpdateTimer();
            _uiScore.UpdateScore();

            CheckGameOver();
        }

        private IEnumerator CountdownRoutine()
        {
            countdownCanvas.SetActive(true);

            for (int remaining = Mathf.CeilToInt(matchRulesConfig.CountdownSeconds); remaining > 0; remaining--)
            {
                countdownText.text = remaining.ToString();
                yield return new WaitForSeconds(1f);
            }

            countdownCanvas.SetActive(false);
        }

        private void CheckGameOver()
        {
            if (_gameOverManager == null || _gameOverManager.Object == null)
                return;

            if (_gameOverManager.IsGameOver && !gameOverCanvas.activeSelf)
            {
                CursorLocker.Unlock();
                gameOverCanvas.SetActive(true);
                finalWinnersText.text = _scoreManager != null ? _scoreManager.GetMatchResultLabel() : string.Empty;
            }
        }

        private void HandleRosterChanged(string _)
        {
            _uiScore?.RefreshNames();
        }

        private void HandleUsernameChanged()
        {
            _uiScore?.RefreshNames();
        }

        private void TriggerGameOver()
        {
            CursorLocker.Unlock();
            gameOverCanvas.SetActive(true);
            finalWinnersText.text = _scoreManager != null ? _scoreManager.GetMatchResultLabel() : string.Empty;
        }

        private void ReturnToMainMenu()
        {
            if (NetworkManager.Instance)
                NetworkManager.Instance.Shutdown();

            var index = SceneCatalog.GetMainMenuIndex(-1);
            if (index < 0)
            {
                Debug.LogError("[UIManager] Could not resolve MainMenu scene index.");
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(index);
        }

        private void TryRelockCursorOnClick()
        {
            if (_unlockedWithEsc && Input.GetMouseButtonDown(0))
            {
                if (!IsPointerOverButton(menuButton))
                {
                    _unlockedWithEsc = false;
                }
            }
        }

        private bool IsPointerOverButton(Button button)
        {
            if (button == null || button.gameObject == null)
                return false;

            RectTransform rectTransform = button.GetComponent<RectTransform>();
            if (rectTransform == null) return false;

            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, null);
        }
    }
}
