using Lucene.Net.Store;
using System;
using System.IO;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Replicator
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
    /// A <see cref="ISourceDirectoryFactory"/> which returns <see cref="FSDirectory"/> under a
    /// dedicated session directory. When a session is over, the entire directory is
    /// deleted.
    /// </summary>
    /// <remarks>
    /// @lucene.experimental
    /// </remarks>
    public class PerSessionDirectoryFactory : ISourceDirectoryFactory
    {
        private readonly string workingDirectory;

        /// <summary>Constructor with the given sources mapping.</summary>
        public PerSessionDirectoryFactory(string workingDirectory)
        {
            this.workingDirectory = workingDirectory;
        }

        public virtual Directory GetDirectory(string sessionId, string source)
        {
            string sourceDirectory = Path.Combine(workingDirectory, sessionId, source);
            System.IO.Directory.CreateDirectory(sourceDirectory);
            if (!System.IO.Directory.Exists(sourceDirectory))
                throw new IOException("failed to create source directory " + sourceDirectory);
            return FSDirectory.Open(sourceDirectory);
        }

        public virtual void CleanupSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId)) throw new ArgumentException("sessionID cannot be empty", nameof(sessionId));

            string sessionDirectory = Path.Combine(workingDirectory, sessionId);
            System.IO.Directory.Delete(sessionDirectory, true);
        }
    }
}