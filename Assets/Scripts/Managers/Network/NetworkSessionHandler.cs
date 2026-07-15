using Fusion;
using Fusion.Sockets;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Managers;
using UnityEngine;

namespace Managers.Network
{
    public class NetworkSessionHandler
    {
        public const string DefaultSessionName = "PongServer";

        private const string LogPrefix = "[NetLifecycle]";

        private static INetworkSceneManager GetOrAddSceneManager(NetworkRunner runner)
        {
            var sceneManager = runner.GetComponent<NetworkSceneManagerDefault>();
            if (sceneManager == null)
                sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

            return sceneManager;
        }

        public async Task<bool> StartServer(NetworkRunner runner, int maxPlayers, int buildIndex, string sessionName = null, string region = null, int? port = null)
        {
            runner.ProvideInput = false;
            sessionName = string.IsNullOrWhiteSpace(sessionName) ? DefaultSessionName : sessionName;

            var args = new StartGameArgs()
            {
                GameMode = GameMode.Server,
                PlayerCount = maxPlayers,
                Scene = SceneRef.FromIndex(buildIndex),
                SceneManager = GetOrAddSceneManager(runner),
                SessionName = sessionName
            };

            var requestedPort = port.HasValue ? Mathf.Clamp(port.Value, 1, ushort.MaxValue) : 0;
            if (port.HasValue)
            {
                if (requestedPort != port.Value)
                {
                    Debug.LogWarning($"{LogPrefix} requested port '{port.Value}' is outside valid range 1-{ushort.MaxValue}; clamping to '{requestedPort}'.");
                }

                args.Address = NetAddress.Any((ushort)requestedPort);
            }

            var regionApplied = TryApplyRegionOverride(args, region);
            if (!string.IsNullOrWhiteSpace(region) && !regionApplied)
                Debug.LogWarning($"{LogPrefix} region override requested for '{region}', but this Fusion version does not expose a writable FixedRegion path.");

            var result = await runner.StartGame(args);
            Debug.Log($"{LogPrefix} server start ok={result.Ok}, session='{sessionName}', sceneBuildIndex={buildIndex}, maxPlayers={maxPlayers}, region='{region ?? "auto"}', port={(port.HasValue ? requestedPort.ToString() : "default")}");
            return result.Ok;
        }

        private static bool TryApplyRegionOverride(StartGameArgs args, string region)
        {
            if (string.IsNullOrWhiteSpace(region))
                return false;

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var startGameArgsType = typeof(StartGameArgs);
            var customSettingsProperty = startGameArgsType.GetProperty("CustomPhotonAppSettings", flags);
            if (customSettingsProperty == null || !customSettingsProperty.CanWrite)
                return false;

            var customSettings = customSettingsProperty.GetValue(args);
            if (customSettings == null)
            {
                try
                {
                    customSettings = Activator.CreateInstance(customSettingsProperty.PropertyType);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"{LogPrefix} could not create CustomPhotonAppSettings for region override: {ex.GetType().Name}");
                    return false;
                }
            }

            var fixedRegionProperty = customSettingsProperty.PropertyType.GetProperty("FixedRegion", flags);
            if (fixedRegionProperty == null || !fixedRegionProperty.CanWrite)
                return false;

            fixedRegionProperty.SetValue(customSettings, region);
            customSettingsProperty.SetValue(args, customSettings);
            return true;
        }

        public async Task<bool> StartClient(NetworkRunner runner, string serverName)
        {
            runner.ProvideInput = true;
            var sessionName = string.IsNullOrWhiteSpace(serverName) ? DefaultSessionName : serverName;

            Debug.Log($"{LogPrefix} client connecting: session='{sessionName}'");

            var args = new StartGameArgs()
            {
                GameMode = GameMode.Client,
                ConnectionToken = LobbyUsernameTokenUtility.CreateConnectionToken(LocalPlayerSession.Username),
                SceneManager = GetOrAddSceneManager(runner),
                SessionName = sessionName
            };

            var result = await runner.StartGame(args);
            Debug.Log($"{LogPrefix} client connect ok={result.Ok}, session='{sessionName}'");
            return result.Ok;
        }

        public async Task<bool> JoinLobbyAsync(NetworkRunner runner, SessionLobby lobby = SessionLobby.ClientServer)
        {
            runner.ProvideInput = false;

            Debug.Log($"{LogPrefix} joining session lobby: lobby='{lobby}'");

            var result = await runner.JoinSessionLobby(lobby);

            Debug.Log($"{LogPrefix} join lobby ok={result.Ok}, lobby='{lobby}'");
            return result.Ok;
        }
    }
}
