using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Helpers
{
    public static class DedicatedServerEnvironment
    {
        public static bool HasDedicatedFlag()
        {
            var args = Environment.GetCommandLineArgs();
            return Array.Exists(args, arg =>
                string.Equals(arg, "-dedicated", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-dedicatedServer", StringComparison.OrdinalIgnoreCase));
        }

        public static bool HasBatchModeFlag()
        {
            var args = Environment.GetCommandLineArgs();
            return Array.Exists(args, arg => string.Equals(arg, "-batchmode", StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsHeadless =>
            Application.isBatchMode
            || HasBatchModeFlag()
            || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
    }
}
