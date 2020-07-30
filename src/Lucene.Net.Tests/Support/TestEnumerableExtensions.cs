using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Support
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

    public class TestEnumerableExtensions : LuceneTestCase
    {
        [Test, LuceneNetSpecific]
        public void TestTakeAllButLast()
        {
            // Reference type
            var references = new List<string>
            {
                "foo",
                "bar",
                "baz",
                "bot"
            };
            // Value type
            var values = new List<int>
            {
                1,2,3,4
            };

            Assert.IsTrue((new string[] { "foo", "bar", "baz" }).SequenceEqual(references.TakeAllButLast()));
            Assert.IsTrue((new int[] { 1, 2, 3 }).SequenceEqual(values.TakeAllButLast()));

            Assert.IsTrue((new string[] { "foo", "bar" }).SequenceEqual(references.TakeAllButLast(2)));
            Assert.IsTrue((new int[] { 1, 2 }).SequenceEqual(values.TakeAllButLast(2)));
        }
    }
}
