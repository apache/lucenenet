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
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Lucene.Net.Util
{
    /// <summary>
    /// The static accessor class for file paths used in testing. 
    /// </summary>
    public static class Paths
    {
        private static string s_tempDirectory = null;
        private static string s_testDocDirectory = null;
        private static string s_assemblyDirectory = null;
        private static string s_projectRootDirectory = null;

        /// <summary>
        /// Gets the temp directory.
        /// </summary>
        /// <value>
        /// The temp directory.
        /// </value>
        /// <remarks>
        /// 	<para>
        /// 		The temp directory first looks at the app settings for the &qt;tempDir&qt; 
        /// 		key. If the path does not exists or the setting is empty, the temp directory
        /// 		fall back to using the environment's temp directory path and 
        /// 		append &qt;lucene-net&qt; to it. 
        /// 	</para>
        /// </remarks>
        public static string TempDirectory
        {
            get
            {
                if (s_tempDirectory == null)
                {
                   
                    string tempDirectory = SupportClass.AppSettings.Get("tempDir", "");

                    if (string.IsNullOrWhiteSpace(tempDirectory) ||
                        !Directory.Exists(tempDirectory))
                    {
                        tempDirectory = CombinePath(Path.GetTempPath(), "lucene-net");

                        if (!Directory.Exists(tempDirectory))
                            Directory.CreateDirectory(tempDirectory);

                    }
                    s_tempDirectory = tempDirectory;
                }

                return s_tempDirectory;
            }
        }

        /// <summary>
        /// Gets the test document directory.
        /// </summary>
        /// <value>
        /// The test document directory.
        /// </value>
        public static string TestDocDirectory
        {
            get
            {
                if (s_testDocDirectory == null)
                {
                    s_testDocDirectory = CombinePath(TempDirectory, "TestDoc");
                }
                return s_testDocDirectory;
            }
        }

        /// <summary>
        /// Gets the directory where the compiled assembly Lucene.Net.Tests is found.
        /// We use Assembly.CodeBase in case NUnit or the current test runner 
        /// has shadow copy enabled. 
        /// </summary>
        public static string AssemblyDirectory
        {
            get
            {
                if (s_assemblyDirectory == null)
                {
                    s_assemblyDirectory = typeof(Paths).Assembly.CodeBase;


                    // ensure that we're only getting the directory.
                    s_assemblyDirectory = Path.GetDirectoryName(s_assemblyDirectory);

                    // CodeBase uses unc path, get rid of the file prefix if it exists.  
                    if (s_assemblyDirectory.StartsWith("file:"))
                        s_assemblyDirectory = s_assemblyDirectory.Replace(("file:" + Path.DirectorySeparatorChar).ToString(), "");
                }
                return s_assemblyDirectory;
            }
        }

        /// <summary>
        /// Gets the root directory for the project. e.g. if you were working on trunk
        /// it would be the trunk directory. 
        /// </summary>
        public static string ProjectRootDirectory
        {
            get
            {
                if (s_projectRootDirectory == null)
                {
                    // we currently assume that the assembly's directory is root/bin/[Section]/[Build]
                    // where [Section] is either core, demo, or contrib, and [Build] is either Debug or Release.  
                    string assemblyLocation = AssemblyDirectory;
                    int index = -1;
                    if (assemblyLocation.IndexOf("build") > -1)
                        index = assemblyLocation.IndexOf(Path.DirectorySeparatorChar + "build" + Path.DirectorySeparatorChar);
                    else
                        index = assemblyLocation.IndexOf(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar);

                    int difference = assemblyLocation.Substring(index).Count(o => o == Path.DirectorySeparatorChar);

                    var list = new List<string>();

                    for (int i = 0; i < difference; i++)
                        list.Add("..");

                    var parameters = list.ToArray();

                    s_projectRootDirectory = Path.GetFullPath(CombinePath(assemblyLocation, parameters));

                    //TODO: remove
                    Console.WriteLine(s_projectRootDirectory);
                }
                return s_projectRootDirectory;
            }

        }

        /// <summary>
        /// Combines the path.
        /// </summary>
        /// <returns>
        /// The path.
        /// </returns>
        /// <param name='startSegment'>
        /// Start segment of the path.
        /// </param>
        /// <param name='segments'>
        /// Path segments e.g. directory or file names.
        /// </param>
        /// <exception cref='ArgumentNullException'>
        /// Is thrown when an argument passed to a method is invalid because it is <see langword="null" /> .
        /// </exception>
        /// <exception cref='InvalidOperationException'>
        /// Is thrown when an operation cannot be performed.
        /// </exception>
        internal static string CombinePath(string startSegment, params string[] segments)
        {
            if (startSegment == null)
                throw new ArgumentNullException(startSegment);

            if (segments == null || segments.Length == 0)
                throw new InvalidOperationException("Paths can not be combined" +
                    " unless the startSegment and one additional segment is present.");

            string path = startSegment;

            foreach (string segment in segments)
                path = System.IO.Path.Combine(path, segment);

            return path;
        }
    }
}