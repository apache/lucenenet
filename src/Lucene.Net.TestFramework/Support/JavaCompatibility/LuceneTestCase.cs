using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

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


    // LUCENENET specific extensions to <see cref="LuceneTestCase"/> to make it easier to port tests
    // from Java with fewer changes.
    // LUCENENET NOTE: Don't add xml doc comments here, they must be applied only to the main LuceneTestCase class
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "These methods are for making porting tests from Java simpler")]
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

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertTrue(bool condition)
        {
            Assert.IsTrue(condition);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertTrue(string message, bool condition)
        {
            Assert.IsTrue(condition, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertFalse(bool condition)
        {
            Assert.IsFalse(condition);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertFalse(string message, bool condition)
        {
            Assert.IsFalse(condition, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals<T>(T expected, T actual)
        {
            Assert.AreEqual(expected, actual);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals<T>(string message, T expected, T actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals(string expected, string actual)
        {
            Assert.AreEqual(expected, actual);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals(string message, string expected, string actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals(bool expected, bool actual)
        {
            Assert.AreEqual(expected, actual);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals(string message, bool expected, bool actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals(long expected, long actual)
        {
            Assert.AreEqual(expected, actual);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals(string message, long expected, long actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals(int expected, int actual)
        {
            Assert.AreEqual(expected, actual);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals(string message, int expected, int actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals(byte expected, byte actual)
        {
            Assert.AreEqual(expected, actual);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals(string message, byte expected, byte actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals(double d1, double d2, double delta)
        {
            Assert.AreEqual(d1, d2, delta);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals(string msg, float d1, float d2, float delta)
        {
            Assert.AreEqual(d1, d2, delta, msg);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals(float d1, float d2, float delta)
        {
            Assert.AreEqual(d1, d2, delta);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals(string msg, double d1, double d2, double delta)
        {
            Assert.AreEqual(d1, d2, delta, msg);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals<T>(ISet<T> expected, ISet<T> actual, bool aggressive = true)
        {
            Assert.AreEqual(expected, actual, aggressive);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals<T>(string message, ISet<T> expected, ISet<T> actual, bool aggressive = true)
        {
            Assert.AreEqual(expected, actual, aggressive, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals<T>(IList<T> expected, IList<T> actual, bool aggressive = true)
        {
            Assert.AreEqual(expected, actual, aggressive);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals<T>(string message, IList<T> expected, IList<T> actual, bool aggressive = true)
        {
            Assert.AreEqual(expected, actual, aggressive, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals<T>(T[] expected, T[] actual)
        {
            Assert.AreEqual(expected, actual);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals<T>(string message, T[] expected, T[] actual)
        {
            Assert.AreEqual(expected, actual, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals<TKey, TValue>(IDictionary<TKey, TValue> expected, IDictionary<TKey, TValue> actual, bool aggressive = true)
        {
            Assert.AreEqual(expected, actual, aggressive);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertEquals<TKey, TValue>(string message, IDictionary<TKey, TValue> expected, IDictionary<TKey, TValue> actual, bool aggressive = true)
        {
            Assert.AreEqual(expected, actual, aggressive, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertNotSame(object unexpected, object actual)
        {
            Assert.AreNotSame(unexpected, actual);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertNotSame(string message, object unexpected, object actual)
        {
            Assert.AreNotSame(unexpected, actual, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertNotNull(object o)
        {
            Assert.NotNull(o);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertNotNull(string msg, object o)
        {
            Assert.NotNull(o, msg);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertNull(object o)
        {
            Assert.Null(o);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertNull(string msg, object o)
        {
            Assert.Null(o, msg);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertArrayEquals<T>(T[] a1, T[] a2)
        {
            Assert.AreEqual(a1, a2);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertArrayEquals<T>(string message, T[] a1, T[] a2)
        {
            Assert.AreEqual(a1, a2, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertArrayEquals<T>(Func<string> getMessage, T[] a1, T[] a2)
        {
            Assert.AreEqual(a1, a2, getMessage());
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertSame(object expected, object actual)
        {
            Assert.AreSame(expected, actual);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void assertSame(string message, object expected, object actual)
        {
            Assert.AreSame(expected, actual, message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void fail()
        {
            Assert.Fail();
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void fail(string message)
        {
            Assert.Fail(message);
        }


        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ISet<T> AsSet<T>(params T[] args)
        {
            return new JCG.HashSet<T>(args);
        }

        [ExceptionToNetNumericConvention] // LUCENENET: This is for making test porting easier, keeping as-is
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int randomInt(int max)
        {
            return randomIntBetween(0, max);
        }

        [ExceptionToNetNumericConvention] // LUCENENET: This is for making test porting easier, keeping as-is
        [DebuggerStepThrough]
        internal static int randomIntBetween(int min, int max)
        {
            // LUCENENET specific - added guard clause instead of assert
            if (max < min)
                throw new ArgumentOutOfRangeException(nameof(max), $"max must be >= min: {min}, {max}");
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
        [DebuggerStepThrough]
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

        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [DebuggerStepThrough]
        internal double randomGaussian()
        {
            return RandomGaussian();
        }
    }
}
