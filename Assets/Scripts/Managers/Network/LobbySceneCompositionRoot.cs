using System.Linq;
using Config;
using Fusion;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Managers.Network
{
    [DisallowMultipleComponent]
    public sealed class LobbySceneCompositionRoot : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NetworkPrefabRef lobbyRosterStatePrefab;
        [SerializeField] private PaddleColorPalette paddleColorPalette;
        [SerializeField] private UIWaitingRoom waitingRoom;

        private NetworkRunner _composedRunner;
        private LobbySessionState _composedSessionState;

        private static LobbySceneCompositionRoot _activeInstance;

        public static LobbySceneCompositionRoot ActiveInstance =>
            _activeInstance != null && IsInActiveScene(_activeInstance) ? _activeInstance : null;

        private void Awake()
        {
            MarkAsActive();
            SubscribeWaitingRoomLifecycle();
        }

        private void OnEnable()
        {
            MarkAsActive();
            SubscribeWaitingRoomLifecycle();

            if (_composedRunner != null)
                Compose(_composedRunner);
        }

        private void OnDisable()
        {
            UnsubscribeWaitingRoomLifecycle();

            if (ReferenceEquals(_activeInstance, this))
                _activeInstance = null;

            waitingRoom?.Unbind();
        }

        public void Compose(NetworkRunner runner)
        {
            if (runner == null)
            {
                Debug.LogError("[LobbySceneCompositionRoot] Compose failed because runner is null.", this);
                return;
            }

            MarkAsActive();

            if (!ValidateCompositionReferences(runner))
                return;

            _composedRunner = runner;
            _composedSessionState = LobbySessionState.EnsureOnRunner(runner);

            if (_composedSessionState == null)
            {
                Debug.LogError("[LobbySceneCompositionRoot] Compose failed because LobbySessionState could not be resolved for the runner.", this);
                return;
            }

            waitingRoom.Configure(paddleColorPalette);
            _composedSessionState.Configure(paddleColorPalette);
            _composedSessionState?.EnterLobby(runner, lobbyRosterStatePrefab);
            waitingRoom.Bind(_composedSessionState);
        }

        internal static bool TryGetActive(out LobbySceneCompositionRoot root)
        {
            root = ResolveActiveRoot();
            if (root == null)
                return false;

            return true;
        }

        private void SubscribeWaitingRoomLifecycle()
        {
            if (waitingRoom == null)
                return;

            waitingRoom.Enabled -= HandleWaitingRoomEnabled;
            waitingRoom.Enabled += HandleWaitingRoomEnabled;
        }

        private void UnsubscribeWaitingRoomLifecycle()
        {
            if (waitingRoom != null)
                waitingRoom.Enabled -= HandleWaitingRoomEnabled;
        }

        private void HandleWaitingRoomEnabled()
        {
            if (_composedSessionState == null)
                return;

            waitingRoom.Configure(paddleColorPalette);
            waitingRoom.Bind(_composedSessionState);
        }

        private void MarkAsActive()
        {
            _activeInstance = this;
        }

        private bool ValidateCompositionReferences(NetworkRunner runner)
        {
            if (!IsInActiveScene(this))
            {
                Debug.LogError("[LobbySceneCompositionRoot] Compose failed because the composition root is not in the active scene.", this);
                return false;
            }

            if (waitingRoom == null)
            {
                Debug.LogError("[LobbySceneCompositionRoot] Compose failed because UIWaitingRoom is not assigned. Wire the waiting room through the Lobby scene composition root.", this);
                return false;
            }

            if (paddleColorPalette == null)
            {
                Debug.LogError("[LobbySceneCompositionRoot] Compose failed because PaddleColorPalette is not assigned. The Lobby scene composition root is the single scene owner and must inject the palette into UIWaitingRoom and LobbySessionState.", this);
                return false;
            }

            if (runner.IsServer && !lobbyRosterStatePrefab.IsValid)
            {
                Debug.LogError("[LobbySceneCompositionRoot] Compose failed because the lobby roster prefab is not assigned for the host runner. Wire the roster prefab directly through the Lobby scene composition root.", this);
                return false;
            }

            return true;
        }

        private static LobbySceneCompositionRoot ResolveActiveRoot()
        {
            if (ActiveInstance != null)
                return ActiveInstance;

            var resolved = FindFirstInActiveScene<LobbySceneCompositionRoot>();
            if (resolved == null)
                return null;

            _activeInstance = resolved;
            return resolved;
        }

        private static T FindFirstInActiveScene<T>() where T : Component
        {
            var activeScene = SceneManager.GetActiveScene();

            return Object.FindObjectsByType<T>(FindObjectsSortMode.InstanceID)
                .FirstOrDefault(component => component != null && component.gameObject.scene == activeScene);
        }

        private static bool IsInActiveScene(LobbySceneCompositionRoot root)
        {
            return root != null && root.gameObject.scene == SceneManager.GetActiveScene();
        }

        private static bool IsInActiveScene(Component component)
        {
            return component != null && component.gameObject.scene == SceneManager.GetActiveScene();
        }
    }
}
