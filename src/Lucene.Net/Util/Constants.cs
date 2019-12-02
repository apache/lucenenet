using System;
#if NETSTANDARD
using System.Runtime.InteropServices;
#else
using Microsoft.Win32;
#endif
using System.Text.RegularExpressions;

namespace Lucene.Net.Util
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Some useful constants.
    /// </summary>
    public static class Constants // LUCENENET specific - made static because all members are static and constructor in Lucene was private
    {
        // LUCENENET NOTE: IMPORTANT - this line must be placed before RUNTIME_VERSION so it can be parsed.
        private static Regex VERSION = new Regex(@"(\d+\.\d+(?:\.\d+)?(?:\.\d+)?)", RegexOptions.Compiled);

        // LUCENENET specific - renamed JAVA_VERSION to RUNTIME_VERSION and moved below OS constants because loading is dependent upon OS


        /// <summary>
        /// NOTE: This was JAVA_VENDOR in Lucene
        /// </summary>
        public static readonly string RUNTIME_VENDOR = "Microsoft"; // AppSettings.Get("java.vendor", "");
                                                                    //public static readonly string JVM_VENDOR = GetEnvironmentVariable("java.vm.vendor", "");
                                                                    //public static readonly string JVM_VERSION = GetEnvironmentVariable("java.vm.version", "");
                                                                    //public static readonly string JVM_NAME = GetEnvironmentVariable("java.vm.name", "");

#if NETSTANDARD
        /// <summary>
        /// The value of <see cref="RuntimeInformation.OSDescription"/>, excluding the version number.</summary>
#else
        /// <summary>
        /// The value of System.Environment.OSVersion.VersionString, excluding the version number.</summary>
#endif
        public static readonly string OS_NAME = LoadOSName();

        private static string LoadOSName() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
#if NETSTANDARD
            return VERSION.Replace(RuntimeInformation.OSDescription, string.Empty).Trim();
#else
            return VERSION.Replace(Environment.OSVersion.VersionString, string.Empty).Trim();
#endif
        }

        /// <summary>
        /// True iff running on Linux. </summary>
        public static readonly bool LINUX = LoadLinux();
        private static bool LoadLinux()
        {
#if NETSTANDARD
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
#else
            // 128 is mono's old platform tag for Unix.
            // Reference: https://stackoverflow.com/a/5117005
            int id = (int)Environment.OSVersion.Platform;
            return id == 4 || id == 128;
#endif
        }


        /// <summary>
        /// True iff running on Windows. </summary>
        public static readonly bool WINDOWS = LoadWindows();
        private static bool LoadWindows()
        {
#if NETSTANDARD
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
            PlatformID pid = Environment.OSVersion.Platform;
            return pid == PlatformID.Win32NT || pid == PlatformID.Win32Windows;
#endif
        }

        /// <summary>
        /// True iff running on SunOS. </summary>
        public static readonly bool SUN_OS = LoadSunOS();
        private static bool LoadSunOS()
        {
#if NETSTANDARD
            return RuntimeInformation.IsOSPlatform(OSPlatform.Create("SunOS"));
#else
            return false; // Not possible
#endif
        }

        /// <summary>
        /// True iff running on Mac OS X </summary>
        public static readonly bool MAC_OS_X = LoadMacOSX();
        private static bool LoadMacOSX() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
#if NETSTANDARD
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
#else
            // Reference: https://stackoverflow.com/a/5117005
            return Environment.OSVersion.Platform == PlatformID.MacOSX;
#endif
        }

        /// <summary>
        /// True iff running on FreeBSD </summary>
        public static readonly bool FREE_BSD = LoadFreeBSD();
        private static bool LoadFreeBSD() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
#if NETSTANDARD
            return RuntimeInformation.IsOSPlatform(OSPlatform.Create("FreeBSD"));
#else
            return false; // Not possible
#endif
        }


        public static readonly string OS_ARCH = LoadOSArch();
        private static string LoadOSArch() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
#if NETSTANDARD
            // Possible Values: X86, X64, Arm, Arm64
            return RuntimeInformation.OSArchitecture.ToString();
#else
            return Environment.Is64BitOperatingSystem ? "X64" : "X86";
#endif
        }


        public static readonly string OS_VERSION = LoadOSVersion();
        private static string LoadOSVersion() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
#if NETSTANDARD
            return ExtractString(RuntimeInformation.OSDescription, VERSION);
#else
            return Environment.OSVersion.Version.ToString();
#endif
        }

#if NETSTANDARD
        /// <summary>
        /// The value of the version parsed from <see cref="RuntimeInformation.FrameworkDescription"/>.
        /// <para/>
        /// NOTE: This was JAVA_VERSION in Lucene
        /// </summary>
#else
        /// <summary>
        /// The value of the currently installed .NET Framework version on Windows or <see cref="Environment.Version"/> on other operating systems.
        /// <para/>
        /// NOTE: This was JAVA_VERSION in Lucene
        /// </summary>
#endif
        public static readonly string RUNTIME_VERSION = LoadRuntimeVersion();

        private static string LoadRuntimeVersion() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
#if NETSTANDARD
            return ExtractString(RuntimeInformation.FrameworkDescription, VERSION);
#else
            return WINDOWS ? GetFramework45PlusFromRegistry() : Environment.Version.ToString();
