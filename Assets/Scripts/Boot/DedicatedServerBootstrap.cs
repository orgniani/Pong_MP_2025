using Fusion;
using System;
using Managers.Network;
using UnityEngine;

namespace Boot
{
    public class DedicatedServerBootstrap : MonoBehaviour
    {
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private int sceneBuildIndex = 1;
        [SerializeField] private bool allowNonHeadlessStartInEditor = true;

        private async void Start()
        {
            if (!ShouldStartDedicatedServer())
            {
                Debug.Log("DedicatedServerBootstrap skipped: non-headless runtime detected.");
                return;
            }

            var runnerObj = new GameObject("NetworkRunner", typeof(NetworkRunner));
            var runner = runnerObj.GetComponent<NetworkRunner>();
            runner.ProvideInput = false;

            var sessionHandler = new NetworkSessionHandler();
            await sessionHandler.StartServer(runner, maxPlayers, sceneBuildIndex);
        }

        private bool ShouldStartDedicatedServer()
        {
            var args = Environment.GetCommandLineArgs();
            var hasDedicated = Array.Exists(args, arg => string.Equals(arg, "-dedicated", StringComparison.OrdinalIgnoreCase));
            var hasBatchModeArg = Array.Exists(args, arg => string.Equals(arg, "-batchmode", StringComparison.OrdinalIgnoreCase));

            if (!hasDedicated)
            {
                return false;
            }

#if UNITY_EDITOR
            if (allowNonHeadlessStartInEditor)
            {
                return true;
            }

            return Application.isBatchMode || hasBatchModeArg;
#else
            return Application.isBatchMode || hasBatchModeArg;
#endif
        }
    }
}
