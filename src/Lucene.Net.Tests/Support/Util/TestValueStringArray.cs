using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

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

    [TestFixture]
    [LuceneNetSpecific]
    public class TestValueStringArray
    {
        [Test]
        public void TestNullArray()
        {
            ValueStringArray values = new((string[])null);

            Assert.IsTrue(values.IsNull);
            Assert.AreEqual(0, values.Length);
        }

        [Test]
        public void TestEmptyArray()
        {
            ValueStringArray values = new(Array.Empty<string>());

            Assert.IsFalse(values.IsNull);
            Assert.AreEqual(0, values.Length);
        }

        [Test]
        public void TestEmptySpan()
        {
            ValueStringArray values = new(ReadOnlySpan<char>.Empty);

            Assert.IsFalse(values.IsNull);
            Assert.AreEqual(1, values.Length);
            Assert.AreEqual(0, values[0].Length);
        }

        [Test]
        public void TestSingleSpan()
        {
            ValueStringArray values = new("hello".AsSpan());

            Assert.IsFalse(values.IsNull);
            Assert.AreEqual(1, values.Length);
            Assert.IsTrue(values[0].SequenceEqual("hello"));
        }

        [Test]
        public void TestStringArray()
        {
            string[] input = { "one", "two", "three" };

            ValueStringArray values = new(input);

            Assert.IsFalse(values.IsNull);
            Assert.AreEqual(3, values.Length);

            Assert.IsTrue(values[0].SequenceEqual("one"));
            Assert.IsTrue(values[1].SequenceEqual("two"));
            Assert.IsTrue(values[2].SequenceEqual("three"));
        }

        [Test]
        public void TestReadOnlySpanStringArray()
        {
            string[] input = { "one", "two" };

            ValueStringArray values = new(input.AsSpan());

            Assert.IsFalse(values.IsNull);
            Assert.AreEqual(2, values.Length);
            Assert.IsTrue(values[0].SequenceEqual("one"));
            Assert.IsTrue(values[1].SequenceEqual("two"));
        }

        [Test]
        public void TestMultipleSpans()
        {
            ValueStringArray values = new ValueStringArray(
                "one".AsSpan(),
                "two".AsSpan(),
                "three".AsSpan());

            Assert.IsFalse(values.IsNull);
            Assert.AreEqual(3, values.Length);

            Assert.IsTrue(values[0].SequenceEqual("one"));
            Assert.IsTrue(values[1].SequenceEqual("two"));
            Assert.IsTrue(values[2].SequenceEqual("three"));
        }
    }
}

