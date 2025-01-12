using Lucene.Net.Attributes;
using Lucene.Net.Index;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;

#nullable enable

namespace Lucene.Net.Documents.Extensions
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
    public class TestIndexableFieldExtensions : LuceneTestCase
    {
        public static IEnumerable<TestCaseData> TestCases()
        {
#pragma warning disable CS8974 // Converting method group to non-delegate type
            yield return new TestCaseData(new Int32Field("field", byte.MaxValue, Field.Store.NO), byte.MaxValue, IndexableFieldExtensions.GetByteValueOrDefault);
            yield return new TestCaseData(new Int32Field("field", short.MaxValue, Field.Store.NO), short.MaxValue, IndexableFieldExtensions.GetInt16ValueOrDefault);
            yield return new TestCaseData(new Int32Field("field", int.MaxValue, Field.Store.NO), int.MaxValue, IndexableFieldExtensions.GetInt32ValueOrDefault);
            yield return new TestCaseData(new Int64Field("field", long.MaxValue, Field.Store.NO), long.MaxValue, IndexableFieldExtensions.GetInt64ValueOrDefault);
            yield return new TestCaseData(new SingleField("field", float.MaxValue, Field.Store.NO), float.MaxValue, IndexableFieldExtensions.GetSingleValueOrDefault);
            yield return new TestCaseData(new DoubleField("field", double.MaxValue, Field.Store.NO), double.MaxValue, IndexableFieldExtensions.GetDoubleValueOrDefault);
            yield return new TestCaseData(null, (byte)0, IndexableFieldExtensions.GetByteValueOrDefault);
            yield return new TestCaseData(null, (short)0, IndexableFieldExtensions.GetInt16ValueOrDefault);
            yield return new TestCaseData(null, 0, IndexableFieldExtensions.GetInt32ValueOrDefault);
            yield return new TestCaseData(null, 0L, IndexableFieldExtensions.GetInt64ValueOrDefault);
            yield return new TestCaseData(null, 0f, IndexableFieldExtensions.GetSingleValueOrDefault);
            yield return new TestCaseData(null, 0d, IndexableFieldExtensions.GetDoubleValueOrDefault);
            yield return new TestCaseData(new StringField("field", "value", Field.Store.NO), (byte)0, IndexableFieldExtensions.GetByteValueOrDefault);
            yield return new TestCaseData(new StringField("field", "value", Field.Store.NO), (short)0, IndexableFieldExtensions.GetInt16ValueOrDefault);
            yield return new TestCaseData(new StringField("field", "value", Field.Store.NO), 0, IndexableFieldExtensions.GetInt32ValueOrDefault);
            yield return new TestCaseData(new StringField("field", "value", Field.Store.NO), 0L, IndexableFieldExtensions.GetInt64ValueOrDefault);
            yield return new TestCaseData(new StringField("field", "value", Field.Store.NO), 0f, IndexableFieldExtensions.GetSingleValueOrDefault);
            yield return new TestCaseData(new StringField("field", "value", Field.Store.NO), 0d, IndexableFieldExtensions.GetDoubleValueOrDefault);
#pragma warning restore CS8974 // Converting method group to non-delegate type
        }

        [Test, LuceneNetSpecific]
        [TestCaseSource(nameof(TestCases))]
        public void TestIndexableFieldExtensions_TestCases(IIndexableField? field, object expected, Delegate func)
        {
            Assert.AreEqual(expected, func.DynamicInvoke(field));
        }
    }
}
