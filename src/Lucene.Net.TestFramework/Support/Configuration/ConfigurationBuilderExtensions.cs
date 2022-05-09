using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace Lucene.Net.Configuration
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
    /// Extension methods for <see cref="IConfigurationBuilder"/>.
    /// </summary>
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Helper Extension method to add a <see cref="TestParameterConfigurationSource"/>
        /// Uses the <see cref="NUnit.Framework.TestContext.Parameters"/> to build provider.
        /// </summary>
        /// <param name="builder">This <see cref="IConfigurationBuilder"/>.</param>
        /// <returns>This <see cref="IConfigurationBuilder"/>.</returns>
        [CLSCompliant(false)]
        public static IConfigurationBuilder AddNUnitTestRunSettings(this IConfigurationBuilder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            return builder.Add(new TestParameterConfigurationSource() { TestParameters = NUnit.Framework.TestContext.Parameters });
        }

        /// <summary>
        /// Scans from <paramref name="currentPath"/> to the root directory looking for <paramref name="fileName"/> configuration settings.
        /// This loads a Json Configuration provider in ascending hierarchy.
        /// </summary>
        /// <param name="builder">This <see cref="IConfigurationBuilder"/>.</param>
        /// <param name="currentPath">The current path to start in.</param>
        /// <param name="fileName">The filename to be searched for.</param>
        /// <returns>This <see cref="IConfigurationBuilder"/>.</returns>
        [CLSCompliant(false)]
        public static IConfigurationBuilder AddJsonFilesFromRootDirectoryTo(this IConfigurationBuilder builder, string currentPath, string fileName)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            Stack<string> locations = ScanConfigurationFiles(currentPath, fileName);

            while (locations.Count != 0)
            {
                builder.AddJsonFile(locations.Pop(), optional: true, reloadOnChange: true);
            }
            return builder;
        }

        /// <summary>
        /// Scans from <paramref name="currentPath"/> to the root directory looking for <paramref name="fileName"/> configuration settings.
        /// This loads a XML Configuration provider in ascending hierarchy.
        /// </summary>
        /// <param name="builder">This <see cref="IConfigurationBuilder"/>.</param>
        /// <param name="currentPath">The current path to start in.</param>
        /// <param name="fileName">The filename to be searched for.</param>
        /// <returns>This <see cref="IConfigurationBuilder"/>.</returns>
        [CLSCompliant(false)]
        public static IConfigurationBuilder AddXmlFilesFromRootDirectoryTo(this IConfigurationBuilder builder, string currentPath, string fileName)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            Stack<string> locations = ScanConfigurationFiles(currentPath, fileName);

            while (locations.Count != 0)
            {
                builder.AddXmlFile(locations.Pop(), optional: true, reloadOnChange: true);
            }
            return builder;
        }

        private static Stack<string> ScanConfigurationFiles(string currentPath, string fileName)
        {
            Stack<string> locations = new Stack<string>();

            string candidatePath = System.IO.Path.Combine(currentPath, fileName);
            if (File.Exists(candidatePath))
            {
                locations.Push(candidatePath);
            }

            try
            {
                while (new DirectoryInfo(currentPath).Parent != null)
                {
                    candidatePath = System.IO.Path.Combine(new DirectoryInfo(currentPath).Parent.FullName, fileName);
                    if (File.Exists(candidatePath))
                    {
                        locations.Push(candidatePath);
                    }
                    currentPath = new DirectoryInfo(currentPath).Parent.FullName;
                }
            }
            catch (SecurityException)
            {
                // ignore security errors
            }
            return locations;
        }
    }
}
