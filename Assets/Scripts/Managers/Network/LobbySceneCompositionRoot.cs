using Config;
using System.Linq;
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
        [SerializeField] private LobbyRosterConfig lobbyRosterConfig;
        [SerializeField] private UIWaitingRoom waitingRoom;

        private NetworkRunner _composedRunner;
        private LobbySessionState _composedSessionState;

        private static LobbySceneCompositionRoot _activeInstance;

        public static LobbySceneCompositionRoot ActiveInstance =>
            _activeInstance != null && IsInActiveScene(_activeInstance) ? _activeInstance : null;

        private void Awake()
        {
            if (waitingRoom == null)
                waitingRoom = GetComponent<UIWaitingRoom>();

            MarkAsActive();
        }

        private void OnEnable()
        {
            MarkAsActive();

            if (_composedRunner != null)
                Compose(_composedRunner);
        }

        private void OnDisable()
        {
            if (ReferenceEquals(_activeInstance, this))
                _activeInstance = null;

            waitingRoom?.Unbind();
        }

        public void Compose(NetworkRunner runner)
        {
            if (runner == null)
                return;

            MarkAsActive();

            _composedRunner = runner;
            _composedSessionState = LobbySessionState.EnsureOnRunner(runner);
            _composedSessionState?.EnterLobby(runner, lobbyRosterConfig);

            if (waitingRoom != null && _composedSessionState != null)
                waitingRoom.Bind(_composedSessionState);
        }

        public static bool TryCompose(NetworkRunner runner)
        {
            var root = ResolveActiveRoot();
            if (root == null)
                return false;

            root.Compose(runner);
            return true;
        }

        public static bool TryBindWaitingRoom(LobbySessionState lobbySessionState)
        {
            return TryBindWaitingRoom(ResolveWaitingRoom(), lobbySessionState);
        }

        public static bool TryBindWaitingRoom(UIWaitingRoom targetWaitingRoom)
        {
            return TryBindWaitingRoom(targetWaitingRoom, ResolveLobbySessionState());
        }

        private static bool TryBindWaitingRoom(UIWaitingRoom targetWaitingRoom, LobbySessionState lobbySessionState)
        {
            if (targetWaitingRoom == null || lobbySessionState == null || !IsInActiveScene(targetWaitingRoom))
                return false;

            targetWaitingRoom.Bind(lobbySessionState);
            return true;
        }

        private void MarkAsActive()
        {
            _activeInstance = this;
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

        private static UIWaitingRoom ResolveWaitingRoom()
        {
            var root = ResolveActiveRoot();
            if (root != null && root.waitingRoom != null)
                return root.waitingRoom;

            return FindFirstInActiveScene<UIWaitingRoom>();
        }

        private static LobbySessionState ResolveLobbySessionState()
        {
            var root = ResolveActiveRoot();
            if (root != null)
            {
                if (root._composedSessionState != null)
                    return root._composedSessionState;

                if (root._composedRunner != null)
                    return LobbySessionState.FindForRunner(root._composedRunner);
            }

            return LobbySessionState.ActiveInstance ?? LobbySessionState.FindRunnerOwnedInstance();
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
