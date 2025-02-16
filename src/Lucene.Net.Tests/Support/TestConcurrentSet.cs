// Some tests adapted from Apache Harmony:
// https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/concurrent/src/test/java/ConcurrentHashMapTest.java
// https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/luni/src/test/api/common/org/apache/harmony/luni/tests/java/util/CollectionsTest.java

using Lucene.Net.Support;
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
    /// Tests for <see cref="ConcurrentSet{T}"/>.
    /// </summary>
    /// <remarks>
    /// See the <see cref="BaseConcurrentSetTestCase"/> class for most of the test cases.
    /// This class specializes the tests for the <see cref="ConcurrentSet{T}"/> class.
    /// </remarks>
    public class TestConcurrentSet : BaseConcurrentSetTestCase
    {
        protected override ISet<T> NewSet<T>()
            => new ConcurrentSet<T>(new HashSet<T>());

        protected override ISet<T> NewSet<T>(IEnumerable<T> collection)
            => new ConcurrentSet<T>(new HashSet<T>(collection));
    }
}
