using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Managers.Network;
using Fusion;
using System.Collections;
using TMPro;
using Config;

namespace UI
{
    public class UIMainMenu : MonoBehaviour
    {
        [SerializeField] private Button joinButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private TMP_InputField usernameInputField;

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

            var lobbySceneIndex = SceneCatalog.GetLobbyIndex();
            if (lobbySceneIndex < 0)
            {
                Debug.LogError("Could not resolve lobby scene index from SceneIndexCatalog.");
                joinButton.interactable = true;
                loadingPanel.SetActive(false);
                return;
            }

            bool success = await _sessionHandler.StartClient(_networkRunner, NetworkSessionHandler.DefaultSessionName, lobbySceneIndex);

            if (!success)
            {
                Debug.LogError("Could not connect to the server.");
                joinButton.interactable = true;
                loadingPanel.SetActive(false);
            }
            else
            {
                Debug.Log("Connected successfully! Fusion will load the Lobby scene from the server.");
            }
        }

    }
}
