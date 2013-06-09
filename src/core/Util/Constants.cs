/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Support;
using LucenePackage = Lucene.Net.LucenePackage;

namespace Lucene.Net.Util
{

    /// <summary> Some useful constants.</summary>
    public sealed class Constants
    {
        private Constants()
        {
        } // can't construct

        public static readonly String JVM_VENDOR = AppSettings.Get("java.vm.vendor", "");
        public static readonly String JVM_VERSION = AppSettings.Get("java.vm.version", "");
        public static readonly String JVM_NAME = AppSettings.Get("java.vm.name", "");

        /// <summary>The value of <tt>System.getProperty("java.version")</tt>. *</summary>
        public static readonly System.String JAVA_VERSION = AppSettings.Get("java.version", "");
        
        /// <summary>The value of <tt>System.getProperty("os.name")</tt>. *</summary>
        public static readonly System.String OS_NAME = GetEnvironmentVariable("OS", "Windows_NT") ?? "Linux";
        /// <summary>True iff running on Linux. </summary>
        public static readonly bool LINUX = OS_NAME.StartsWith("Linux");
        /// <summary>True iff running on Windows. </summary>
        public static readonly bool WINDOWS = OS_NAME.StartsWith("Windows");
        /// <summary>True iff running on SunOS. </summary>
        public static readonly bool SUN_OS = OS_NAME.StartsWith("SunOS");
        /// <summary>True iff running on Mac OS X. </summary>
        public static readonly bool MAC_OS_X = OS_NAME.StartsWith("Mac OS X");

        public static readonly System.String OS_ARCH = GetEnvironmentVariable("PROCESSOR_ARCHITECTURE", "x86");
        public static readonly System.String OS_VERSION = GetEnvironmentVariable("OS_VERSION", "?");
        public static readonly System.String JAVA_VENDOR = AppSettings.Get("java.vendor", "");

        // NOTE: this logic may not be correct; if you know of a
        // more reliable approach please raise it on java-dev!
        public static bool JRE_IS_64BIT;

        static Constants()
        {
            if (IntPtr.Size == 8)
            {
                JRE_IS_64BIT = true;// 64 bit machine
            }
            else if (IntPtr.Size == 4)
            {
                JRE_IS_64BIT = false;// 32 bit machine
            }

            try
            {
                LUCENE_VERSION = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
            catch (System.Security.SecurityException) //Ignore in medium trust.
            {
            }

        }

        // this method prevents inlining the final version constant in compiled
        // classes,
        // see: http://www.javaworld.com/community/node/3400
        private static System.String Ident(System.String s)
        {
            return s.ToString();
        }

        public static readonly System.String LUCENE_MAIN_VERSION = Ident("4.3.1");

        public static System.String LUCENE_VERSION = "8.8.8.8";
        

        #region MEDIUM-TRUST Support
        static string GetEnvironmentVariable(string variable, string defaultValueOnSecurityException)
        {
            try
            {
                if (variable == "OS_VERSION") return System.Environment.OSVersion.ToString();

                return System.Environment.GetEnvironmentVariable(variable);
            }
            catch (System.Security.SecurityException)
            {
                return defaultValueOnSecurityException;
            }

        }
        #endregion
    }
}