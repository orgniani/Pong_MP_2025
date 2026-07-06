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
        [SerializeField] private GameObject usernameValidationPanel;
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
            usernameInputField.SetTextWithoutNotify(Managers.LocalPlayerSession.Username);

            play1v1Button.onClick.AddListener(OnPlay1v1Clicked);
            play2v2Button.onClick.AddListener(OnPlay2v2Clicked);
            creditsButton.onClick.AddListener(OnCreditsClicked);
            if (closeCreditsButton != null) closeCreditsButton.onClick.AddListener(OnCloseCreditsClicked);
            quitButton.onClick.AddListener(QuitGame);
            usernameInputField.onValueChanged.AddListener(OnUsernameValueChanged);

            if (creditsPanel != null) creditsPanel.SetActive(false);
            SetUsernameValidationVisible(false);
        }

        private void OnEnable()
        {
            if (usernameInputField != null)
                usernameInputField.SetTextWithoutNotify(Managers.LocalPlayerSession.Username);

            SetUsernameValidationVisible(false);
        }

        private void OnPlay1v1Clicked()
        {
            TryOpenSessionBrowser(UIGameModeFilter.OneVsOne);
        }

        private void OnPlay2v2Clicked()
        {
            TryOpenSessionBrowser(UIGameModeFilter.TwoVsTwo);
        }

        private void OnUsernameValueChanged(string _)
        {
            Managers.LocalPlayerSession.Username = Username;

            if (!string.IsNullOrEmpty(Managers.LocalPlayerSession.Username))
            {
                SetUsernameValidationVisible(false);
            }
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

        private void TryOpenSessionBrowser(UIGameModeFilter gameModeFilter)
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                SetUsernameValidationVisible(true);
                return;
            }

            Managers.LocalPlayerSession.Username = Username;
            SetUsernameValidationVisible(false);
            sessionBrowser.Open(gameModeFilter);
        }

        private void SetUsernameValidationVisible(bool isVisible)
        {
            if (usernameValidationPanel != null)
            {
                usernameValidationPanel.SetActive(isVisible);
            }
        }

        private void OnDestroy()
        {
            if (play1v1Button != null) play1v1Button.onClick.RemoveListener(OnPlay1v1Clicked);
            if (play2v2Button != null) play2v2Button.onClick.RemoveListener(OnPlay2v2Clicked);
            if (creditsButton != null) creditsButton.onClick.RemoveListener(OnCreditsClicked);
            if (closeCreditsButton != null) closeCreditsButton.onClick.RemoveListener(OnCloseCreditsClicked);
            if (quitButton != null) quitButton.onClick.RemoveListener(QuitGame);
            if (usernameInputField != null) usernameInputField.onValueChanged.RemoveListener(OnUsernameValueChanged);
        }

    }
}
