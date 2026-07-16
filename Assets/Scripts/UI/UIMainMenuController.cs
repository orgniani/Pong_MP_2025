using Common;
using Helpers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public sealed class UIMainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button play1v1Button;
        [SerializeField] private Button play2v2Button;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button closeCreditsButton;
        [SerializeField] private Button quitButton;

        [Header("Text")]
        [SerializeField] private TMP_InputField usernameInputField;

        [Header("References")]
        [SerializeField] private UISessionBrowser sessionBrowser;

        [Header("Game objects")]
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject usernameValidationPanel;

        [Header("Settings")]
        [SerializeField] private int usernameCharacterLimit = 10;

        public string Username => usernameInputField != null ? usernameInputField.text.Trim() : string.Empty;

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            usernameInputField.characterLimit = usernameCharacterLimit;
            usernameInputField.onValidateInput = OnValidateUsernameInput;
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
            TryOpenSessionBrowser(MatchMode.OneVsOne);
        }

        private void OnPlay2v2Clicked()
        {
            TryOpenSessionBrowser(MatchMode.TwoVsTwo);
        }

        private static char OnValidateUsernameInput(string text, int charIndex, char addedChar)
        {
            return char.IsLetterOrDigit(addedChar) ? addedChar : '\0';
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
            var ok = ReferenceValidator.Validate(play1v1Button, nameof(play1v1Button), this)
                    & ReferenceValidator.Validate(play2v2Button, nameof(play2v2Button), this)
                    & ReferenceValidator.Validate(creditsButton, nameof(creditsButton), this)
                    & ReferenceValidator.Validate(quitButton, nameof(quitButton), this)
                    & ReferenceValidator.Validate(usernameInputField, nameof(usernameInputField), this)
                    & ReferenceValidator.Validate(sessionBrowser, nameof(sessionBrowser), this);

            ReferenceValidator.ValidateOptional(closeCreditsButton, nameof(closeCreditsButton), this);
            ReferenceValidator.ValidateOptional(creditsPanel, nameof(creditsPanel), this);
            ReferenceValidator.ValidateOptional(usernameValidationPanel, nameof(usernameValidationPanel), this);

            return ok;
        }

        private void TryOpenSessionBrowser(MatchMode gameModeFilter)
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
