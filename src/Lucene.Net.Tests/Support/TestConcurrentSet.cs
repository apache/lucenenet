// Some tests adapted from Apache Harmony:
// https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/concurrent/src/test/java/ConcurrentHashMapTest.java
// https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/luni/src/test/api/common/org/apache/harmony/luni/tests/java/util/CollectionsTest.java

using Lucene.Net.Attributes;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net
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
    /// Tests for <see cref="ConcurrentSet{T}"/>.
    /// </summary>
    /// <remarks>
    /// See the <see cref="BaseConcurrentSetTestCase"/> class for most of the test cases.
    /// This class specializes the tests for the <see cref="ConcurrentSet{T}"/> class.
    /// </remarks>
    public class TestConcurrentSet : BaseConcurrentSetTestCase
    {
        protected override ISet<T> NewSet<T>()
            => new ConcurrentSet<T>(new JCG.LinkedHashSet<T>());

        protected override ISet<T> NewSet<T>(ISet<T> set)
            => new ConcurrentSet<T>(set);

        /// <summary>
        /// Tests the <see cref="ConcurrentSet{T}.SyncRoot"/> property to ensure it is
        /// synchronized via the same sync root as the <see cref="ConcurrentSet{T}"/> uses internally.
        /// </summary>
        [Test, LuceneNetSpecific]
        public async Task TestSyncRoot()
        {
            var innerSet = new JCG.LinkedHashSet<int>();
            var set = new ConcurrentSet<int>(innerSet);
            Assert.IsNotNull(set.SyncRoot);

            var tasks = new List<Task>();

            for (int i = 0; i < 100; i++)
            {
                var capturedIndex = i; // Capture the index so it doesn't change in the closure
                tasks.Add(Task.Run(() =>
                {
                    // Alternate between using the SyncRoot and letting the ConcurrentSet handle synchronization
                    if (capturedIndex % 2 == 0)
                    {
                        UninterruptableMonitor.Enter(set.SyncRoot);
                        try
                        {
                            for (int j = capturedIndex * 100; j < (capturedIndex + 1) * 100; j++)
                            {
                                innerSet.Add(j);
                            }
                        }
                        finally
                        {
                            UninterruptableMonitor.Exit(set.SyncRoot);
                        }
                    }
                    else
                    {
                        for (int j = capturedIndex * 100; j < (capturedIndex + 1) * 100; j++)
                        {
                            set.Add(j);
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);

            Assert.AreEqual(10000, set.Count);
            Assert.AreEqual(10000, innerSet.Count);
        }

        [Test, LuceneNetSpecific]
        public void TestGetEnumerator()
        {
            var innerSet = new JCG.LinkedHashSet<int>();
            var set = new ConcurrentSet<int>(innerSet);
            for (int i = 0; i < 100; i++)
            {
                set.Add(i);
            }

            using var enumerator = set.GetEnumerator();
            var list = new List<int>();
            while (enumerator.MoveNext())
            {
                list.Add(enumerator.Current);
                // modify set while iterating
                set.Add(100 + enumerator.Current);
            }

            Assert.AreEqual(100, list.Count);
            for (int i = 0; i < 100; i++)
            {
                Assert.IsTrue(list.Contains(i));
            }
        }
    }
}