#endif
        }


        //[Obsolete("We are not running on Java for heavens sake")]
        //public static readonly bool JRE_IS_MINIMUM_JAVA6 = (bool)new bool?(true); // prevent inlining in foreign class files

        //[Obsolete("We are not running on Java for heavens sake")]
        //public static readonly bool JRE_IS_MINIMUM_JAVA7 = (bool)new bool?(true); // prevent inlining in foreign class files

        //[Obsolete("We are not running on Java for heavens sake")]
        //public static readonly bool JRE_IS_MINIMUM_JAVA8;

        /// <summary>
        /// NOTE: This was JRE_IS_64BIT in Lucene
        /// </summary>
        public static readonly bool RUNTIME_IS_64BIT = LoadRuntimeIs64Bit(); // LUCENENET NOTE: We still need this constant to indicate 64 bit runtime.
        private static bool LoadRuntimeIs64Bit() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        {
            // LUCENENET NOTE: In Java, the check is for sun.misc.Unsafe.addressSize,
            // which is the pointer size of the current environment. We don't need to
            // fallback to the OS bitness in .NET because this property is reliable and 
            // doesn't throw exceptions.
            if (IntPtr.Size == 8)
                return true;// 64 bit machine
            else // if (IntPtr.Size == 4)
                return false;// 32 bit machine
        }

        // this method prevents inlining the final version constant in compiled classes,
        // see: http://www.javaworld.com/community/node/3400
        private static string Ident(string s)
        {
            return s.ToString();
        }

        // We should never change index format with minor versions, so it should always be x.y or x.y.0.z for alpha/beta versions!
        /// <summary>
        /// this is the internal Lucene version, recorded into each segment.
        /// NOTE: we track per-segment version as a <see cref="string"/> with the <c>"X.Y"</c> format
        /// (no minor version), e.g. <c>"4.0", "3.1", "3.0"</c>.
        /// <para/>Alpha and Beta versions will have numbers like <c>"X.Y.0.Z"</c>,
        /// anything else is not allowed. This is done to prevent people from
        /// using indexes created with ALPHA/BETA versions with the released version.
        /// </summary>
        public static readonly string LUCENE_MAIN_VERSION = Ident("4.8");

        // LUCENENET NOTE: This version is automatically updated by the
        // build script, so there is no need to change it here (although
        // it might make sense to change it when a major/minor/patch
        // port to Lucene is done).
        /// <summary>
        /// This is the Lucene version for display purposes.
        /// </summary>
        public static readonly string LUCENE_VERSION = "4.8.0";

        /// <summary>
        /// Returns a LUCENE_MAIN_VERSION without any ALPHA/BETA qualifier
        /// Used by test only!
        /// </summary>
        public static string MainVersionWithoutAlphaBeta()
        {
            string[] parts = MAIN_VERSION_WITHOUT_ALPHA_BETA.Split(LUCENE_MAIN_VERSION);
            if (parts.Length == 4 && "0".Equals(parts[2], StringComparison.Ordinal))
            {
                return parts[0] + "." + parts[1];
            }
            return LUCENE_MAIN_VERSION;
        }

        private static Regex MAIN_VERSION_WITHOUT_ALPHA_BETA = new Regex("\\.", RegexOptions.Compiled);

#if !NETSTANDARD

        // Gets the .NET Framework Version (if at least 4.5)
        // Reference: https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed
        private static string GetFramework45PlusFromRegistry()
        {
            const string subkey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";

            // As an alternative, if you know the computers you will query are running .NET Framework 4.5 
            // or later, you can use:
            using (RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(subkey))
            {
                if (ndpKey != null && ndpKey.GetValue("Release") != null)
                {
                    return CheckFor45PlusVersion((int)ndpKey.GetValue("Release"));
                }
                else
                {
                    // Fall back to Environment.Version (probably wrong, but this is our best guess if the registry check fails)
                    return Environment.Version.ToString();
                    //Console.WriteLine(".NET Framework Version 4.5 or later is not detected.");
                }
            }
        }

        // Checking the version using >= will enable forward compatibility.
        private static string CheckFor45PlusVersion(int releaseKey)
        {
            if (releaseKey >= 460799)
                return "4.8 or later";
            if (releaseKey >= 460798)
                return "4.7";
            if (releaseKey >= 394802)
                return "4.6.2";
            if (releaseKey >= 394254)
            {
                return "4.6.1";
            }
            if (releaseKey >= 393295)
            {
                return "4.6";
            }
            if ((releaseKey >= 379893))
            {
                return "4.5.2";
            }
            if ((releaseKey >= 378675))
            {
                return "4.5.1";
            }
            if ((releaseKey >= 378389))
            {
                return "4.5";
            }
            // This code should never execute. A non-null release key should mean
            // that 4.5 or later is installed.
            return "No 4.5 or later version detected";
        }

#endif

        // LUCENENET TODO: Move to Support ?
        /// <summary>
        /// Extracts the first group matched with the regex as a new string.
        /// </summary>
        /// <param name="input">The string to examine</param>
        /// <param name="pattern">A regex object to use to extract the string</param>
        private static string ExtractString(string input, Regex pattern)
        {
            Match m = pattern.Match(input);
            return (m.Groups.Count > 1) ? m.Groups[1].Value : string.Empty;
        }
    }
}