/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Lucene.Net.Support;
using Debug = Lucene.Net.Diagnostics.Debug; // LUCENENET NOTE: We cannot use System.Diagnostics.Debug because those calls will be optimized out of the release!

namespace Lucene.Net.Util
{
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

        internal static void assertEquals(object expected, object actual)
        {
            Assert.AreEqual(expected, actual);
        }

        internal static void assertEquals(string message, object expected, object actual)
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

        internal static void assertEquals<T>(ISet<T> expected, ISet<T> actual)
        {
            Assert.True(expected.SetEquals(actual));
        }

        internal static void assertEquals<T>(string message, ISet<T> expected, ISet<T> actual)
        {
            Assert.True(expected.SetEquals(actual), message);
        }

        internal static void assertEquals<T, S>(IDictionary<T, S> expected, IDictionary<T, S> actual)
        {
            Assert.AreEqual(expected.Count, actual.Count);
            foreach (var key in expected.Keys)
            {
                Assert.AreEqual(expected[key], actual[key]);
            }
        }

        internal static void assertNotSame(object unexpected, object actual)
        {
            Assert.AreNotSame(unexpected, actual);
        }

        internal static void assertNotSame(string message, object unexpected, object actual)
        {
            Assert.AreNotSame(unexpected, actual, message);
        }

        internal static void assertEquals(double d1, double d2, double delta)
        {
            Assert.AreEqual(d1, d2, delta);
        }

        internal static void assertEquals(string msg, double d1, double d2, double delta)
        {
            Assert.AreEqual(d1, d2, delta, msg);
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

        internal static void assertArrayEquals(IEnumerable a1, IEnumerable a2)
        {
            CollectionAssert.AreEqual(a1, a2);
        }

        internal static void assertSame(Object expected, Object actual)
        {
            Assert.AreSame(expected, actual);
        }

        internal static void assertSame(string message, Object expected, Object actual)
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
            return new HashSet<T>(args);
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
