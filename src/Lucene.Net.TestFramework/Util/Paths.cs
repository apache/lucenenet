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

using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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
                    string tempDirectory = AppSettings.Get("tempDir", "");

                    if (string.IsNullOrEmpty(tempDirectory) ||
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
                    // CodeBase uses unc path, get rid of the file prefix if it exists.
                    // File prefix could be file:// or file:///
                    var assemblyDirectoryUri = new Uri(typeof(Paths).GetTypeInfo().Assembly.Location);
                    s_assemblyDirectory = Path.GetDirectoryName(assemblyDirectoryUri.LocalPath);
                }
                return s_assemblyDirectory;
            }
        }

        private const string TestArtifactsFolder = "test-files";

        public static string ResolveTestArtifactPath(string fileName)
        {
            var rootPath = ProjectRootDirectory;
            while (true)
            {
                var possiblePath = Path.Combine(rootPath, TestArtifactsFolder);
                if (Directory.Exists(possiblePath)) return Path.Combine(possiblePath, fileName);
                
                var parent = Directory.GetParent(rootPath);
                if (parent == parent.Root) break;
                rootPath = parent.FullName;
            }
            throw new FileNotFoundException("Could not find the test-files folder");
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
                    var assemblyLocation = AssemblyDirectory;

                    var index = assemblyLocation.IndexOf(Path.DirectorySeparatorChar + "build" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
                    if (index == -1)
                    {
                        index = assemblyLocation.IndexOf(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
                    }

                    if (index < 0)
                    {
                        throw new ArgumentOutOfRangeException("Could not locate project root directory in " + assemblyLocation);
                    }

                    var difference = assemblyLocation.Substring(index).Count(o => o == Path.DirectorySeparatorChar);

                    var list = new List<string>();

                    for (int i = 0; i < difference; i++)
                    {
                        list.Add("..");
                    }

                    var parameters = list.ToArray();

                    s_projectRootDirectory = Path.GetFullPath(CombinePath(assemblyLocation, parameters));
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