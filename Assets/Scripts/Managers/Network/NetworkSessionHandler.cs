using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
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
            {
                sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();
            }

            return sceneManager;
        }

        public async Task<bool> StartServer(NetworkRunner runner, int maxPlayers, int buildIndex, string sessionName = null, string region = null, int? port = null)
        {
            runner.ProvideInput = false;
            sessionName = string.IsNullOrWhiteSpace(sessionName) ? DefaultSessionName : sessionName;

            Debug.Log($"{LogPrefix} server started: initializing session='{sessionName}', sceneBuildIndex={buildIndex}, maxPlayers={maxPlayers}, region='{region ?? "auto"}', port={(port.HasValue ? port.Value.ToString() : "default")}");

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

            var regionApplied = TryApplyRegionOverride(args, region, out var regionEvidence);

            if (!string.IsNullOrWhiteSpace(region) && !regionApplied)
            {
                Debug.LogWarning($"{LogPrefix} region override was requested but cannot be enforced by this Fusion API surface. Requested region='{region}'. {regionEvidence}");
            }

            Debug.Log($"{LogPrefix} server startup args: session='{sessionName}', effectivePort={(port.HasValue ? requestedPort.ToString() : "default")}, requestedRegion='{region ?? "auto"}', regionApplied={regionApplied}");

            var result = await runner.StartGame(args);
            Debug.Log($"{LogPrefix} server started result: ok={result.Ok}, session='{sessionName}'");
            return result.Ok;
        }

        private static bool TryApplyRegionOverride(StartGameArgs args, string region, out string evidence)
        {
            evidence = "No region override API found in StartGameArgs.";

            if (string.IsNullOrWhiteSpace(region))
            {
                evidence = "No region requested.";
                return false;
            }

            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            var startGameArgsType = typeof(StartGameArgs);
            var customSettingsProperty = startGameArgsType.GetProperty("CustomPhotonAppSettings", flags);
            if (customSettingsProperty == null || !customSettingsProperty.CanWrite)
            {
                evidence = "StartGameArgs.CustomPhotonAppSettings is unavailable in this Fusion version.";
                return false;
            }

            var customSettings = customSettingsProperty.GetValue(args);
            if (customSettings == null)
            {
                try
                {
                    customSettings = Activator.CreateInstance(customSettingsProperty.PropertyType);
                }
                catch (Exception ex)
                {
                    evidence = $"Unable to create CustomPhotonAppSettings instance: {ex.GetType().Name}";
                    return false;
                }
            }

            var fixedRegionProperty = customSettingsProperty.PropertyType.GetProperty("FixedRegion", flags);
            if (fixedRegionProperty == null || !fixedRegionProperty.CanWrite)
            {
                evidence = "CustomPhotonAppSettings.FixedRegion is unavailable in this Fusion version.";
                return false;
            }

            fixedRegionProperty.SetValue(customSettings, region);
            customSettingsProperty.SetValue(args, customSettings);
            evidence = "Applied via StartGameArgs.CustomPhotonAppSettings.FixedRegion.";
            return true;
        }

        public async Task<bool> StartClient(NetworkRunner runner, string serverName, int buildIndex)
        {
            runner.ProvideInput = true;
            var sessionName = string.IsNullOrWhiteSpace(serverName) ? DefaultSessionName : serverName;

            Debug.Log($"{LogPrefix} client connecting: session='{sessionName}', sceneBuildIndex={buildIndex}");

            var args = new StartGameArgs()
            {
                GameMode = GameMode.Client,
                Scene = SceneRef.FromIndex(buildIndex),
                SceneManager = GetOrAddSceneManager(runner),
                SessionName = sessionName
            };

            var result = await runner.StartGame(args);
            Debug.Log($"{LogPrefix} client connect result: ok={result.Ok}, session='{sessionName}'");
            return result.Ok;
        }
    }
}
