using Fusion;
using Managers.Network;
using UnityEngine;

namespace Boot
{
    public class DedicatedServerBootstrap : MonoBehaviour
    {
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private int sceneBuildIndex = 1;

        private async void Start()
        {
            var runnerObj = new GameObject("NetworkRunner", typeof(NetworkRunner));
            var runner = runnerObj.GetComponent<NetworkRunner>();
            var sessionHandler = new NetworkSessionHandler();
            await sessionHandler.StartServer(runner, maxPlayers, sceneBuildIndex);
        }
    }
}