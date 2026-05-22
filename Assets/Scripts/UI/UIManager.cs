using Managers;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Helpers;
using Managers.Network;
using Config;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text racePositionsText;
        [SerializeField] private TMP_Text winnersText;
        [SerializeField] private TMP_Text finalWinnersText;
        [SerializeField] private TMP_Text countdownText;

        [Header("Game objects")]
        [SerializeField] private GameObject waitingForPlayersPanel;
        [SerializeField] private GameObject countdownCanvas;
        [SerializeField] private GameObject gameOverCanvas;

        [Header("Buttons")]
        [SerializeField] private Button menuButton;
        [SerializeField] private Button quitButton;

        private TimerManager _timerManager;
        private ScoreManager _scoreManager;
        private GameOverManager _gameOverManager;

        private UITimer _uiTimer;
        private UIScore _uiScore;

        private bool _unlockedWithEsc = false;

        private void OnEnable()
        {
            menuButton.onClick.AddListener(ReturnToMainMenu);
            quitButton.onClick.AddListener(QuitGame);

            gameOverCanvas.SetActive(false);
            waitingForPlayersPanel.SetActive(true);
            countdownCanvas.SetActive(false);

            if (NetworkManager.Instance)
                NetworkManager.Instance.OnDisconnected += TriggerGameOver;

            CursorLocker.Lock();
        }

        private void OnDisable()
        {
            menuButton.onClick.RemoveListener(ReturnToMainMenu);
            quitButton.onClick.RemoveListener(QuitGame);

            if (NetworkManager.Instance)
                NetworkManager.Instance.OnDisconnected -= TriggerGameOver;

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


            _uiTimer = new UITimer(_timerManager, timerText);
            _uiScore = new UIScore(_scoreManager, racePositionsText, winnersText);

            StartCountdownVisual();
            UpdateWaitingStatus();
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
            _uiScore.UpdateRacePositions();
            _uiScore.UpdateWinners();

            CheckGameOver();
        }

        private void UpdateWaitingStatus()
        {
            if (_timerManager == null) return;
            waitingForPlayersPanel.SetActive(false);
        }

        private void CheckGameOver()
        {
            if(_gameOverManager == null || _gameOverManager.Object == null)
                return;

            if (_gameOverManager.IsGameOver && !gameOverCanvas.activeSelf)
            {
                Debug.Log("Client: Showing game over screen!");

                CursorLocker.Unlock();
                gameOverCanvas.SetActive(true);
                finalWinnersText.text = _scoreManager != null ? _scoreManager.GetMatchResultLabel() : winnersText.text;
            }
        }

        private void TriggerGameOver()
        {
            CursorLocker.Unlock();
            gameOverCanvas.SetActive(true);
            finalWinnersText.text = _scoreManager != null ? _scoreManager.GetMatchResultLabel() : winnersText.text;
        }

        private void ReturnToMainMenu()
        {
            var resolvedMainMenuBuildIndex = SceneCatalog.GetMainMenuIndex(-1);
            if (resolvedMainMenuBuildIndex < 0)
            {
                Debug.LogError("[UIManager] Could not resolve MainMenu scene index from SceneCatalog.");
                return;
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene(resolvedMainMenuBuildIndex);
        }

        private void StartCountdownVisual()
        {
            countdownCanvas.SetActive(true);
            StartCoroutine(CountdownRoutine());
        }

        private IEnumerator CountdownRoutine()
        {
            countdownText.gameObject.SetActive(true);

            countdownText.text = "3";
            yield return new WaitForSeconds(1f);

            countdownText.text = "2";
            yield return new WaitForSeconds(1f);

            countdownText.text = "1";
            yield return new WaitForSeconds(1f);

            countdownCanvas.gameObject.SetActive(false);
        }

        private void TryRelockCursorOnClick()
        {
            if (_unlockedWithEsc && Input.GetMouseButtonDown(0))
            {
                if (!IsPointerOverButton(quitButton) && !IsPointerOverButton(menuButton))
                {
                    CursorLocker.Lock();
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

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
        }
    }
}
