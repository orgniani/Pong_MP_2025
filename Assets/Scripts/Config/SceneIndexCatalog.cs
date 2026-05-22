using UnityEngine;

namespace Config
{
    [CreateAssetMenu(fileName = "SceneIndexCatalog", menuName = "Config/Scene Index Catalog")]
    public class SceneIndexCatalog : ScriptableObject
    {
        [SerializeField] private int mainMenuBuildIndex = 1;
        [SerializeField] private int lobbyBuildIndex = 2;
        [SerializeField] private int gameBuildIndex = 3;

        public int MainMenuBuildIndex => mainMenuBuildIndex;
        public int LobbyBuildIndex => lobbyBuildIndex;
        public int GameBuildIndex => gameBuildIndex;
    }
}
