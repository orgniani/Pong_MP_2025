using Fusion;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "LobbyRosterConfig", menuName = "Config/Lobby Roster Config")]
    public sealed class LobbyRosterConfig : ScriptableObject
    {
        private const string ResourcesPath = "LobbyRosterConfig";

        [SerializeField] private NetworkPrefabRef lobbyRosterPrefab;

        public NetworkPrefabRef LobbyRosterPrefab => lobbyRosterPrefab;

        public static NetworkPrefabRef ResolveLobbyRosterPrefab()
        {
            var config = Resources.Load<LobbyRosterConfig>(ResourcesPath);
            if (config == null)
            {
                Debug.LogError($"[LobbyRosterConfig] Could not load '{ResourcesPath}' from Resources. Create it via Assets > Create > Pong > Lobby Roster Config.");
                return default;
            }

            return config.LobbyRosterPrefab;
        }
    }
}
