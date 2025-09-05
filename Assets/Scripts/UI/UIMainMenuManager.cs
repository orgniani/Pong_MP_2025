using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Managers.Network;
using Fusion;
using System.Collections;
using TMPro;

namespace UI
{
    public class UIMainMenuManager : MonoBehaviour
    {
        [SerializeField] private Button joinButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private TMP_InputField usernameInputField;

        [Header("Build Index of Game Scene")]
        [SerializeField] private int raceSceneBuildIndex = 1;

        private NetworkSessionHandler _sessionHandler;
        private NetworkRunner _networkRunner;

        private void Start()
        {
            _sessionHandler = new NetworkSessionHandler();
            joinButton.onClick.AddListener(OnJoinClicked);
            quitButton.onClick.AddListener(QuitGame);

            usernameInputField.characterLimit = 16;
        }

        private void OnDisable()
        {
            joinButton.onClick.RemoveListener(OnJoinClicked);
            quitButton.onClick.RemoveListener(QuitGame);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
        }

        private void OnJoinClicked()
        {
            var username = usernameInputField.text.Trim();

            //PlayerInfo.PlayerName = username;
            StartCoroutine(StartGameCoroutine());
        }

        private IEnumerator StartGameCoroutine()
        {
            var task = StartGame();
            while (!task.IsCompleted)
                yield return null;

            if (task.Exception != null)
                Debug.LogError(task.Exception);
        }

        private async Task StartGame()
        {
            joinButton.interactable = false;
            loadingPanel.SetActive(true);

            GameObject runnerObj = new GameObject("NetworkRunner", typeof(NetworkRunner));
            _networkRunner = runnerObj.GetComponent<NetworkRunner>();
            _networkRunner.ProvideInput = true;

            bool success = await _sessionHandler.JoinOrCreateSession(_networkRunner, 4, raceSceneBuildIndex);

            if (!success)
            {
                Debug.LogError("Could not connect or create a session.");
                joinButton.interactable = true;
                loadingPanel.SetActive(false);
            }
            else
            {
                Debug.Log("Connected successfully! Loading game...");
            }
        }
    }
}
