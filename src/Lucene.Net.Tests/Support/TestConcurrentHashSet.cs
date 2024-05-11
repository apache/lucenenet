using Lucene.Net.Attributes;
using Lucene.Net.Support;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

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

    public class TestConcurrentHashSet
    {
        [Test, LuceneNetSpecific]
        public void TestExceptWith()
        {
            // Numbers 0-8, 10-80, 99
            var initialSet = Enumerable.Range(1, 8)
                .Concat(Enumerable.Range(1, 8).Select(i => i * 10))
                .Append(99)
                .Append(0);

            var hashSet = new ConcurrentHashSet<int>(initialSet);

            Parallel.ForEach(Enumerable.Range(1, 8), i =>
            {
                // Remove i and i * 10, i.e. 1 and 10, 2 and 20, etc.
                var except = new[] { i, i * 10 };
                hashSet.ExceptWith(except);
            });

            Assert.AreEqual(2, hashSet.Count);
            Assert.IsTrue(hashSet.Contains(0));
            Assert.IsTrue(hashSet.Contains(99));
        }
    }
}
