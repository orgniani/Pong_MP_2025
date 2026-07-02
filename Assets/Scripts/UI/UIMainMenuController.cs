using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class UIMainMenuController : MonoBehaviour
    {
        [SerializeField] private Button play1v1Button;
        [SerializeField] private Button play2v2Button;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button closeCreditsButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_InputField usernameInputField;
        [SerializeField] private UISessionBrowser sessionBrowser;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private bool enableLogs = false;

        private const int UsernameCharacterLimit = 16;

        public string Username => usernameInputField != null ? usernameInputField.text.Trim() : string.Empty;

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            usernameInputField.characterLimit = UsernameCharacterLimit;

            play1v1Button.onClick.AddListener(OnPlay1v1Clicked);
            play2v2Button.onClick.AddListener(OnPlay2v2Clicked);
            creditsButton.onClick.AddListener(OnCreditsClicked);
            if (closeCreditsButton != null) closeCreditsButton.onClick.AddListener(OnCloseCreditsClicked);
            quitButton.onClick.AddListener(QuitGame);

            if (creditsPanel != null) creditsPanel.SetActive(false);
        }

        private void OnPlay1v1Clicked()
        {
            Managers.LocalPlayerSession.Username = Username;
            Log("Opening session browser in 1v1 mode.");
            sessionBrowser.Open(UIGameModeFilter.OneVsOne);
        }

        private void OnPlay2v2Clicked()
        {
            Managers.LocalPlayerSession.Username = Username;
            Log("Opening session browser in 2v2 mode.");
            sessionBrowser.Open(UIGameModeFilter.TwoVsTwo);
        }

        private void OnCreditsClicked()
        {
            if (creditsPanel != null) creditsPanel.SetActive(true);
        }

        private void OnCloseCreditsClicked()
        {
            if (creditsPanel != null) creditsPanel.SetActive(false);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private bool ValidateReferences()
        {
            var ok = true;

            if (play1v1Button == null) { Debug.LogError("[UIMainMenuController] play1v1Button is not assigned.", this); ok = false; }
            if (play2v2Button == null) { Debug.LogError("[UIMainMenuController] play2v2Button is not assigned.", this); ok = false; }
            if (creditsButton == null) { Debug.LogError("[UIMainMenuController] creditsButton is not assigned.", this); ok = false; }
            if (quitButton == null) { Debug.LogError("[UIMainMenuController] quitButton is not assigned.", this); ok = false; }
            if (usernameInputField == null) { Debug.LogError("[UIMainMenuController] usernameInputField is not assigned.", this); ok = false; }
            if (sessionBrowser == null) { Debug.LogError("[UIMainMenuController] sessionBrowser is not assigned.", this); ok = false; }

            return ok;
        }

        private void OnDestroy()
        {
            if (play1v1Button != null) play1v1Button.onClick.RemoveListener(OnPlay1v1Clicked);
            if (play2v2Button != null) play2v2Button.onClick.RemoveListener(OnPlay2v2Clicked);
            if (creditsButton != null) creditsButton.onClick.RemoveListener(OnCreditsClicked);
            if (closeCreditsButton != null) closeCreditsButton.onClick.RemoveListener(OnCloseCreditsClicked);
            if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
        }

        private void Log(string message)
        {
            if (!enableLogs) return;
            Debug.Log($"[{GetType().Name}] {message}", this);
        }
    }
}
