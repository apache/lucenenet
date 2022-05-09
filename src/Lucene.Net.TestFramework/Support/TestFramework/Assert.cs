using J2N.Text;
using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using _NUnit = NUnit.Framework;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.TestFramework
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
    /// Facade for NUnit Assertions
    /// </summary>
    internal partial class Assert
    {
        private const string FailureFormat = "Expected: {0}, Actual: {1}";

        //
        // Summary:
        //     We don't actually want any instances of this object, but some people like to
        //     inherit from it to add other static methods. Hence, the protected constructor
        //     disallows any instances of this object.
        protected Assert()
        { }

        //
        // Summary:
        //     Verifies that two objects are equal. Two objects are considered equal if both
        //     are null, or if both have the same value. NUnit has special semantics for some
        //     object types. If they are not equal an NUnit.Framework.AssertionException is
        //     thrown.
        //
        // Parameters:
        //   expected:
        //     The value that is expected
        //
        //   actual:
        //     The actual value
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<T>(T expected, T actual)
        {
            if (!JCG.EqualityComparer<T>.Default.Equals(expected, actual))
                Fail(FailureFormat, expected, actual);
        }
        //
        // Summary:
        //     Verifies that two objects are equal. Two objects are considered equal if both
        //     are null, or if both have the same value. NUnit has special semantics for some
        //     object types. If they are not equal an NUnit.Framework.AssertionException is
        //     thrown.
        //
        // Parameters:
        //   expected:
        //     The value that is expected
        //
        //   actual:
        //     The actual value
        //
        //   message:
        //     The message to display in case of failure
        //
        //   args:
        //     Array of objects to be used in formatting the message
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<T>(T expected, T actual, string message, params object[] args)
        {
            if (!JCG.EqualityComparer<T>.Default.Equals(expected, actual))
                Fail(FormatErrorMessage(expected, actual, message, args));
        }

        //
        // Summary:
        //     Verifies that two objects are equal. Two objects are considered equal if both
        //     are null, or if both have the same value. NUnit has special semantics for some
        //     object types. If they are not equal an NUnit.Framework.AssertionException is
        //     thrown.
        //
        // Parameters:
        //   expected:
        //     The value that is expected
        //
        //   actual:
        //     The actual value
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(string expected, string actual)
        {
            if (!StringComparer.Ordinal.Equals(expected, actual))
                Fail(FailureFormat, expected, actual);
        }

        //
        // Summary:
        //     Verifies that two objects are equal. Two objects are considered equal if both
        //     are null, or if both have the same value. NUnit has special semantics for some
        //     object types. If they are not equal an NUnit.Framework.AssertionException is
        //     thrown.
        //
        // Parameters:
        //   expected:
        //     The value that is expected
        //
        //   message:
        //     The message to display in case of failure
        //
        //   actual:
        //     The actual value
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(string expected, string actual, string message, params object[] args)
        {
            if (!StringComparer.Ordinal.Equals(expected, actual))
                Fail(FormatErrorMessage(expected, actual, message, args));
        }

        //
        // Summary:
        //     Verifies that two objects are equal. Two objects are considered equal if both
        //     are null, or if both have the same value. NUnit has special semantics for some
        //     object types. If they are not equal an NUnit.Framework.AssertionException is
        //     thrown.
        //
        // Parameters:
        //   expected:
        //     The value that is expected
        //
        //   actual:
        //     The actual value
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(bool expected, bool actual)
        {
            if (!expected.Equals(actual))
                Fail(FailureFormat, expected, actual);
        }
        //
        // Summary:
        //     Verifies that two objects are equal. Two objects are considered equal if both
        //     are null, or if both have the same value. NUnit has special semantics for some
        //     object types. If they are not equal an NUnit.Framework.AssertionException is
        //     thrown.
        //
        // Parameters:
        //   expected:
        //     The value that is expected
        //
        //   actual:
        //     The actual value
        //
        //   message:
        //     The message to display in case of failure
        //
        //   args:
        //     Array of objects to be used in formatting the message
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(bool expected, bool actual, string message, params object[] args)
        {
            if (!expected.Equals(actual))
                Fail(FormatErrorMessage(expected, actual, message, args));
        }

        //
        // Summary:
        //     Verifies that two doubles are equal considering a delta. If the expected value
        //     is infinity then the delta value is ignored. If they are not equal then an NUnit.Framework.AssertionException
        //     is thrown.
        //
        // Parameters:
        //   expected:
        //     The expected value
        //
        //   actual:
        //     The actual value
        //
        //   delta:
        //     The maximum acceptable difference between the the expected and the actual
        //
        //   message:
        //     The message to display in case of failure
        //
        //   args:
        //     Array of objects to be used in formatting the message
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(double expected, double actual, double delta, string message, params object[] args)
        {
            if (Math.Abs(expected - actual) > delta)
                _NUnit.Assert.AreEqual(expected, actual, delta, message, args);
        }
        //
        // Summary:
        //     Verifies that two doubles are equal considering a delta. If the expected value
        //     is infinity then the delta value is ignored. If they are not equal then an NUnit.Framework.AssertionException
        //     is thrown.
        //
        // Parameters:
        //   expected:
        //     The expected value
        //
        //   actual:
        //     The actual value
        //
        //   delta:
        //     The maximum acceptable difference between the the expected and the actual
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(double expected, double actual, double delta)
        {
            if (Math.Abs(expected - actual) > delta)
                _NUnit.Assert.AreEqual(expected, actual, delta);
        }
        //
        // Summary:
        //     Verifies that two doubles are equal considering a delta. If the expected value
        //     is infinity then the delta value is ignored. If they are not equal then an NUnit.Framework.AssertionException
        //     is thrown.
        //
        // Parameters:
        //   expected:
        //     The expected value
        //
        //   actual:
        //     The actual value
        //
        //   delta:
        //     The maximum acceptable difference between the the expected and the actual
        //
        //   message:
        //     The message to display in case of failure
        //
        //   args:
        //     Array of objects to be used in formatting the message
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(float expected, float actual, float delta, string message, params object[] args)
        {
            if (Math.Abs(expected - actual) > delta)
                _NUnit.Assert.AreEqual(expected, actual, delta, message, args);
        }
        //
        // Summary:
        //     Verifies that two doubles are equal considering a delta. If the expected value
        //     is infinity then the delta value is ignored. If they are not equal then an NUnit.Framework.AssertionException
        //     is thrown.
        //
        // Parameters:
        //   expected:
        //     The expected value
        //
        //   actual:
        //     The actual value
        //
        //   delta:
        //     The maximum acceptable difference between the the expected and the actual
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(float expected, float actual, float delta)
        {
            if (Math.Abs(expected - actual) > delta)
                _NUnit.Assert.AreEqual(expected, actual, delta);
        }
        //
        // Summary:
        //     Verifies that two objects are equal. Two objects are considered equal if both
        //     are null, or if both have the same value. NUnit has special semantics for some
        //     object types. If they are not equal an NUnit.Framework.AssertionException is
        //     thrown.
        //
        // Parameters:
        //   expected:
        //     The value that is expected
        //
        //   actual:
        //     The actual value
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(int expected, int actual)
        {
            if (!expected.Equals(actual))
                Fail(FailureFormat, expected, actual);
        }

        //
        // Summary:
        //     Verifies that two objects are equal. Two objects are considered equal if both
        //     are null, or if both have the same value. NUnit has special semantics for some
        //     object types. If they are not equal an NUnit.Framework.AssertionException is
        //     thrown.
        //
        // Parameters:
        //   expected:
        //     The value that is expected
        //
        //   message:
        //     The message to display in case of failure
        //
        //   actual:
        //     The actual value
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(int expected, int actual, string message, params object[] args)
        {
            if (!expected.Equals(actual))
                Fail(FormatErrorMessage(expected, actual, message, args));
        }

        //
        // Summary:
        //     Verifies that two objects are equal. Two objects are considered equal if both
        //     are null, or if both have the same value. NUnit has special semantics for some
        //     object types. If they are not equal an NUnit.Framework.AssertionException is
        //     thrown.
        //
        // Parameters:
        //   expected:
        //     The value that is expected
        //
        //   actual:
        //     The actual value
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(long expected, long actual)
        {
            if (!expected.Equals(actual))
                Fail(FailureFormat, expected, actual);
        }

        //
        // Summary:
        //     Verifies that two objects are equal. Two objects are considered equal if both
        //     are null, or if both have the same value. NUnit has special semantics for some
        //     object types. If they are not equal an NUnit.Framework.AssertionException is
        //     thrown.
        //
        // Parameters:
        //   expected:
        //     The value that is expected
        //
        //   message:
        //     The message to display in case of failure
        //
        //   actual:
        //     The actual value
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(long expected, long actual, string message, params object[] args)
        {
            if (!expected.Equals(actual))
                Fail(FormatErrorMessage(expected, actual, message, args));
        }

        //
        // Summary:
        //     Verifies that two objects are equal. Two objects are considered equal if both
        //     are null, or if both have the same value. NUnit has special semantics for some
        //     object types. If they are not equal an NUnit.Framework.AssertionException is
        //     thrown.
        //
        // Parameters:
        //   expected:
        //     The value that is expected
        //
        //   actual:
        //     The actual value
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(byte expected, byte actual)
        {
            if (!expected.Equals(actual))
                Fail(FailureFormat, expected, actual);
        }

        //
        // Summary:
        //     Verifies that two objects are equal. Two objects are considered equal if both
        //     are null, or if both have the same value. NUnit has special semantics for some
        //     object types. If they are not equal an NUnit.Framework.AssertionException is
        //     thrown.
        //
        // Parameters:
        //   expected:
        //     The value that is expected
        //
        //   message:
        //     The message to display in case of failure
        //
        //   actual:
        //     The actual value
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(byte expected, byte actual, string message, params object[] args)
        {
            if (!expected.Equals(actual))
                Fail(FormatErrorMessage(expected, actual, message, args));
        }


        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static JCG.SetEqualityComparer<T> GetSetComparer<T>(bool aggressive)
        {
            return aggressive
                ? JCG.SetEqualityComparer<T>.Aggressive
                : JCG.SetEqualityComparer<T>.Default;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static JCG.ListEqualityComparer<T> GetListComparer<T>(bool aggressive)
        {
            return aggressive
                ? JCG.ListEqualityComparer<T>.Aggressive
                : JCG.ListEqualityComparer<T>.Default;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static JCG.DictionaryEqualityComparer<TKey, TValue> GetDictionaryComparer<TKey, TValue>(bool aggressive)
        {
            return aggressive
                ? JCG.DictionaryEqualityComparer<TKey, TValue>.Aggressive
                : JCG.DictionaryEqualityComparer<TKey, TValue>.Default;
        }

        private static string FormatErrorMessage(object expected, object actual, string message, params object[] args)
        {
            string failureHeader = string.Format(FailureFormat, expected, actual);
            string msg = args is null || args.Length == 0 ? message : string.Format(message, args);
            return string.Concat(failureHeader, Environment.NewLine, Environment.NewLine, msg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatCollection(object collection)
        {
            return string.Format(StringFormatter.CurrentCulture, "{0}", collection);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<T>(ISet<T> expected, ISet<T> actual, bool aggressive = true)
        {
            if (!GetSetComparer<T>(aggressive).Equals(expected, actual))
                Fail(FailureFormat, FormatCollection(expected), FormatCollection(actual));
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<T>(ISet<T> expected, ISet<T> actual, bool aggressive, string message, params object[] args)
        {
            //Fail(FormatErrorMessage(expected, actual, message, args));
            if (!GetSetComparer<T>(aggressive).Equals(expected, actual))
                Fail(FormatErrorMessage(FormatCollection(expected), FormatCollection(actual), message, args));
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<T>(ISet<T> expected, ISet<T> actual, bool aggressive, Func<string> getMessage)
        {
            if (!GetSetComparer<T>(aggressive).Equals(expected, actual))
                Fail(FormatErrorMessage(FormatCollection(expected), FormatCollection(actual), getMessage()));
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<T>(IList<T> expected, IList<T> actual, bool aggressive = true)
        {
            if (!GetListComparer<T>(aggressive).Equals(expected, actual))
                Fail(string.Format(FailureFormat, FormatCollection(expected), FormatCollection(actual)));
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<T>(IList<T> expected, IList<T> actual, bool aggressive, string message, params object[] args)
        {
            if (!GetListComparer<T>(aggressive).Equals(expected, actual))
                Fail(FormatErrorMessage(FormatCollection(expected), FormatCollection(actual), message, args));
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<T>(IList<T> expected, IList<T> actual, bool aggressive, Func<string> getMessage)
        {
            if (!GetListComparer<T>(aggressive).Equals(expected, actual))
                Fail(FormatErrorMessage(FormatCollection(expected), FormatCollection(actual), getMessage()));
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<TKey, TValue>(IDictionary<TKey, TValue> expected, IDictionary<TKey, TValue> actual, bool aggressive = true)
        {
            if (!GetDictionaryComparer<TKey, TValue>(aggressive).Equals(expected, actual))
                Fail(FailureFormat, FormatCollection(expected), FormatCollection(actual));
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<TKey, TValue>(IDictionary<TKey, TValue> expected, IDictionary<TKey, TValue> actual, bool aggressive, string message, params object[] args)
        {
            if (!GetDictionaryComparer<TKey, TValue>(aggressive).Equals(expected, actual))
                Fail(FormatErrorMessage(FormatCollection(expected), FormatCollection(actual), message, args));
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<TKey, TValue>(IDictionary<TKey, TValue> expected, IDictionary<TKey, TValue> actual, bool aggressive, Func<string> getMessage)
        {
            if (!GetDictionaryComparer<TKey, TValue>(aggressive).Equals(expected, actual))
                Fail(FormatErrorMessage(FormatCollection(expected), FormatCollection(actual), getMessage()));
        }


        // From CollectionAssert
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<T>(T[] expected, T[] actual)
        {
            if (!J2N.Collections.ArrayEqualityComparer<T>.OneDimensional.Equals(expected, actual))
                Fail(FailureFormat, FormatCollection(expected), FormatCollection(actual));
        }

        // From CollectionAssert
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<T>(T[] expected, T[] actual, string message, params object[] args)
        {
            if (!J2N.Collections.ArrayEqualityComparer<T>.OneDimensional.Equals(expected, actual))
                Fail(FormatErrorMessage(FormatCollection(expected), FormatCollection(actual), message, args));
        }

        // From CollectionAssert
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<T>(T[] expected, T[] actual, Func<string> getMessage)
        {
            if (!J2N.Collections.ArrayEqualityComparer<T>.OneDimensional.Equals(expected, actual))
                Fail(FormatErrorMessage(FormatCollection(expected), FormatCollection(actual), getMessage()));
        }

        //
        // Summary:
        //     Verifies that two objects are not equal. Two objects are considered equal if
        //     both are null, or if both have the same value. NUnit has special semantics for
        //     some object types. If they are equal an NUnit.Framework.AssertionException is
        //     thrown.
        //
        // Parameters:
        //   expected:
        //     The value that is expected
        //
        //   actual:
        //     The actual value
        //
        //   message:
        //     The message to display in case of failure
        //
        //   args:
        //     Array of objects to be used in formatting the message
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreNotEqual(object expected, object actual, string message, params object[] args)
        {
            _NUnit.Assert.AreNotEqual(expected, actual, message, args);
        }
        //
        // Summary:
        //     Verifies that two objects are not equal. Two objects are considered equal if
        //     both are null, or if both have the same value. NUnit has special semantics for
        //     some object types. If they are equal an NUnit.Framework.AssertionException is
        //     thrown.
        //
        // Parameters:
        //   expected:
        //     The value that is expected
        //
        //   actual:
        //     The actual value
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreNotEqual(object expected, object actual)
        {
            _NUnit.Assert.AreNotEqual(expected, actual);
        }
        //
        // Summary:
        //     Asserts that two objects do not refer to the same object. If they are the same
        //     an NUnit.Framework.AssertionException is thrown.
        //
        // Parameters:
        //   expected:
        //     The expected object
        //
        //   actual:
        //     The actual object
        //
        //   message:
        //     The message to display in case of failure
        //
        //   args:
        //     Array of objects to be used in formatting the message
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreNotSame(object expected, object actual, string message, params object[] args)
        {
            _NUnit.Assert.AreNotSame(expected, actual, message, args);
        }
        //
        // Summary:
        //     Asserts that two objects do not refer to the same object. If they are the same
        //     an NUnit.Framework.AssertionException is thrown.
        //
        // Parameters:
        //   expected:
        //     The expected object
        //
        //   actual:
        //     The actual object
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreNotSame(object expected, object actual)
        {
            _NUnit.Assert.AreNotSame(expected, actual);
        }
        //
        // Summary:
        //     Asserts that two objects refer to the same object. If they are not the same an
        //     NUnit.Framework.AssertionException is thrown.
        //
        // Parameters:
        //   expected:
        //     The expected object
        //
        //   actual:
        //     The actual object
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreSame(object expected, object actual)
        {
            _NUnit.Assert.AreSame(expected, actual);
        }
        //
        // Summary:
        //     Asserts that two objects refer to the same object. If they are not the same an
        //     NUnit.Framework.AssertionException is thrown.
        //
        // Parameters:
        //   expected:
        //     The expected object
        //
        //   actual:
        //     The actual object
        //
        //   message:
        //     The message to display in case of failure
        //
        //   args:
        //     Array of objects to be used in formatting the message
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreSame(object expected, object actual, string message, params object[] args)
        {
            _NUnit.Assert.AreSame(expected, actual, message, args);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fail(string message, params object[] args)
        {
            _NUnit.Assert.Fail(message, args);
        }
        //
        // Summary:
        //     Throws an NUnit.Framework.AssertionException. This is used by the other Assert
        //     functions.
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fail()
        {
            _NUnit.Assert.Fail();
        }
        //
        // Summary:
        //     Throws an NUnit.Framework.AssertionException with the message that is passed
        //     in. This is used by the other Assert functions.
        //
        // Parameters:
        //   message:
        //     The message to initialize the NUnit.Framework.AssertionException with.
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fail(string message)
        {
            _NUnit.Assert.Fail(message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Pass(string message, params object[] args)
        {
            _NUnit.Assert.Pass(message, args);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Pass(string message)
        {
            _NUnit.Assert.Pass(message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Pass()
        {
            _NUnit.Assert.Pass();
        }

        //
        // Summary:
        //     Asserts that a condition is false. If the condition is true the method throws
        //     an NUnit.Framework.AssertionException.
        //
        // Parameters:
        //   condition:
        //     The evaluated condition
        //
        //   message:
        //     The message to display in case of failure
        //
        //   args:
        //     Array of objects to be used in formatting the message
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void False(bool condition, string message, params object[] args)
        {
            if (condition)
                _NUnit.Assert.Fail(message, args);
        }
        //
        // Summary:
        //     Asserts that a condition is false. If the condition is true the method throws
        //     an NUnit.Framework.AssertionException.
        //
        // Parameters:
        //   condition:
        //     The evaluated condition
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void False(bool condition)
        {
            if (condition)
                _NUnit.Assert.Fail("Expected: False  Actual: True");
        }

        //
        // Summary:
        //     Asserts that a condition is false. If the condition is true the method throws
        //     an NUnit.Framework.AssertionException.
        //
        // Parameters:
        //   condition:
        //     The evaluated condition
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsFalse(bool condition)
        {
            if (condition)
                _NUnit.Assert.Fail("Expected: False  Actual: True");
        }

        //
        // Summary:
        //     Asserts that a condition is false. If the condition is true the method throws
        //     an NUnit.Framework.AssertionException.
        //
        // Parameters:
        //   condition:
        //     The evaluated condition
        //
        //   message:
        //     The message to display in case of failure
        //
        //   args:
        //     Array of objects to be used in formatting the message
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsFalse(bool condition, string message, params object[] args)
        {
            if (condition)
                _NUnit.Assert.Fail(message, args);
        }

        //
        // Summary:
        //     Verifies that the object that is passed in is not equal to null If the object
        //     is null then an NUnit.Framework.AssertionException is thrown.
        //
        // Parameters:
        //   anObject:
        //     The object that is to be tested
        //
        //   message:
        //     The message to display in case of failure
        //
        //   args:
        //     Array of objects to be used in formatting the message
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNotNull(object anObject, string message, params object[] args)
        {
            _NUnit.Assert.IsNotNull(anObject, message, args);
        }
        //
        // Summary:
        //     Verifies that the object that is passed in is not equal to null If the object
        //     is null then an NUnit.Framework.AssertionException is thrown.
        //
        // Parameters:
        //   anObject:
        //     The object that is to be tested
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNotNull(object anObject)
        {
            _NUnit.Assert.IsNotNull(anObject);
        }
        //
        // Summary:
        //     Verifies that the object that is passed in is equal to null If the object is
        //     not null then an NUnit.Framework.AssertionException is thrown.
        //
        // Parameters:
        //   anObject:
        //     The object that is to be tested
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNull(object anObject)
        {
            _NUnit.Assert.IsNull(anObject);
        }
        //
        // Summary:
        //     Verifies that the object that is passed in is equal to null If the object is
        //     not null then an NUnit.Framework.AssertionException is thrown.
        //
        // Parameters:
        //   anObject:
        //     The object that is to be tested
        //
        //   message:
        //     The message to display in case of failure
        //
        //   args:
        //     Array of objects to be used in formatting the message
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNull(object anObject, string message, params object[] args)
        {
            _NUnit.Assert.IsNull(anObject, message, args);
        }

        //
        // Summary:
        //     Asserts that a condition is true. If the condition is false the method throws
        //     an NUnit.Framework.AssertionException.
        //
        // Parameters:
        //   condition:
        //     The evaluated condition
        //
        //   message:
        //     The message to display in case of failure
        //
        //   args:
        //     Array of objects to be used in formatting the message
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsTrue(bool condition, string message, params object[] args)
        {
            if (!condition)
                _NUnit.Assert.Fail(message, args);
        }

        //
        // Summary:
        //     Asserts that a condition is true. If the condition is false the method throws
        //     an NUnit.Framework.AssertionException.
        //
        // Parameters:
        //   condition:
        //     The evaluated condition
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsTrue(bool condition)
        {
            if (!condition)
                _NUnit.Assert.Fail("Expected: True  Actual: False");
        }

        //
        // Summary:
        //     Verifies that the object that is passed in is not equal to null If the object
        //     is null then an NUnit.Framework.AssertionException is thrown.
        //
        // Parameters:
        //   anObject:
        //     The object that is to be tested
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull(object anObject)
        {
            if (!(anObject is null))
                _NUnit.Assert.NotNull(anObject);
        }
        //
        // Summary:
        //     Verifies that the object that is passed in is not equal to null If the object
        //     is null then an NUnit.Framework.AssertionException is thrown.
        //
        // Parameters:
        //   anObject:
        //     The object that is to be tested
        //
        //   message:
        //     The message to display in case of failure
        //
        //   args:
        //     Array of objects to be used in formatting the message
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull(object anObject, string message, params object[] args)
        {
            if (anObject is null)
                _NUnit.Assert.NotNull(anObject, message, args);
        }

        //
        // Summary:
        //     Verifies that the object that is passed in is equal to null If the object is
        //     not null then an NUnit.Framework.AssertionException is thrown.
        //
        // Parameters:
        //   anObject:
        //     The object that is to be tested
        //
        //   message:
        //     The message to display in case of failure
        //
        //   args:void Null
        //     Array of objects to be used in formatting the message
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Null(object anObject, string message, params object[] args)
        {
            if (!(anObject is null))
                _NUnit.Assert.Null(anObject, message, args);
        }
        //
        // Summary:
        //     Verifies that the object that is passed in is equal to null If the object is
        //     not null then an NUnit.Framework.AssertionException is thrown.
        //
        // Parameters:
        //   anObject:
        //     The object that is to be tested
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Null(object anObject)
        {
            if (!(anObject is null))
                _NUnit.Assert.Null(anObject);
        }

        //
        // Summary:
        //     Asserts that a condition is true. If the condition is false the method throws
        //     an NUnit.Framework.AssertionException.
        //
        // Parameters:
        //   condition:
        //     The evaluated condition
        //
        //   message:
        //     The message to display in case of failure
        //
        //   args:
        //     Array of objects to be used in formatting the message
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void True(bool condition, string message, params object[] args)
        {
            if (!condition)
                _NUnit.Assert.Fail(message, args);
        }

        //
        // Summary:
        //     Asserts that a condition is true. If the condition is false the method throws
        //     an NUnit.Framework.AssertionException.
        //
        // Parameters:
        //   condition:
        //     The evaluated condition
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void True(bool condition)
        {
            if (!condition)
                _NUnit.Assert.Fail("Expected: True  Actual: False");
        }


        //
        // Summary:
        //     Verifies that the object that is passed in is not equal to null If the object
        //     is null then an NUnit.Framework.AssertionException is thrown.
        //
        // Parameters:
        //   anObject:
        //     The object that is to be tested
        //
        //   message:
        //     The message to display in case of failure
        //
        //   args:
        //     Array of objects to be used in formatting the message
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNotEmpty(string aString, string message, params object[] args)
        {
            if (string.Empty.Equals(aString))
                _NUnit.Assert.IsNotEmpty(aString, message, args);
        }
        //
        // Summary:
        //     Verifies that the object that is passed in is not equal to null If the object
        //     is null then an NUnit.Framework.AssertionException is thrown.
        //
        // Parameters:
        //   anObject:
        //     The object that is to be tested
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNotEmpty(string aString)
        {
            if (string.Empty.Equals(aString))
                _NUnit.Assert.IsNotEmpty(aString);
        }


        //
        // Summary:
        //     Verifies that the object that is passed in is not equal to null If the object
        //     is null then an NUnit.Framework.AssertionException is thrown.
        //
        // Parameters:
        //   anObject:
        //     The object that is to be tested
        //
        //   message:
        //     The message to display in case of failure
        //
        //   args:
        //     Array of objects to be used in formatting the message
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsEmpty(string aString, string message, params object[] args)
        {
            if (!string.Empty.Equals(aString))
                _NUnit.Assert.IsEmpty(aString, message, args);
        }
        //
        // Summary:
        //     Verifies that the object that is passed in is not equal to null If the object
        //     is null then an NUnit.Framework.AssertionException is thrown.
        //
        // Parameters:
        //   anObject:
        //     The object that is to be tested
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsEmpty(string aString)
        {
            if (!string.Empty.Equals(aString))
                _NUnit.Assert.IsEmpty(aString);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LessOrEqual(int arg1, int arg2)
        {
            if (arg1 > arg2)
                _NUnit.Assert.LessOrEqual(arg1, arg2);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Greater(int arg1, int arg2)
        {
            if (arg1 <= arg2)
                _NUnit.Assert.Greater(arg1, arg2);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DoesNotThrow(Action action, string message, params object[] args)
        {
            _NUnit.Assert.DoesNotThrow(() => action(), message, args);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DoesNotThrow(Action action)
        {
            _NUnit.Assert.DoesNotThrow(() => action());
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Throws<TException>(Action action, string message, params object[] args)
        {
            return Throws(typeof(TException), action, message, args);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Throws<TException>(Action action)
        {
            return Throws(typeof(TException), action);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Throws(Type expectedExceptionType, Action action)
        {
            return _NUnit.Assert.Throws(expectedExceptionType, () => action());
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Throws(Type expectedExceptionType, Action action, string message, params object[] args)
        {
            return _NUnit.Assert.Throws(expectedExceptionType, () => action(), message, args);
        }

        [DebuggerStepThrough]
        public static Exception ThrowsFileAlreadyExistsException(string filePath, Action action)
        {
            var messagePrefix = $"Expected: IOException indicating file not found\nBut was:";
            try
            {
                action();
                throw new _NUnit.AssertionException($"{messagePrefix} <null>");
            }
            catch (Exception ex) when (!FileSupport.IsFileAlreadyExistsException(ex, filePath))
            {
                throw new _NUnit.AssertionException($"{messagePrefix} {ex.GetType().FullName}", ex);
            }
            catch (Exception ex)
            {
                return ex; // Success
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception ThrowsAnyOf<TException1, TException2>(Action action)
        {
            return ThrowsAnyOf(new Type[] { typeof(TException1), typeof(TException2) }, action);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception ThrowsAnyOf<TException1, TException2, TException3>(Action action)
        {
            return ThrowsAnyOf(new Type[] { typeof(TException1), typeof(TException2), typeof(TException3) }, action);
        }

        [DebuggerStepThrough]
        public static Exception ThrowsAnyOf(IEnumerable<Type> expectedExceptionTypes, Action action)
        {
            Exception exception = null;
            try
            {
                action();
            }
            catch (Exception ex) when (!expectedExceptionTypes.Contains(ex.GetType()))
            {
                exception = ex;
            }
            catch (Exception ex)
            {
                return ex; // Success
            }
            string exString = exception is null ? "<null>" : exception.GetType().FullName;
            throw new _NUnit.AssertionException($"Expected one of: {Collections.ToString(expectedExceptionTypes.Select(ex => ex.FullName).ToArray())}\nBut was: {exString}");
        }
    }
}
