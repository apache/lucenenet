// Some tests adapted from Apache Harmony:
// https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/concurrent/src/test/java/ConcurrentHashMapTest.java
// https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/luni/src/test/api/common/org/apache/harmony/luni/tests/java/util/CollectionsTest.java

using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;

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
    /// Tests for <see cref="ConcurrentHashSet{T}"/>.
    /// </summary>
    /// <remarks>
    /// See the <see cref="BaseConcurrentSetTestCase"/> class for most of the test cases.
    /// This class specializes the tests for the <see cref="ConcurrentHashSet{T}"/> class.
    /// </remarks>
    public class TestConcurrentHashSet : BaseConcurrentSetTestCase
    {
        protected override ISet<T> NewSet<T>()
            => new ConcurrentHashSet<T>();

        protected override ISet<T> NewSet<T>(ISet<T> set)
            => new ConcurrentHashSet<T>(set);

        /// <summary>
        /// Cannot create with negative capacity
        /// </summary>
        [Test]
        public void TestConstructor1() {
            try
            {
                _ = new ConcurrentHashSet<object>(8, -1);
                shouldThrow();
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException
            {
            }
        }

        /// <summary>
        /// Cannot create with negative concurrency level
        /// </summary>
        [Test]
        public void TestConstructor2()
        {
            try
            {
                _ = new ConcurrentHashSet<object>(-1, 100);
                shouldThrow();
            }
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException
            {
            }
        }

        [Test]
        [Ignore("ConcurrentHashSet does not currently implement structural Equals")]
        public override void TestEquals() => base.TestEquals();
    }
}
