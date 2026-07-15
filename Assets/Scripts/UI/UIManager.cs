using Managers;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Helpers;
using Network;
using Players;
using Balls;

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
        [SerializeField] private GameObject playerDisconnectedNode;

        [Header("Buttons")]
        [SerializeField] private Button menuButton;

        [Header("Ball")]
        [SerializeField] private Ball ball;

        private TimerManager _timerManager;
        private ScoreManager _scoreManager;
        private GameOverManager _gameOverManager;

        private UITimer _uiTimer;
        private UIScore _uiScore;

        private bool _unlockedWithEsc = false;

        private void Awake()
        {
            if (!ReferenceValidator.Validate(ball, nameof(ball), this)) return;
        }

        private void OnEnable()
        {
            menuButton.onClick.AddListener(ReturnToMainMenu);

            countdownCanvas.SetActive(false);
            gameOverCanvas.SetActive(false);
            playerDisconnectedNode.SetActive(false);

            if (NetworkManager.Instance)
            {
                NetworkManager.Instance.OnDisconnected += TriggerGameOver;
                NetworkManager.Instance.OnRosterChanged += HandleRosterChanged;
            }

            Player.OnAnyUsernameChanged += HandleRosterChanged;
        }

        private void OnDisable()
        {
            menuButton.onClick.RemoveListener(ReturnToMainMenu);

            if (NetworkManager.Instance)
            {
                NetworkManager.Instance.OnDisconnected -= TriggerGameOver;
                NetworkManager.Instance.OnRosterChanged -= HandleRosterChanged;
            }

            Player.OnAnyUsernameChanged -= HandleRosterChanged;

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

            while (FlowManager.Instance != null && FlowManager.Instance.IsLoading)
                yield return null;

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
            if (!ball.IsLaunchCountdownActive)
                yield break;

            countdownCanvas.SetActive(true);

            while (ball.IsLaunchCountdownActive)
            {
                countdownText.text = Mathf.CeilToInt(ball.LaunchCountdownRemaining).ToString();
                yield return null;
            }

            countdownCanvas.SetActive(false);
            PlayerNameLookup.FreezeCachedSideNames();
        }

        private void CheckGameOver()
        {
            if (_gameOverManager == null || _gameOverManager.Object == null || !_gameOverManager.IsGameOver)
                return;

            if (!gameOverCanvas.activeSelf || string.IsNullOrWhiteSpace(finalWinnersText.text))
                ShowGameOver(_scoreManager != null ? _scoreManager.GetMatchResultLabel() : string.Empty, _gameOverManager.Reason);
        }

        private void HandleRosterChanged()
        {
            _uiScore?.RefreshNames();
        }

        private void TriggerGameOver()
        {
            if (gameOverCanvas.activeSelf && !string.IsNullOrWhiteSpace(finalWinnersText.text))
                return;

            ShowGameOver(_scoreManager != null ? _scoreManager.GetMatchResultLabel() : string.Empty, GameOverReason.None);
        }

        private void ShowGameOver(string winnersText, GameOverReason reason)
        {
            CursorLocker.Unlock();
            gameOverCanvas.SetActive(true);
            finalWinnersText.text = winnersText;
            playerDisconnectedNode.SetActive(reason == GameOverReason.PlayerDisconnected);
        }

        private void ReturnToMainMenu()
        {
            SessionExitToMainMenu.Execute("[UIManager]");
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
