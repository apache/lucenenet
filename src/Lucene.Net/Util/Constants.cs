using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
#if NETSTANDARD
using System.Runtime.InteropServices;
#else
using System.Diagnostics;
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
    public sealed class Constants
    {
        private Constants() // can't construct
        {
        }

        // LUCENENET NOTE: IMPORTANT - this line must be placed before RUNTIME_VERSION so it can be parsed.
        private static Regex VERSION_PARSER = new Regex(@"(\d+\.\d+\.\d+\.\d+)", RegexOptions.Compiled);

#if NETSTANDARD
        /// <summary>
        /// The value of the version parsed from <see cref="RuntimeInformation.FrameworkDescription"/>.
        /// <para/>
        /// NOTE: This was JAVA_VERSION in Lucene
        /// </summary>
#else
        /// <summary>
        /// The value of <see cref="Environment.Version"/>.
        /// <para/>
        /// NOTE: This was JAVA_VERSION in Lucene
        /// </summary>
#endif
        public static readonly string RUNTIME_VERSION = GetEnvironmentVariable("RUNTIME_VERSION", "?");


        /// <summary>
        /// NOTE: This was JAVA_VENDOR in Lucene
        /// </summary>
        public static readonly string RUNTIME_VENDOR = "Microsoft"; // AppSettings.Get("java.vendor", "");
        //public static readonly string JVM_VENDOR = GetEnvironmentVariable("java.vm.vendor", "");
        //public static readonly string JVM_VERSION = GetEnvironmentVariable("java.vm.version", "");
        //public static readonly string JVM_NAME = GetEnvironmentVariable("java.vm.name", "");

        /// <summary>
        /// The value of <see cref="Environment.GetEnvironmentVariable(string)"/> with parameter "OS".</summary>
        public static readonly string OS_NAME = GetEnvironmentVariable("OS", "Windows_NT") ?? "Linux";

        /// <summary>
        /// True iff running on Linux. </summary>
        public static readonly bool LINUX = OS_NAME.StartsWith("Linux", StringComparison.Ordinal);

        /// <summary>
        /// True iff running on Windows. </summary>
        public static readonly bool WINDOWS = OS_NAME.StartsWith("Windows", StringComparison.Ordinal);

        /// <summary>
        /// True iff running on SunOS. </summary>
        public static readonly bool SUN_OS = OS_NAME.StartsWith("SunOS", StringComparison.Ordinal);

        /// <summary>
        /// True iff running on Mac OS X </summary>
        public static readonly bool MAC_OS_X = OS_NAME.StartsWith("Mac OS X", StringComparison.Ordinal);

        /// <summary>
        /// True iff running on FreeBSD </summary>
        public static readonly bool FREE_BSD = OS_NAME.StartsWith("FreeBSD", StringComparison.Ordinal);

        public static readonly string OS_ARCH = GetEnvironmentVariable("PROCESSOR_ARCHITECTURE", "x86");
        public static readonly string OS_VERSION = GetEnvironmentVariable("OS_VERSION", "?");

        //[Obsolete("We are not running on Java for heavens sake")]
        //public static readonly bool JRE_IS_MINIMUM_JAVA6 = (bool)new bool?(true); // prevent inlining in foreign class files

        //[Obsolete("We are not running on Java for heavens sake")]
        //public static readonly bool JRE_IS_MINIMUM_JAVA7 = (bool)new bool?(true); // prevent inlining in foreign class files

        //[Obsolete("We are not running on Java for heavens sake")]
        //public static readonly bool JRE_IS_MINIMUM_JAVA8;

        /// <summary>
        /// NOTE: This was JRE_IS_64BIT in Lucene
        /// </summary>
        public static readonly bool RUNTIME_IS_64BIT; // LUCENENET NOTE: We still need this constant to indicate 64 bit runtime.

        static Constants()
        {
            if (IntPtr.Size == 8)
            {
                RUNTIME_IS_64BIT = true;// 64 bit machine
            }
            else if (IntPtr.Size == 4)
            {
                RUNTIME_IS_64BIT = false;// 32 bit machine
            }

            //LUCENENET NOTE: Well that was all over the top to check architechture
            //bool is64Bit = false;
            //try
            //{
            //  Type unsafeClass = Type.GetType("sun.misc.Unsafe");
            //  Field unsafeField = unsafeClass.getDeclaredField("theUnsafe");
            //  unsafeField.Accessible = true;
            //  object @unsafe = unsafeField.get(null);
            //  int addressSize = (int)((Number) unsafeClass.GetMethod("addressSize").invoke(@unsafe));
            //  is64Bit = addressSize >= 8;
            //}
            //catch (Exception e)
            //{
            //  string x = System.getProperty("sun.arch.data.model");
            //  if (x != null)
            //  {
            //    is64Bit = x.IndexOf("64") != -1;
            //  }
            //  else
            //  {
            //    if (OS_ARCH != null && OS_ARCH.IndexOf("64") != -1)
            //    {
            //      is64Bit = true;
            //    }
            //    else
            //    {
            //      is64Bit = false;
            //    }
            //  }
            //}
            //RUNTIME_IS_64BIT = is64Bit;

            //// this method only exists in Java 8:
            //bool v8 = true;
            //try
            //{
            //  typeof(Collections).getMethod("emptySortedSet");
            //}
            //catch (NoSuchMethodException nsme)
            //{
            //  v8 = false;
            //}
            //JRE_IS_MINIMUM_JAVA8 = v8;
            //Package pkg = LucenePackage.Get();
            //string v = (pkg == null) ? null : pkg.ImplementationVersion;
            //if (v == null)
            //{
            //  v = MainVersionWithoutAlphaBeta() + "-SNAPSHOT";
            //}
            //LUCENE_VERSION = Ident(v);
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
        

#region MEDIUM-TRUST Support

        private static string GetEnvironmentVariable(string variable, string defaultValueOnSecurityException)
        {
            try
            {
                if (variable == "OS_VERSION")
                {
#if NETSTANDARD
                    return RuntimeInformation.OSDescription;
#else
                    return Environment.OSVersion.ToString();
#endif
                }
				
                if (variable == "PROCESSOR_ARCHITECTURE") {
                    return RuntimeInformation.OSArchitecture.ToString();
                }

                if (variable == "RUNTIME_VERSION")
                {
#if NETSTANDARD
                    return ExtractString(RuntimeInformation.FrameworkDescription, VERSION_PARSER);
#else
                    return Environment.Version.ToString();
#endif
                }

                return System.Environment.GetEnvironmentVariable(variable);
            }
            catch (System.Security.SecurityException)
            {
                return defaultValueOnSecurityException;
            }
        }

#endregion MEDIUM-TRUST Support

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