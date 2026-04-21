using J2N.Text;
using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using _NUnit = NUnit.Framework.Legacy;
using JCG = J2N.Collections.Generic;
// ReSharper disable AccessToStaticMemberViaDerivedType

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
    internal class Assert
    {
        private const string FailureFormat = "Expected: {0}, Actual: {1}";

        /// <summary>
        /// We don't actually want any instances of this object, but some people like to
        /// inherit from it to add other static methods. Hence, the protected constructor
        /// disallows any instances of this object.
        /// </summary>
        protected Assert()
        { }

        /// <summary>
        /// Verifies that two objects are equal. Two objects are considered equal if both
        /// are <c>null</c>, or if both have the same value. NUnit has special semantics for some
        /// object types. If they are not equal an <see cref="AssertionException"/> is
        /// thrown.
        /// </summary>
        /// <param name="expected">The value that is expected.</param>
        /// <param name="actual">The actual value.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<T>(T expected, T actual)
        {
            if (!JCG.EqualityComparer<T>.Default.Equals(expected, actual))
                Fail(FailureFormat, expected, actual);
        }

        /// <summary>
        /// Verifies that two objects are equal. Two objects are considered equal if both
        /// are <c>null</c>, or if both have the same value. NUnit has special semantics for some
        /// object types. If they are not equal an <see cref="AssertionException"/> is
        /// thrown.
        /// </summary>
        /// <param name="expected">The value that is expected.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<T>(T expected, T actual, string message, params object[] args)
        {
            if (!JCG.EqualityComparer<T>.Default.Equals(expected, actual))
                Fail(FormatErrorMessage(expected, actual, message, args));
        }

        /// <summary>
        /// Verifies that two objects are equal. Two objects are considered equal if both
        /// are <c>null</c>, or if both have the same value. NUnit has special semantics for some
        /// object types. If they are not equal an <see cref="AssertionException"/> is
        /// thrown.
        /// </summary>
        /// <param name="expected">The value that is expected.</param>
        /// <param name="actual">The actual value.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(string expected, string actual)
        {
            if (!StringComparer.Ordinal.Equals(expected, actual))
                Fail(FailureFormat, expected, actual);
        }

        /// <summary>
        /// Verifies that two objects are equal. Two objects are considered equal if both
        /// are <c>null</c>, or if both have the same value. NUnit has special semantics for some
        /// object types. If they are not equal an <see cref="AssertionException"/> is
        /// thrown.
        /// </summary>
        /// <param name="expected">The value that is expected.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(string expected, string actual, string message, params object[] args)
        {
            if (!StringComparer.Ordinal.Equals(expected, actual))
                Fail(FormatErrorMessage(expected, actual, message, args));
        }

        /// <summary>
        /// Verifies that two objects are equal. Two objects are considered equal if both
        /// are <c>null</c>, or if both have the same value. NUnit has special semantics for some
        /// object types. If they are not equal an <see cref="AssertionException"/> is
        /// thrown.
        /// </summary>
        /// <param name="expected">The value that is expected.</param>
        /// <param name="actual">The actual value.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(bool expected, bool actual)
        {
            if (!expected.Equals(actual))
                Fail(FailureFormat, expected, actual);
        }

        /// <summary>
        /// Verifies that two objects are equal. Two objects are considered equal if both
        /// are <c>null</c>, or if both have the same value. NUnit has special semantics for some
        /// object types. If they are not equal an <see cref="AssertionException"/> is
        /// thrown.
        /// </summary>
        /// <param name="expected">The value that is expected.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(bool expected, bool actual, string message, params object[] args)
        {
            if (!expected.Equals(actual))
                Fail(FormatErrorMessage(expected, actual, message, args));
        }

        /// <summary>
        /// Verifies that two doubles are equal considering a delta. If the expected value
        /// is infinity then the delta value is ignored. If they are not equal then an
        /// <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="delta">The maximum acceptable difference between the expected and the actual.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(double expected, double actual, double delta, string message, params object[] args)
        {
            if (Math.Abs(expected - actual) > delta)
                _NUnit.ClassicAssert.AreEqual(expected, actual, delta, message, args);
        }

        /// <summary>
        /// Verifies that two doubles are equal considering a delta. If the expected value
        /// is infinity then the delta value is ignored. If they are not equal then an
        /// <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="delta">The maximum acceptable difference between the expected and the actual.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(double expected, double actual, double delta)
        {
            if (Math.Abs(expected - actual) > delta)
                _NUnit.ClassicAssert.AreEqual(expected, actual, delta);
        }

        /// <summary>
        /// Verifies that two floats are equal considering a delta. If the expected value
        /// is infinity then the delta value is ignored. If they are not equal then an
        /// <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="delta">The maximum acceptable difference between the expected and the actual.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(float expected, float actual, float delta, string message, params object[] args)
        {
            if (Math.Abs(expected - actual) > delta)
                _NUnit.ClassicAssert.AreEqual(expected, actual, delta, message, args);
        }

        /// <summary>
        /// Verifies that two floats are equal considering a delta. If the expected value
        /// is infinity then the delta value is ignored. If they are not equal then an
        /// <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="delta">The maximum acceptable difference between the expected and the actual.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(float expected, float actual, float delta)
        {
            if (Math.Abs(expected - actual) > delta)
                _NUnit.ClassicAssert.AreEqual(expected, actual, delta);
        }

        /// <summary>
        /// Verifies that two objects are equal. Two objects are considered equal if both
        /// are <c>null</c>, or if both have the same value. NUnit has special semantics for some
        /// object types. If they are not equal an <see cref="AssertionException"/> is
        /// thrown.
        /// </summary>
        /// <param name="expected">The value that is expected.</param>
        /// <param name="actual">The actual value.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(int expected, int actual)
        {
            if (!expected.Equals(actual))
                Fail(FailureFormat, expected, actual);
        }

        /// <summary>
        /// Verifies that two objects are equal. Two objects are considered equal if both
        /// are <c>null</c>, or if both have the same value. NUnit has special semantics for some
        /// object types. If they are not equal an <see cref="AssertionException"/> is
        /// thrown.
        /// </summary>
        /// <param name="expected">The value that is expected.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(int expected, int actual, string message, params object[] args)
        {
            if (!expected.Equals(actual))
                Fail(FormatErrorMessage(expected, actual, message, args));
        }

        /// <summary>
        /// Verifies that two objects are equal. Two objects are considered equal if both
        /// are <c>null</c>, or if both have the same value. NUnit has special semantics for some
        /// object types. If they are not equal an <see cref="AssertionException"/> is
        /// thrown.
        /// </summary>
        /// <param name="expected">The value that is expected.</param>
        /// <param name="actual">The actual value.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(long expected, long actual)
        {
            if (!expected.Equals(actual))
                Fail(FailureFormat, expected, actual);
        }

        /// <summary>
        /// Verifies that two objects are equal. Two objects are considered equal if both
        /// are <c>null</c>, or if both have the same value. NUnit has special semantics for some
        /// object types. If they are not equal an <see cref="AssertionException"/> is
        /// thrown.
        /// </summary>
        /// <param name="expected">The value that is expected.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(long expected, long actual, string message, params object[] args)
        {
            if (!expected.Equals(actual))
                Fail(FormatErrorMessage(expected, actual, message, args));
        }

        /// <summary>
        /// Verifies that two objects are equal. Two objects are considered equal if both
        /// are <c>null</c>, or if both have the same value. NUnit has special semantics for some
        /// object types. If they are not equal an <see cref="AssertionException"/> is
        /// thrown.
        /// </summary>
        /// <param name="expected">The value that is expected.</param>
        /// <param name="actual">The actual value.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual(byte expected, byte actual)
        {
            if (!expected.Equals(actual))
                Fail(FailureFormat, expected, actual);
        }

        /// <summary>
        /// Verifies that two objects are equal. Two objects are considered equal if both
        /// are <c>null</c>, or if both have the same value. NUnit has special semantics for some
        /// object types. If they are not equal an <see cref="AssertionException"/> is
        /// thrown.
        /// </summary>
        /// <param name="expected">The value that is expected.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
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

        /// <summary>
        /// Verifies that two objects are not equal. Two objects are considered equal if
        /// both are <c>null</c>, or if both have the same value. NUnit has special semantics for
        /// some object types. If they are equal an <see cref="AssertionException"/> is
        /// thrown.
        /// </summary>
        /// <param name="expected">The value that is expected.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreNotEqual(object expected, object actual, string message, params object[] args)
        {
            _NUnit.ClassicAssert.AreNotEqual(expected, actual, message, args);
        }

        /// <summary>
        /// Verifies that two objects are not equal. Two objects are considered equal if
        /// both are <c>null</c>, or if both have the same value. NUnit has special semantics for
        /// some object types. If they are equal an <see cref="AssertionException"/> is
        /// thrown.
        /// </summary>
        /// <param name="expected">The value that is expected.</param>
        /// <param name="actual">The actual value.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreNotEqual(object expected, object actual)
        {
            _NUnit.ClassicAssert.AreNotEqual(expected, actual);
        }

        /// <summary>
        /// Asserts that two objects do not refer to the same object. If they are the same
        /// an <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreNotSame(object expected, object actual, string message, params object[] args)
        {
            _NUnit.ClassicAssert.AreNotSame(expected, actual, message, args);
        }

        /// <summary>
        /// Asserts that two objects do not refer to the same object. If they are the same
        /// an <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreNotSame(object expected, object actual)
        {
            _NUnit.ClassicAssert.AreNotSame(expected, actual);
        }

        /// <summary>
        /// Asserts that two objects refer to the same object. If they are not the same an
        /// <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreSame(object expected, object actual)
        {
            _NUnit.ClassicAssert.AreSame(expected, actual);
        }

        /// <summary>
        /// Asserts that two objects refer to the same object. If they are not the same an
        /// <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="expected">The expected object.</param>
        /// <param name="actual">The actual object.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreSame(object expected, object actual, string message, params object[] args)
        {
            _NUnit.ClassicAssert.AreSame(expected, actual, message, args);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fail(string message, params object[] args)
        {
            _NUnit.ClassicAssert.Fail(string.Format(message, args));
        }

        /// <summary>
        /// Throws an <see cref="AssertionException"/>. This is used by the other <see cref="Assert"/>
        /// functions.
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fail()
        {
            _NUnit.ClassicAssert.Fail();
        }

        /// <summary>
        /// Throws an <see cref="AssertionException"/> with the message that is passed
        /// in. This is used by the other <see cref="Assert"/> functions.
        /// </summary>
        /// <param name="message">The message to initialize the <see cref="AssertionException"/> with.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Fail(string message)
        {
            _NUnit.ClassicAssert.Fail(message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Pass(string message, params object[] args)
        {
            _NUnit.ClassicAssert.Pass(string.Format(message, args));
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Pass(string message)
        {
            _NUnit.ClassicAssert.Pass(message);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Pass()
        {
            _NUnit.ClassicAssert.Pass();
        }

        /// <summary>
        /// Asserts that a condition is <c>false</c>. If the condition is <c>true</c> the method throws
        /// an <see cref="AssertionException"/>.
        /// </summary>
        /// <param name="condition">The evaluated condition.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void False(bool condition, string message, params object[] args)
        {
            if (condition)
                _NUnit.ClassicAssert.Fail(string.Format(message, args));
        }

        /// <summary>
        /// Asserts that a condition is <c>false</c>. If the condition is <c>true</c> the method throws
        /// an <see cref="AssertionException"/>.
        /// </summary>
        /// <param name="condition">The evaluated condition.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void False(bool condition)
        {
            if (condition)
                _NUnit.ClassicAssert.Fail("Expected: False  Actual: True");
        }

        /// <summary>
        /// Asserts that a condition is <c>false</c>. If the condition is <c>true</c> the method throws
        /// an <see cref="AssertionException"/>.
        /// </summary>
        /// <param name="condition">The evaluated condition.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsFalse(bool condition)
        {
            if (condition)
                _NUnit.ClassicAssert.Fail("Expected: False  Actual: True");
        }

        /// <summary>
        /// Asserts that a condition is <c>false</c>. If the condition is <c>true</c> the method throws
        /// an <see cref="AssertionException"/>.
        /// </summary>
        /// <param name="condition">The evaluated condition.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsFalse(bool condition, string message, params object[] args)
        {
            if (condition)
                _NUnit.ClassicAssert.Fail(string.Format(message, args));
        }

        /// <summary>
        /// Verifies that the object that is passed in is not equal to <c>null</c>. If the object
        /// is <c>null</c> then an <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="anObject">The object that is to be tested.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNotNull(object anObject, string message, params object[] args)
        {
            _NUnit.ClassicAssert.IsNotNull(anObject, message, args);
        }

        /// <summary>
        /// Verifies that the object that is passed in is not equal to <c>null</c>. If the object
        /// is <c>null</c> then an <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="anObject">The object that is to be tested.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNotNull(object anObject)
        {
            _NUnit.ClassicAssert.IsNotNull(anObject);
        }

        /// <summary>
        /// Verifies that the object that is passed in is equal to <c>null</c>. If the object is
        /// not <c>null</c> then an <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="anObject">The object that is to be tested.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNull(object anObject)
        {
            _NUnit.ClassicAssert.IsNull(anObject);
        }

        /// <summary>
        /// Verifies that the object that is passed in is equal to <c>null</c>. If the object is
        /// not <c>null</c> then an <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="anObject">The object that is to be tested.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNull(object anObject, string message, params object[] args)
        {
            _NUnit.ClassicAssert.IsNull(anObject, message, args);
        }

        /// <summary>
        /// Asserts that a condition is <c>true</c>. If the condition is <c>false</c> the method throws
        /// an <see cref="AssertionException"/>.
        /// </summary>
        /// <param name="condition">The evaluated condition.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsTrue(bool condition, string message, params object[] args)
        {
            if (!condition)
                _NUnit.ClassicAssert.Fail(string.Format(message, args));
        }

        /// <summary>
        /// Asserts that a condition is <c>true</c>. If the condition is <c>false</c> the method throws
        /// an <see cref="AssertionException"/>.
        /// </summary>
        /// <param name="condition">The evaluated condition.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsTrue(bool condition)
        {
            if (!condition)
                _NUnit.ClassicAssert.Fail("Expected: True  Actual: False");
        }

        /// <summary>
        /// Verifies that the object that is passed in is not equal to <c>null</c>. If the object
        /// is <c>null</c> then an <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="anObject">The object that is to be tested.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull(object anObject)
        {
            if (anObject is null)
                // ReSharper disable once ExpressionIsAlwaysNull
                _NUnit.ClassicAssert.NotNull(anObject);
        }

        /// <summary>
        /// Verifies that the object that is passed in is not equal to <c>null</c>. If the object
        /// is <c>null</c> then an <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="anObject">The object that is to be tested.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull(object anObject, string message, params object[] args)
        {
            if (anObject is null)
                // ReSharper disable once ExpressionIsAlwaysNull
                _NUnit.ClassicAssert.NotNull(anObject, message, args);
        }

        /// <summary>
        /// Verifies that the object that is passed in is equal to <c>null</c>. If the object is
        /// not <c>null</c> then an <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="anObject">The object that is to be tested.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Null(object anObject, string message, params object[] args)
        {
            if (anObject is not null)
                _NUnit.ClassicAssert.Null(anObject, message, args);
        }

        /// <summary>
        /// Verifies that the object that is passed in is equal to <c>null</c>. If the object is
        /// not <c>null</c> then an <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="anObject">The object that is to be tested.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Null(object anObject)
        {
            if (anObject is not null)
                _NUnit.ClassicAssert.Null(anObject);
        }

        /// <summary>
        /// Asserts that a condition is <c>true</c>. If the condition is <c>false</c> the method throws
        /// an <see cref="AssertionException"/>.
        /// </summary>
        /// <param name="condition">The evaluated condition.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void True(bool condition, string message, params object[] args)
        {
            if (!condition)
                _NUnit.ClassicAssert.Fail(string.Format(message, args));
        }

        /// <summary>
        /// Asserts that a condition is <c>true</c>. If the condition is <c>false</c> the method throws
        /// an <see cref="AssertionException"/>.
        /// </summary>
        /// <param name="condition">The evaluated condition.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void True(bool condition)
        {
            if (!condition)
                _NUnit.ClassicAssert.Fail("Expected: True  Actual: False");
        }


        /// <summary>
        /// Verifies that the string that is passed in is not empty. If the string is empty
        /// then an <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="aString">The string that is to be tested.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNotEmpty(string aString, string message, params object[] args)
        {
            if (string.Empty.Equals(aString))
                _NUnit.ClassicAssert.IsNotEmpty(aString, message, args);
        }

        /// <summary>
        /// Verifies that the string that is passed in is not empty. If the string is empty
        /// then an <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="aString">The string that is to be tested.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNotEmpty(string aString)
        {
            if (string.Empty.Equals(aString))
                _NUnit.ClassicAssert.IsNotEmpty(aString);
        }


        /// <summary>
        /// Verifies that the string that is passed in is empty. If the string is not empty
        /// then an <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="aString">The string that is to be tested.</param>
        /// <param name="message">The message to display in case of failure.</param>
        /// <param name="args">Array of objects to be used in formatting the message.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsEmpty(string aString, string message, params object[] args)
        {
            if (!string.Empty.Equals(aString))
                _NUnit.ClassicAssert.IsEmpty(aString, message, args);
        }

        /// <summary>
        /// Verifies that the string that is passed in is empty. If the string is not empty
        /// then an <see cref="AssertionException"/> is thrown.
        /// </summary>
        /// <param name="aString">The string that is to be tested.</param>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsEmpty(string aString)
        {
            if (!string.Empty.Equals(aString))
                _NUnit.ClassicAssert.IsEmpty(aString);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LessOrEqual(int arg1, int arg2)
        {
            if (arg1 > arg2)
                _NUnit.ClassicAssert.LessOrEqual(arg1, arg2);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Greater(int arg1, int arg2)
        {
            if (arg1 <= arg2)
                _NUnit.ClassicAssert.Greater(arg1, arg2);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DoesNotThrow(Action action, string message, params object[] args)
        {
            _NUnit.ClassicAssert.DoesNotThrow(() => action(), message, args);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DoesNotThrow(Action action)
        {
            _NUnit.ClassicAssert.DoesNotThrow(() => action());
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
            return _NUnit.ClassicAssert.Throws(expectedExceptionType, () => action());
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Throws(Type expectedExceptionType, Action action, string message, params object[] args)
        {
            return _NUnit.ClassicAssert.Throws(expectedExceptionType, () => action(), message, args);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Throws<T>(string expectedParamName, Action action)
            where T : ArgumentException
        {
            T exception = _NUnit.ClassicAssert.Throws<T>(() => action());

            if (exception is null)
            {
                throw new AssertionException("Cannot validate exception ParamName because the exception is null (possible inside a multiple assert block)");
            }

            _NUnit.ClassicAssert.AreEqual(expectedParamName, exception.ParamName);

            return exception;
        }

        [DebuggerStepThrough]
        public static Exception ThrowsFileAlreadyExistsException(string filePath, Action action)
        {
            const string messagePrefix = $"Expected: IOException indicating file not found\nBut was:";
            try
            {
                action();
                throw new AssertionException($"{messagePrefix} <null>");
            }
            catch (Exception ex) when (!FileSupport.IsFileAlreadyExistsException(ex, filePath))
            {
                throw new AssertionException($"{messagePrefix} {ex.GetType().FullName}", ex);
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
            return ThrowsAnyOf(new[] { typeof(TException1), typeof(TException2) }, action);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception ThrowsAnyOf<TException1, TException2, TException3>(Action action)
        {
            return ThrowsAnyOf(new[] { typeof(TException1), typeof(TException2), typeof(TException3) }, action);
        }

        [DebuggerStepThrough]
        public static Exception ThrowsAnyOf(IEnumerable<Type> expectedExceptionTypes, Action action)
        {
            Exception exception = null;
            try
            {
                action();
            }
            // ReSharper disable once PossibleMultipleEnumeration
            catch (Exception ex) when (!expectedExceptionTypes.Contains(ex.GetType()))
            {
                exception = ex;
            }
            catch (Exception ex)
            {
                return ex; // Success
            }
            string exString = exception is null ? "<null>" : exception.GetType().FullName;
            // ReSharper disable once PossibleMultipleEnumeration
            throw new AssertionException($"Expected one of: {Collections.ToString(expectedExceptionTypes.Select(ex => ex.FullName).ToArray())}\nBut was: {exString}");
        }
    }
}
