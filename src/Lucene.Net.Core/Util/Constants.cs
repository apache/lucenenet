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

namespace Lucene.Net.Util
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Global constants that Lucene.Net uses to make decisions based upon the environment
    /// that Lucene.Net is executing on.
    /// </summary>
    public class Constants
    {
        static Constants()
        {
            string version = null;
            try
            {
                version = typeof(Constants).GetTypeInfo().Assembly.GetName().Version.ToString();
            }
            catch (System.Security.SecurityException) //Ignore in medium trust.
            {
            }

            if(version == null)
            {
                version = MainVersionWithoutAlphaBeta + "-SNAPSHOT";
            }

            LUCENE_VERSION = version;
        }

        // TODO 5.9 determine if the constants for JVM & JAVA are actually needed.
        // public static readonly String JVM_VENDOR = AppSettings.Get("java.vm.vendor", "");
        // public static readonly String JVM_VERSION = AppSettings.Get("java.vm.version", "");
        // public static readonly String JVM_NAME = AppSettings.Get("java.vm.name", "");
        // public static readonly System.String JAVA_VERSION = AppSettings.Get("java.version", "");

        /// <summary>
        /// Determines if the KRE is 32 or 64 bit. 
        /// </summary>
        /// <remarks><para>JRE_IS_64BIT</para></remarks>
        public static readonly bool KRE_IS_64BIT = IntPtr.Size == 8;

        /// <summary>
        /// Returns the runtime version.
        /// </summary>
        public static readonly string KRE_VERSION = SystemProps.Get("Version");

        /// <summary>
        /// Gets the name of the operating system.
        /// </summary>
        public static readonly string OS_NAME = SystemProps.Get("OS");

        /// <summary>Returns true, if running on Linux. </summary>
        public static readonly bool LINUX = OS_NAME.StartsWith("Linux");
        /// <summary>Returns true, if running on Windows. </summary>
        public static readonly bool WINDOWS = OS_NAME.StartsWith("Windows");
        /// <summary>Returns true, if running on SunOS. </summary>
        public static readonly bool SUN_OS = OS_NAME.StartsWith("SunOS");
        /// <summary>Returns true, if running on Mac OS X. </summary>
        public static readonly bool MAC_OS_X = OS_NAME.StartsWith("Mac OS X");

        /// <summary>
        /// Gets the proccess architechture for the current machine.
        /// </summary>
        public static string OS_ARCH = SystemProps.Get("PROCESSOR_ARCHITECTURE");


        /// <summary>
        /// Gets the version of the operating system for the current machine.
        /// </summary>
        public static string OS_VERSION = SystemProps.Get("OS_VERSION", "?");


        /// <summary>
        /// This is the internal Lucene version, recorded into each segment.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         NOTE: we track per-segment version as a String with the {@code "X.Y"} format
        ///         (no minor version), e.g. {@code "4.0", "3.1", "3.0"}.
        ///  </para>
        ///     <para>
        ///         Alpha and Beta versions will have numbers like { @code "X.Y.0.Z"},
        ///         anything else is not allowed.This is done to prevent people from
        ///         using indexes created with ALPHA/BETA versions with the released version.
        ///     </para>
        /// </remarks>
        public static readonly System.String LUCENE_MAIN_VERSION = Ident("5.0");

        /// <summary>
        /// This is the Lucene version for display purposes.
        /// </summary>
        public static System.String LUCENE_VERSION;

        // this method prevents inlining the final version constant in compiled
        // classes,
        // see: http://www.javaworld.com/community/node/3400
        private static System.String Ident(System.String s)
        {
            return s.ToString();
        }

      

        public static string MainVersionWithoutAlphaBeta
        {
            get
            {
                var parts = LUCENE_MAIN_VERSION.Split('.');
                if (parts.Length == 4 && "0".Equals(parts[2]))
                {
                    return parts[0] + "." + parts[1];
                }

                return LUCENE_MAIN_VERSION;
            }
        }
    }

}
