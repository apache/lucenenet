using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;

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
    public class TestCastTo : LuceneTestCase
    {
        public static IEnumerable<TestCaseData> TestFromSuccessCases()
        {
            yield return new TestCaseData((byte)1, (short)1);
            yield return new TestCaseData((byte)1, 1);
            yield return new TestCaseData((byte)1, 1L);
            yield return new TestCaseData((byte)1, 1f);
            yield return new TestCaseData((byte)1, 1d);
            yield return new TestCaseData((short)2, (byte)2);
            yield return new TestCaseData((short)2, 2);
            yield return new TestCaseData((short)2, 2L);
            yield return new TestCaseData((short)2, 2f);
            yield return new TestCaseData((short)2, 2d);
            yield return new TestCaseData(3, (byte)3);
            yield return new TestCaseData(3, (short)3);
            yield return new TestCaseData(3, 3L);
            yield return new TestCaseData(3, 3f);
            yield return new TestCaseData(3, 3d);
            yield return new TestCaseData(4L, (byte)4);
            yield return new TestCaseData(4L, (short)4);
            yield return new TestCaseData(4L, 4);
            yield return new TestCaseData(4L, 4f);
            yield return new TestCaseData(4L, 4d);
            yield return new TestCaseData(5f, (byte)5);
            yield return new TestCaseData(5f, (short)5);
            yield return new TestCaseData(5f, 5);
            yield return new TestCaseData(5f, 5L);
            yield return new TestCaseData(5f, 5d);
            yield return new TestCaseData(6d, (byte)6);
            yield return new TestCaseData(6d, (short)6);
            yield return new TestCaseData(6d, 6);
            yield return new TestCaseData(6d, 6L);
            yield return new TestCaseData(6d, 6f);
        }

        [Test, LuceneNetSpecific]
        [TestCaseSource(nameof(TestFromSuccessCases))]
        public void TestFrom_Success(object value, object expected)
        {
            var castTo = typeof(CastTo<>).MakeGenericType(expected.GetType());
            var from = castTo.GetMethod("From")?.MakeGenericMethod(value.GetType())
                ?? throw new InvalidOperationException("Could not find method CastTo<T>.From<TSource>");
            Assert.AreEqual(expected, from.Invoke(null, new[] { value }));
        }

        public static IEnumerable<TestCaseData> TestFromInvalidCastCases()
        {
            yield return new TestCaseData(1, "1");
            yield return new TestCaseData(new object(), 1);
        }

        [Test, LuceneNetSpecific]
        [TestCaseSource(nameof(TestFromInvalidCastCases))]
        public void TestFrom_InvalidCast(object value, object expected)
        {
            var castTo = typeof(CastTo<>).MakeGenericType(expected.GetType());
            var from = castTo.GetMethod("From")?.MakeGenericMethod(value.GetType())
                ?? throw new InvalidOperationException("Could not find method CastTo<T>.From<TSource>");
            try
            {
                from.Invoke(null, new[] { value });
                Assert.Fail("Expected an exception");
            }
            catch
            {
                // ignored
            }
        }
    }
}
