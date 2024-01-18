﻿using Lucene.Net.Attributes;
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

    [TestFixture]
    public class TestIntsRef : LuceneTestCase
    {
        [Test]
        public virtual void TestEmpty()
        {
            Int32sRef i = new Int32sRef();
            Assert.AreEqual(Int32sRef.EMPTY_INT32S, i.Int32s);
            Assert.AreEqual(0, i.Offset);
            Assert.AreEqual(0, i.Length);
        }

        [Test]
        public virtual void TestFromInts()
        {
            int[] ints = new int[] { 1, 2, 3, 4 };
            Int32sRef i = new Int32sRef(ints, 0, 4);
            Assert.AreEqual(ints, i.Int32s);
            Assert.AreEqual(0, i.Offset);
            Assert.AreEqual(4, i.Length);

            Int32sRef i2 = new Int32sRef(ints, 1, 3);
            Assert.AreEqual(new Int32sRef(new int[] { 2, 3, 4 }, 0, 3), i2);

            Assert.IsFalse(i.Equals(i2));
        }

#if FEATURE_SERIALIZABLE

        [Test, LuceneNetSpecific]
        public void TestSerialization()
        {
            var ints = new int[] { 5, 10, 15, 20, 25, 30, 35, 40 };

            var intsRef = new Int32sRef(ints, 3, 4);

            Assert.AreEqual(4, intsRef.Length);
            Assert.AreSame(ints, intsRef.Int32s);
            Assert.AreEqual(ints, intsRef.Int32s);
            Assert.AreEqual(3, intsRef.Offset);

            var clone = Clone(intsRef);

            Assert.AreEqual(4, clone.Length);
            Assert.AreNotSame(ints, clone.Int32s);
            Assert.AreEqual(ints, clone.Int32s);
            Assert.AreEqual(3, clone.Offset);
        }
#endif
    }
}
