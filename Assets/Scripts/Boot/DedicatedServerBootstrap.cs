using Fusion;
using System;
using System.Threading.Tasks;
using Common;
using Config;
using Helpers;
using Lobby;
using Network;
using UnityEngine;

namespace Boot
{
    public class DedicatedServerBootstrap : MonoBehaviour
    {
        [SerializeField] private MatchRulesConfig matchRulesConfig;
        [SerializeField] private float restartDelaySec = 2f;

        private const string SessionPrefix = "PongServer";

        private void Awake()
        {
            ReferenceValidator.ValidateOptional(matchRulesConfig, nameof(matchRulesConfig), this);
            if (matchRulesConfig != null)
                MatchRulesRegistry.RegisterProvider(new MatchRulesProvider(matchRulesConfig), this);
        }

        private async void Start()
        {
            if (!ShouldStartDedicatedServer())
            {
                return;
            }

            var options = BuildServerOptions();

            var lobbySceneIndex = SceneCatalog.GetLobbyIndex();
            if (lobbySceneIndex < 0)
            {
                Debug.LogError("[DedicatedServerBootstrap] Could not resolve Lobby scene index from SceneCatalog.");
                return;
            }

            while (true)
            {
                await RunServerSession(options, lobbySceneIndex);
                await Task.Delay(TimeSpan.FromSeconds(restartDelaySec));
            }
        }

        private async Task RunServerSession(DedicatedServerOptions options, int lobbySceneIndex)
        {
            var runnerObj = new GameObject("NetworkRunner", typeof(NetworkRunner));
            DontDestroyOnLoad(runnerObj);
            runnerObj.AddComponent<MatchSessionState>();
            var runner = runnerObj.GetComponent<NetworkRunner>();
            runner.ProvideInput = false;
            LobbyRunnerCallbacks.EnsureOnRunner(runner);

            var shutdownTcs = new TaskCompletionSource<ShutdownReason>();
            runner.AddCallbacks(new DedicatedServerMatchFlow(shutdownTcs));

            var sessionHandler = new NetworkSessionHandler();
            var ok = await sessionHandler.StartServer(runner, options.MaxPlayers, lobbySceneIndex, options.SessionName, options.Region, options.Port);
            if (!ok)
            {
                Destroy(runnerObj);
                return;
            }

            await shutdownTcs.Task;

            if (runnerObj != null)
                Destroy(runnerObj);
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

            return new DedicatedServerOptions
            {
                SessionName = sessionName,
                Region = region,
                Port = port,
                MaxPlayers = ResolveMaxPlayers(maxPlayersFromArgs)
            };
        }

        private static int ResolveMaxPlayers(int? maxPlayersFromArgs)
        {
            if (maxPlayersFromArgs.HasValue)
            {
                return Mathf.Max(1, maxPlayersFromArgs.Value);
            }

            return MatchModeExtensions.TwoVsTwoMaxPlayers + MatchRules.GetDedicatedServerSlots();
        }

        private bool ShouldStartDedicatedServer()
        {
            return DedicatedServerEnvironment.HasDedicatedFlag() && DedicatedServerEnvironment.IsHeadless;
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
