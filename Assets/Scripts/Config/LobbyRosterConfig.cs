using Fusion;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "LobbyRosterConfig", menuName = "Config/Lobby Roster Config")]
    public sealed class LobbyRosterConfig : ScriptableObject
    {
        private const string FallbackResourcesPath = "LobbyRosterConfig";

        [SerializeField] private NetworkPrefabRef lobbyRosterPrefab;

        public NetworkPrefabRef LobbyRosterPrefab => lobbyRosterPrefab;

        public static NetworkPrefabRef ResolveLobbyRosterPrefabFallback()
        {
            var config = Resources.Load<LobbyRosterConfig>(FallbackResourcesPath);
            if (config == null)
            {
                Debug.LogError($"[LobbyRosterConfig] Fallback load failed for '{FallbackResourcesPath}'. Assign LobbyRosterConfig through LobbySceneCompositionRoot in the Lobby scene.");
                return default;
            }

            return config.LobbyRosterPrefab;
        }
    }
}
