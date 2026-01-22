using System;
using System.IO;
using System.Runtime.InteropServices;

namespace MWDMockServer
{
    /// <summary>
    /// Helper class for cross-platform file system operations
    /// </summary>
    public static class CrossPlatformHelper
    {
        /// <summary>
        /// Sets file attributes in a cross-platform safe manner
        /// On Windows: Sets the specified attributes
        /// On Unix/Linux/macOS: Operations are ignored as FileAttributes are Windows-specific
        /// </summary>
        public static void SetFileAttributesSafe(string path, FileAttributes attributes)
        {
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                // Only attempt to set file attributes on Windows
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    File.SetAttributes(path, attributes);
                }
                // On Unix-based systems (Linux, macOS), file attributes work differently
                // and the Windows-specific FileAttributes enum doesn't apply
            }
            catch (Exception)
            {
                // Silently ignore attribute setting errors to maintain cross-platform compatibility
                // This is especially important during cleanup operations
            }
        }

        /// <summary>
        /// Resets file attributes to normal (removes readonly, hidden, etc.) in a cross-platform safe manner
        /// </summary>
        public static void ResetFileAttributes(string path)
        {
            SetFileAttributesSafe(path, FileAttributes.Normal);
        }

        /// <summary>
        /// Checks if the current platform is Windows
        /// </summary>
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Checks if the current platform is Linux
        /// </summary>
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        /// <summary>
        /// Checks if the current platform is macOS
        /// </summary>
        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>
        /// Gets the platform-specific configuration file name
        /// </summary>
        public static string GetPlatformSpecificConfigFile()
        {
            if (IsWindows)
                return "appsettings.Windows.json";
            else if (IsLinux)
                return "appsettings.Linux.json";
            else if (IsMacOS)
                return "appsettings.macOS.json";
            else
                return "appsettings.json";
        }
    }
}