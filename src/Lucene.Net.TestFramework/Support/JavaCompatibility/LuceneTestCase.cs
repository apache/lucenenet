using System;
using System.Collections;
using System.Collections.Generic;
using Lucene.Net.Support;
using JCG = J2N.Collections.Generic;
using Debug = Lucene.Net.Diagnostics.Debug; // LUCENENET NOTE: We cannot use System.Diagnostics.Debug because those calls will be optimized out of the release!
using Assert = Lucene.Net.TestFramework.Assert;

#if TESTFRAMEWORK_MSTEST
using CollectionAssert = Lucene.Net.TestFramework.Assert;
#elif TESTFRAMEWORK_NUNIT
using CollectionAssert = NUnit.Framework.CollectionAssert;
#elif TESTFRAMEWORK_XUNIT
using CollectionAssert = Lucene.Net.TestFramework.Assert;
#endif

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

    /// <summary>
    /// LUCENENET specific extensions to <see cref="LuceneTestCase"/> to make it easier to port tests
    /// from Java with fewer changes.
    /// </summary>
    public abstract partial class LuceneTestCase
    {
        // LUCENENET NOTE: This was not added because it causes naming collisions with
        // member variables "private readonly Random random;". Capitlizing it would collide
        // with the Random property. Better (and safer) just to convert all of these 
        // from "random()" to "Random" going forward.
        //internal static Random random()
        //{
        //    return Random;
        //}

        internal static void assertTrue(bool condition)
        {
            Assert.IsTrue(condition);
        }

        internal static void assertTrue(string message, bool condition)
        {
            Assert.IsTrue(condition, message);
        }

        internal static void assertFalse(bool condition)
        {
            Assert.IsFalse(condition);
        }

        internal static void assertFalse(string message, bool condition)
        {
            Assert.IsFalse(condition, message);
        }

        internal static void assertEquals<T>(T expected, T actual)
        {
            Assert.AreEqual(expected, actual);
        }

        internal static void assertEquals<T>(string message, T expected, T actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        internal static void assertEquals(string expected, string actual)
        {
            Assert.AreEqual(expected, actual);
        }

        internal static void assertEquals(string message, string expected, string actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        internal static void assertEquals(bool expected, bool actual)
        {
            Assert.AreEqual(expected, actual);
        }

        internal static void assertEquals(string message, bool expected, bool actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        internal static void assertEquals(long expected, long actual)
        {
            Assert.AreEqual(expected, actual);
        }

        internal static void assertEquals(string message, long expected, long actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        internal static void assertEquals(int expected, int actual)
        {
            Assert.AreEqual(expected, actual);
        }

        internal static void assertEquals(string message, int expected, int actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        internal static void assertEquals(byte expected, byte actual)
        {
            Assert.AreEqual(expected, actual);
        }

        internal static void assertEquals(string message, byte expected, byte actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        internal static void assertEquals(double d1, double d2, double delta)
        {
            Assert.AreEqual(d1, d2, delta);
        }

        internal static void assertEquals(string msg, float d1, float d2, float delta)
        {
            Assert.AreEqual(d1, d2, delta, msg);
        }

        internal static void assertEquals(float d1, float d2, float delta)
        {
            Assert.AreEqual(d1, d2, delta);
        }

        internal static void assertEquals(string msg, double d1, double d2, double delta)
        {
            Assert.AreEqual(d1, d2, delta, msg);
        }

        internal static void assertEquals<T>(ISet<T> expected, ISet<T> actual, bool aggressive = true)
        {
            Assert.AreEqual(expected, actual, aggressive);
        }

        internal static void assertEquals<T>(string message, ISet<T> expected, ISet<T> actual, bool aggressive = true)
        {
            Assert.AreEqual(expected, actual, aggressive, message);
        }

        internal static void assertEquals<T>(IList<T> expected, IList<T> actual, bool aggressive = true)
        {
            Assert.AreEqual(expected, actual, aggressive);
        }

        internal static void assertEquals<T>(string message, IList<T> expected, IList<T> actual, bool aggressive = true)
        {
            Assert.AreEqual(expected, actual, aggressive, message);
        }

        internal static void assertEquals<T>(T[] expected, T[] actual)
        {
            Assert.AreEqual(expected, actual);
        }

        internal static void assertEquals<T>(string message, T[] expected, T[] actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        internal static void assertEquals<TKey, TValue>(IDictionary<TKey, TValue> expected, IDictionary<TKey, TValue> actual, bool aggressive = true)
        {
            Assert.AreEqual(expected, actual, aggressive);
        }

        internal static void assertEquals<TKey, TValue>(string message, IDictionary<TKey, TValue> expected, IDictionary<TKey, TValue> actual, bool aggressive = true)
        {
            Assert.AreEqual(expected, actual, aggressive, message);
        }

        internal static void assertNotSame(object unexpected, object actual)
        {
            Assert.AreNotSame(unexpected, actual);
        }

        internal static void assertNotSame(string message, object unexpected, object actual)
        {
            Assert.AreNotSame(unexpected, actual, message);
        }

        internal static void assertNotNull(object o)
        {
            Assert.NotNull(o);
        }

        internal static void assertNotNull(string msg, object o)
        {
            Assert.NotNull(o, msg);
        }

        internal static void assertNull(object o)
        {
            Assert.Null(o);
        }

        internal static void assertNull(string msg, object o)
        {
            Assert.Null(o, msg);
        }

        internal static void assertArrayEquals<T>(T[] a1, T[] a2)
        {
            Assert.AreEqual(a1, a2);
        }

        internal static void assertSame(object expected, object actual)
        {
            Assert.AreSame(expected, actual);
        }

        internal static void assertSame(string message, object expected, object actual)
        {
            Assert.AreSame(expected, actual, message);
        }

        internal static void fail()
        {
            Assert.Fail();
        }

        internal static void fail(string message)
        {
            Assert.Fail(message);
        }


        internal static ISet<T> AsSet<T>(params T[] args)
        {
            return new JCG.HashSet<T>(args);
        }

        [ExceptionToNetNumericConvention] // LUCENENET: This is for making test porting easier, keeping as-is
        internal int randomInt(int max)
        {
            return randomIntBetween(0, max);
        }

        [ExceptionToNetNumericConvention] // LUCENENET: This is for making test porting easier, keeping as-is
        internal int randomIntBetween(int min, int max)
        {
            Debug.Assert(max >= min, "max must be >= min: " + min + ", " + max);
            long range = (long)max - (long)min;
            if (range < int.MaxValue)
            {
                return min + Random.nextInt(1 + (int)range);
            }
            else
            {
                return toIntExact(min + Random.Next(1 + (int)range));
            }
        }

        [ExceptionToNetNumericConvention] // LUCENENET: This is for making test porting easier, keeping as-is
        private static int toIntExact(long value)
        {
            if (value > int.MaxValue)
            {
                throw new ArithmeticException("Overflow: " + value);
            }
            else
            {
                return (int)value;
            }
        }
        
        internal double randomGaussian()
        {
            return RandomGaussian();
        }
    }
}
