using Fusion;
using System;
using Config;
using Helpers;
using Managers.Network;
using UnityEngine;

namespace Boot
{
    public class DedicatedServerBootstrap : MonoBehaviour
    {
        [SerializeField] private MatchRulesConfig matchRulesConfig;
        [SerializeField] private bool allowNonHeadlessStartInEditor = true;

        private const string SessionPrefix = "PongServer";

        private async void Start()
        {
            if (!ShouldStartDedicatedServer())
            {
                Debug.Log($"DedicatedServerBootstrap skipped: missing dedicated/headless requirements. batchMode={Application.isBatchMode}");
                return;
            }

            var runnerObj = new GameObject("NetworkRunner", typeof(NetworkRunner));
            var runner = runnerObj.GetComponent<NetworkRunner>();
            runner.ProvideInput = false;
            runner.AddCallbacks(new DedicatedServerMatchFlow(matchRulesConfig));

            var sessionHandler = new NetworkSessionHandler();
            var lobbySceneIndex = SceneCatalog.GetLobbyIndex();
            if (lobbySceneIndex < 0)
            {
                Debug.LogError("[DedicatedServerBootstrap] Could not resolve Lobby scene index from SceneCatalog.");
                return;
            }

            var options = BuildServerOptions();
            if (options == null)
            {
                return;
            }

            await sessionHandler.StartServer(runner, options.MaxPlayers, lobbySceneIndex, options.SessionName, options.Region, options.Port);
        }

        private DedicatedServerOptions BuildServerOptions()
        {
            var args = Environment.GetCommandLineArgs();
            var fallbackSessionName = $"{SessionPrefix}-{System.Diagnostics.Process.GetCurrentProcess().Id}";
            var sessionName = GetArgumentValue(args, "-sessionName");
            var region = GetArgumentValue(args, "-region");
            var port = TryParseInt(GetArgumentValue(args, "-port"));
            var maxPlayersFromArgs = TryParseInt(GetArgumentValue(args, "-maxPlayers"));

            if (string.IsNullOrWhiteSpace(sessionName))
            {
                sessionName = fallbackSessionName;
            }

            var resolvedMaxPlayers = ResolveMaxPlayers(maxPlayersFromArgs);
            if (!resolvedMaxPlayers.HasValue)
            {
                Debug.LogError("[DedicatedServerBootstrap] MatchRulesConfig is missing and no '-maxPlayers' override was provided.");
                return null;
            }

            return new DedicatedServerOptions
            {
                SessionName = sessionName,
                Region = region,
                Port = port,
                MaxPlayers = resolvedMaxPlayers.Value
            };
        }

        private int? ResolveMaxPlayers(int? maxPlayersFromArgs)
        {
            if (maxPlayersFromArgs.HasValue)
            {
                return Mathf.Max(1, maxPlayersFromArgs.Value);
            }

            if (matchRulesConfig == null)
            {
                return null;
            }

            return matchRulesConfig.ResolveMaxPlayers();
        }

        private bool ShouldStartDedicatedServer()
        {
            if (!DedicatedServerEnvironment.HasDedicatedFlag())
            {
                Debug.Log("DedicatedServerBootstrap requires '-dedicated' or '-dedicatedServer' flag to start server mode.");
                return false;
            }

#if UNITY_EDITOR
            if (allowNonHeadlessStartInEditor)
            {
                return true;
            }

            return DedicatedServerEnvironment.IsHeadless;
#else
            return DedicatedServerEnvironment.IsHeadless;
#endif
        }

        private static string GetArgumentValue(string[] args, string key)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static int? TryParseInt(string value)
        {
            if (int.TryParse(value, out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private sealed class DedicatedServerOptions
        {
            public string SessionName;
            public string Region;
            public int? Port;
            public int MaxPlayers;
        }
    }
}
