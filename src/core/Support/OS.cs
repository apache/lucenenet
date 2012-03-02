using System;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Provides platform infos.
    /// </summary>
    public class OS
    {
        static bool isUnix;
        static bool isWindows;

        static OS()
        {
            PlatformID pid = Environment.OSVersion.Platform;
            isWindows = pid == PlatformID.Win32NT || pid == PlatformID.Win32Windows;

            // we use integers instead of enum tags because "MacOS"
            // requires 2.0 SP2, 3.0 SP2 or 3.5 SP1.
            // 128 is mono's old platform tag for Unix.
            int id = (int)pid;
            isUnix = id == 4 || id == 6 || id == 128;
        }

        /// <summary>
        /// Whether we run under a Unix platform.
        /// </summary>
        public static bool IsUnix
        {
            get { return isUnix; }
        }

        /// <summary>
        /// Whether we run under a supported Windows platform.
        /// </summary>
        public static bool IsWindows
        {
            get { return isWindows; }
        }
    }
}