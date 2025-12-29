using System;
using System.Threading;

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
    /// Helper class for tests that need to modify system environment state.
    /// Provides thread-safe access to Environment.CurrentDirectory.
    ///
    /// LUCENENET specific: Created to prevent race conditions when tests that
    /// modify Environment.CurrentDirectory run in parallel (Issue #832).
    /// </summary>
    public static class SystemEnvironment
    {
        /// <summary>
        /// A shared semaphore to synchronize access to Environment.CurrentDirectory.
        /// Only one test at a time should be allowed to change the current directory.
        /// </summary>
        private static readonly SemaphoreSlim currentDirectoryLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Executes an action with exclusive access to Environment.CurrentDirectory.
        /// The current directory is automatically restored after the action completes.
        /// </summary>
        /// <param name="tempDirectory">The temporary directory to switch to</param>
        /// <param name="action">The action to execute in the temporary directory</param>
        public static void WithCurrentDirectory(string tempDirectory, Action action)
        {
            if (tempDirectory == null)
                throw new ArgumentNullException(nameof(tempDirectory));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            currentDirectoryLock.Wait();
            string originalDirectory = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = tempDirectory;
                action();
            }
            finally
            {
                Environment.CurrentDirectory = originalDirectory;
                currentDirectoryLock.Release();
            }
        }

        /// <summary>
        /// Executes a function with exclusive access to Environment.CurrentDirectory.
        /// The current directory is automatically restored after the function completes.
        /// </summary>
        /// <param name="tempDirectory">The temporary directory to switch to</param>
        /// <param name="func">The function to execute in the temporary directory</param>
        /// <returns>The result of the function</returns>
        public static T WithCurrentDirectory<T>(string tempDirectory, Func<T> func)
        {
            if (tempDirectory == null)
                throw new ArgumentNullException(nameof(tempDirectory));
            if (func == null)
                throw new ArgumentNullException(nameof(func));

            currentDirectoryLock.Wait();
            string originalDirectory = Environment.CurrentDirectory;
            try
            {
                Environment.CurrentDirectory = tempDirectory;
                return func();
            }
            finally
            {
                Environment.CurrentDirectory = originalDirectory;
                currentDirectoryLock.Release();
            }
        }

        /// <summary>
        /// Acquires the lock for Environment.CurrentDirectory modifications.
        /// Must be used with ReleaseCurrentDirectoryLock in a try-finally block.
        ///
        /// Usage:
        /// <code>
        /// SystemEnvironment.AcquireCurrentDirectoryLock();
        /// string originalDir = Environment.CurrentDirectory;
        /// try
        /// {
        ///     Environment.CurrentDirectory = newDir;
        ///     // ... do work ...
        /// }
        /// finally
        /// {
        ///     Environment.CurrentDirectory = originalDir;
        ///     SystemEnvironment.ReleaseCurrentDirectoryLock();
        /// }
        /// </code>
        /// </summary>
        public static void AcquireCurrentDirectoryLock()
        {
            currentDirectoryLock.Wait();
        }

        /// <summary>
        /// Releases the lock for Environment.CurrentDirectory modifications.
        /// Must be called after AcquireCurrentDirectoryLock.
        /// </summary>
        public static void ReleaseCurrentDirectoryLock()
        {
            currentDirectoryLock.Release();
        }
    }
}
