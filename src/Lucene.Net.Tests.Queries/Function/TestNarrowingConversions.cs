using Lucene.Net.Attributes;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Tests.Queries.Function
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
    /// Verifies that <see cref="SingleDocValues"/> and <see cref="DoubleDocValues"/> narrow
    /// out-of-range values the way Java does (convert to int, then wrap), rather than
    /// saturating as a direct floating-point to small-integer cast does on .NET 11+.
    /// </summary>
    [LuceneNetSpecific]
    public class TestNarrowingConversions : LuceneTestCase
    {
        [Test]
        public void TestSingleDocValuesByteValWrapsLikeJava()
        {
            assertEquals((byte)5, new ConstSingleDocValues(5f).ByteVal(0));
            assertEquals((byte)44, new ConstSingleDocValues(300f).ByteVal(0)); // Java: (byte) 300f == 44
            assertEquals((byte)255, new ConstSingleDocValues(-1f).ByteVal(0)); // Java: (byte) -1f == -1 (0xFF)
        }

        [Test]
        public void TestSingleDocValuesInt16ValWrapsLikeJava()
        {
            assertEquals((short)5, new ConstSingleDocValues(5f).Int16Val(0));
            assertEquals((short)4464, new ConstSingleDocValues(70000f).Int16Val(0)); // Java: (short) 70000f == 4464
            assertEquals((short)-4464, new ConstSingleDocValues(-70000f).Int16Val(0)); // Java: (short) -70000f == -4464
        }

        [Test]
        public void TestDoubleDocValuesByteValWrapsLikeJava()
        {
            assertEquals((byte)5, new ConstDoubleDocValues(5d).ByteVal(0));
            assertEquals((byte)44, new ConstDoubleDocValues(300d).ByteVal(0)); // Java: (byte) 300d == 44
            assertEquals((byte)255, new ConstDoubleDocValues(-1d).ByteVal(0)); // Java: (byte) -1d == -1 (0xFF)
        }

        [Test]
        public void TestDoubleDocValuesInt16ValWrapsLikeJava()
        {
            assertEquals((short)5, new ConstDoubleDocValues(5d).Int16Val(0));
            assertEquals((short)4464, new ConstDoubleDocValues(70000d).Int16Val(0)); // Java: (short) 70000d == 4464
            assertEquals((short)-4464, new ConstDoubleDocValues(-70000d).Int16Val(0)); // Java: (short) -70000d == -4464
        }

        private sealed class ConstSingleDocValues : SingleDocValues
        {
            private readonly float value;

            public ConstSingleDocValues(float value)
                : base(null)
            {
                this.value = value;
            }

            public override float SingleVal(int doc) => value;
        }

        private sealed class ConstDoubleDocValues : DoubleDocValues
        {
            private readonly double value;

            public ConstDoubleDocValues(double value)
                : base(null)
            {
                this.value = value;
            }

            public override double DoubleVal(int doc) => value;
        }
    }
}
