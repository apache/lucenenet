using System.IO;

namespace Lucene.Net.Benchmarks.ByTask.Utils
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
    /// File utilities.
    /// </summary>
    public static class FileUtils // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        /// <summary>
        /// Delete files and directories, even if non-empty.
        /// </summary>
        /// <param name="dir">File or directory.</param>
        /// <returns><c>true</c> on success, <c>false</c> if no or part of files have been deleted.</returns>
        /// <exception cref="IOException">If there is a low-level I/O error.</exception>
        public static bool FullyDelete(DirectoryInfo dir) 
        {
            try
            {
                Directory.Delete(dir.FullName, true);
                return true;
            }
            catch
            {
                return !Directory.Exists(dir.FullName);
            }
        }
    }
}
