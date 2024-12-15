using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Threading;

namespace Lucene.Net.Threading
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
    /// This is a modified copy of J2N's TestAtomicInt64,
    /// modified to test <see cref="AtomicDouble"/>
    /// </summary>
    [TestFixture]
    public class AtomicDoubleTest : LuceneTestCase
    {
        private const int LONG_DELAY_MS = 50 * 50;

        /**
         * fail with message "Unexpected exception"
         */
        public void unexpectedException()
        {
            fail("Unexpected exception");
        }

        /**
         * constructor initializes to given value
         */
        [Test]
        public void TestConstructor()
        {
            AtomicDouble ai = new AtomicDouble(1.0d);
            assertEquals(1.0d, ai);
        }

        /**
         * default constructed initializes to zero
         */
        [Test]
        public void TestConstructor2()
        {
            AtomicDouble ai = new AtomicDouble();
            assertEquals(0.0d, ai.Value);
        }

        /**
         * get returns the last value set
         */
        [Test]
        public void TestGetSet()
        {
            AtomicDouble ai = new AtomicDouble(1);
            assertEquals(1.0d, ai);
            ai.Value = 2.0d;
            assertEquals(2.0d, ai);
            ai.Value = -3.0d;
            assertEquals(-3.0d, ai);

        }

        /**
         * compareAndSet succeeds in changing value if equal to expected else fails
         */
        [Test]
        public void TestCompareAndSet()
        {
            AtomicDouble ai = new AtomicDouble(1.0d);
            assertTrue(ai.CompareAndSet(1.0d, 2.0d));
            assertTrue(ai.CompareAndSet(2.0d, -4.0d));
            assertEquals(-4.0d, ai.Value);
            assertFalse(ai.CompareAndSet(-5.0d, 7.0d));
            assertFalse(7.0d.Equals(ai.Value));
            assertTrue(ai.CompareAndSet(-4.0d, 7.0d));
            assertEquals(7.0d, ai.Value);
        }

        /**
         * compareAndSet in one thread enables another waiting for value
         * to succeed
         */
        [Test]
        public void TestCompareAndSetInMultipleThreads()
        {
            AtomicDouble ai = new AtomicDouble(1.0d);
            Thread t = new Thread(() =>
            {
                while (!ai.CompareAndSet(2.0d, 3.0d)) Thread.Yield();
            });
            try
            {
                t.Start();
                assertTrue(ai.CompareAndSet(1.0d, 2.0d));
                t.Join(LONG_DELAY_MS);
                assertFalse(t.IsAlive);
                assertEquals(ai.Value, 3.0d);
            }
            catch (Exception /*e*/)
            {
                unexpectedException();
            }
        }

        //    /**
        //     * repeated weakCompareAndSet succeeds in changing value when equal
        //     * to expected
        //     */
        //[Test]
        //    public void TestWeakCompareAndSet()
        //{
        //    AtomicDouble ai = new AtomicDouble(1);
        //    while (!ai.WeakCompareAndSet(1, 2)) ;
        //    while (!ai.WeakCompareAndSet(2, -4)) ;
        //    assertEquals(-4, ai.Value);
        //    while (!ai.WeakCompareAndSet(-4, 7)) ;
        //    assertEquals(7, ai.Value);
        //}

        /**
         * getAndSet returns previous value and sets to given value
         */
        [Test]
        public void TestGetAndSet()
        {
            AtomicDouble ai = new AtomicDouble(1.0d);
            assertEquals(1.0d, ai.GetAndSet(0.0d));
            assertEquals(0.0d, ai.GetAndSet(-10.0d));
            assertEquals(-10.0d, ai.GetAndSet(1.0d));
        }

#if FEATURE_SERIALIZABLE
        /**
         * a deserialized serialized atomic holds same value
         */
        [Test]
        public void TestSerialization()
        {
            AtomicDouble l = new AtomicDouble();

            try
            {
                l.Value = 22.0d;
                AtomicDouble r = Clone(l);
                assertEquals(l.Value, r.Value);
            }
            catch (Exception /*e*/)
            {
                unexpectedException();
            }
        }
#endif

        /**
         * toString returns current value.
         */
        [Test]
        public void TestToString()
        {
            AtomicDouble ai = new AtomicDouble();
            for (double i = -12.0d; i < 6.0d; i += 1.0d)
            {
                ai.Value = i;
                assertEquals(ai.ToString(), J2N.Numerics.Double.ToString(i));
            }
        }

        /**
         * intValue returns current value.
         */
        [Test]
        public void TestIntValue()
        {
            AtomicDouble ai = new AtomicDouble();
            for (double i = -12.0d; i < 6.0d; ++i)
            {
                ai.Value = i;
                assertEquals((int)i, Convert.ToInt32(ai));
            }
        }


        /**
         * longValue returns current value.
         */
        [Test]
        public void TestLongValue()
        {
            AtomicDouble ai = new AtomicDouble();
            for (double i = -12.0d; i < 6.0d; ++i)
            {
                ai.Value = i;
                assertEquals((long)i, Convert.ToInt64(ai));
            }
        }

        /**
         * floatValue returns current value.
         */
        [Test]
        public void TestFloatValue()
        {
            AtomicDouble ai = new AtomicDouble();
            for (double i = -12.0d; i < 6.0d; ++i)
            {
                ai.Value = i;
                assertEquals((float)i, Convert.ToSingle(ai));
            }
        }

        /**
         * doubleValue returns current value.
         */
        [Test]
        public void TestDoubleValue()
        {
            AtomicDouble ai = new AtomicDouble();
            for (double i = -12.0d; i < 6.0d; ++i)
            {
                ai.Value = i;
                assertEquals((double)i, Convert.ToDouble(ai));
            }
        }

        /**
        * doubleValue returns current value.
        */
        [Test]
        public void TestComparisonOperators()
        {
            AtomicDouble ai = new AtomicDouble(6.0d);
            assertTrue(5.0d < ai);
            assertTrue(9.0d > ai);
            assertTrue(ai > 4.0d);
            assertTrue(ai < 7.0d);
            assertFalse(ai < 6.0d);
            assertTrue(ai <= 6.0d);
        }
    }
}
