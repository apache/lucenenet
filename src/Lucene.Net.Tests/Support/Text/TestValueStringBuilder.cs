using J2N.Globalization;
using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Text
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
    /// Smoke tests for <see cref="ValueStringBuilder"/> to ensure it has been generated with
    /// the configured values. More thorough tests are performed in SpanTools
    /// <a href="https://www.nuget.org/packages/SpanTools.ValueStringBuilder.Generator">
    /// https://www.nuget.org/packages/SpanTools.ValueStringBuilder.Generator</a>
    /// </summary>
    [TestFixture, LuceneNetSpecific]
    public class TestValueStringBuilder : LuceneTestCase
    {
        [Test]
        public void TestJavaStyleLastIndexOf()
        {
            using ValueStringBuilder sb = new(stackalloc char[8]);
            sb.Append("aaaaa");
            Assert.AreEqual(2, sb.LastIndexOf("aa", 2));
        }

        [Test]
        public void TestJavaStyleFormatting()
        {
            using CultureContext context = new("pt");
            using ValueStringBuilder sb = new(stackalloc char[8]);
            sb.Append(123d);
            Assert.AreEqual("123.0", sb.ToString());
        }

        // Tests MaxLength tracking
        [Test]
        public void TestFitsIntialBuffer_BeyondInitialBuffer_ReturnsMaxLength()
        {
            using ValueStringBuilder sb = new(stackalloc char[8]);
            sb.Append("Hello");
            sb.Append("World");
            Assert.IsFalse(sb.FitsInitialBuffer(out int charsLength));
            Assert.AreEqual(10, charsLength);
        }

        [Test]
        public void TestFitsIntialBuffer_WithinInitialBuffer_ReturnsLength()
        {
            using ValueStringBuilder sb = new(stackalloc char[8]);
            sb.Append("Hello");
            sb.Append("W");
            Assert.IsTrue(sb.FitsInitialBuffer(out int charsLength));
            Assert.AreEqual(6, charsLength);
            Assert.AreEqual(6, sb.Length);
        }

        [Test]
        public void TestAsMemory()
        {
            using ValueStringBuilder sb = new(8);
            sb.Append("Hello");
            ReadOnlyMemory<char> memory = sb.AsMemory();
            sb.Append("World");
            Assert.AreNotEqual("HelloWorld", memory.Span.ToString()); // This is guaranteed not to be the same memory as otherMemory
            ReadOnlyMemory<char> otherMemory = sb.AsMemory();
            Assert.AreEqual("HelloWorld", otherMemory.Span.ToString());
        }

        [Test]
        public void TestPreload()
        {
            Span<char> destination = stackalloc char[10];
            "Hello".AsSpan().CopyTo(destination);
            ValueStringBuilder sb = new(destination);
            try
            {
                sb.Length = 5; // "Consume" the preloaded value
                sb.Append("World");
                Assert.AreEqual("HelloWorld", sb.ToString());
                Assert.IsTrue(sb.FitsInitialBuffer(out int charsLength));
                Assert.AreEqual(10, charsLength);
                Assert.AreEqual(10, sb.Length);
            }
            finally
            {
                sb.Dispose(); // Technically not required; best practice to call this
            }
        }
    }
}
