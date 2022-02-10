// LUCENENET NOTE: This class appears to be Java-specific, so it is being excluded.

//using Lucene.Net.Support;
//using System.Globalization;
//using System.Threading;

//namespace Lucene.Net.Util
//{
//    /*
//     * Licensed to the Apache Software Foundation (ASF) under one or more
//     * contributor license agreements.  See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     *     http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

//    /// <summary>
//    /// A default <seealso cref="ThreadFactory"/> implementation that accepts the name prefix
//    /// of the created threads as a constructor argument. Otherwise, this factory
//    /// yields the same semantics as the thread factory returned by
//    /// <seealso cref="Executors#defaultThreadFactory()"/>.
//    /// </summary>
//    public class NamedThreadFactory : ThreadFactory
//    {
//        private static int ThreadPoolNumber = 1;
//        private int ThreadNumber = 1;
//        private const string NAME_PATTERN = "{0}-{1}-thread";
//        private readonly string ThreadNamePrefix;

//        /// <summary>
//        /// Creates a new <seealso cref="NamedThreadFactory"/> instance
//        /// </summary>
//        /// <param name="threadNamePrefix"> the name prefix assigned to each thread created. </param>
//        public NamedThreadFactory(string threadNamePrefix)
//        {
//            this.ThreadNamePrefix = string.Format(CultureInfo.InvariantCulture, NAME_PATTERN,
//            CheckPrefix(threadNamePrefix), Interlocked.Increment(ref ThreadPoolNumber));
//        }

//        private static string CheckPrefix(string prefix)
//        {
//            return prefix is null || prefix.Length == 0 ? "Lucene" : prefix;
//        }

//        /// <summary>
//        /// Creates a new <seealso cref="Thread"/>
//        /// </summary>
//        /// <seealso cref= java.util.concurrent.ThreadFactory#newThread(java.lang.Runnable) </seealso>
//        public override Thread NewThread(IThreadRunnable r)
//        {
//            Thread t = new Thread(r.Run)
//            {
//                Name = string.Format(CultureInfo.InvariantCulture, "{0}-{1}", this.ThreadNamePrefix, Interlocked.Increment(ref ThreadNumber)),
//                IsBackground = false,
//            };

//            return t;
//        }
//    }
//}