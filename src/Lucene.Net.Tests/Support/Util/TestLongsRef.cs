using Lucene.Net.Attributes;
using NUnit.Framework;
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

    // LUCENENET: These tests didn't exist in Lucene 4.8.0. Just copying the TestIntsRef tests so we can test the same things.
    [TestFixture]
    public class TestLongsRef : LuceneTestCase
    {
        [Test, LuceneNetSpecific]
        public virtual void TestEmpty()
        {
            Int64sRef i = new Int64sRef();
            Assert.AreEqual(Int64sRef.EMPTY_INT64S, i.Int64s);
            Assert.AreEqual(0, i.Offset);
            Assert.AreEqual(0, i.Length);
        }

        [Test, LuceneNetSpecific]
        public virtual void TestFromLongs()
        {
            long[] ints = new long[] { 1, 2, 3, 4 };
            Int64sRef i = new Int64sRef(ints, 0, 4);
            Assert.AreEqual(ints, i.Int64s);
            Assert.AreEqual(0, i.Offset);
            Assert.AreEqual(4, i.Length);

            Int64sRef i2 = new Int64sRef(ints, 1, 3);
            Assert.AreEqual(new Int64sRef(new long[] { 2, 3, 4 }, 0, 3), i2);

            Assert.IsFalse(i.Equals(i2));
        }

#if FEATURE_SERIALIZABLE

        [Test, LuceneNetSpecific]
        public void TestSerialization()
        {
            var longs = new long[] { 5, 10, 15, 20, 25, 30, 35, 40 };

            var longsRef = new Int64sRef(longs, 3, 4);

            Assert.AreEqual(4, longsRef.Length);
            Assert.AreSame(longs, longsRef.Int64s);
            Assert.AreEqual(longs, longsRef.Int64s);
            Assert.AreEqual(3, longsRef.Offset);

            var clone = Clone(longsRef);

            Assert.AreEqual(4, clone.Length);
            Assert.AreNotSame(longs, clone.Int64s);
            Assert.AreEqual(longs, clone.Int64s);
            Assert.AreEqual(3, clone.Offset);
        }
#endif
    }
}