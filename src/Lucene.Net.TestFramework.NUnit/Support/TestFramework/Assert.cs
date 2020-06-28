using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using J2N.Text;
using NUnit.Framework.Constraints;
using JCG = J2N.Collections.Generic;
using _NUnit = NUnit.Framework;

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
    /// Facade for MSTest Assertions
    /// </summary>
    internal partial class Assert
    {
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
        public static void AreEqual(object expected, object actual)
        {
            _NUnit.Assert.AreEqual(expected, actual);
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
        public static void AreEqual(object expected, object actual, string message, params object[] args)
        {
            _NUnit.Assert.AreEqual(expected, actual, message, args);
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
        public static void AreEqual(bool expected, bool actual)
        {
            _NUnit.Assert.IsTrue(expected.Equals(actual));
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
        public static void AreEqual(bool expected, bool actual, string message, params object[] args)
        {
            _NUnit.Assert.IsTrue(expected.Equals(actual), message, args);
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
        public static void AreEqual(double expected, double actual, double delta, string message, params object[] args)
        {
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
        public static void AreEqual(double expected, double actual, double delta)
        {
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
        public static void AreEqual(float expected, float actual, float delta, string message, params object[] args)
        {
            if (Math.Abs(expected - actual) > delta)
            {
                _NUnit.Assert.AreEqual(expected, actual, delta, message, args);
            }
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
        public static void AreEqual(float expected, float actual, float delta)
        {
            if (Math.Abs(expected - actual) > delta)
            {
                _NUnit.Assert.AreEqual(expected, actual, delta);
            }
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
        public static void AreEqual(int expected, int actual)
        {
            _NUnit.Assert.True(expected.Equals(actual));
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
        public static void AreEqual(int expected, int actual, string message)
        {
            _NUnit.Assert.True(expected.Equals(actual), message);
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
        public static void AreEqual(long expected, long actual)
        {
            _NUnit.Assert.True(expected.Equals(actual));
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
        public static void AreEqual(long expected, long actual, string message)
        {
            _NUnit.Assert.True(expected.Equals(actual), message);
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
        public static void AreEqual(byte expected, byte actual)
        {
            _NUnit.Assert.True(expected.Equals(actual));
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
        public static void AreEqual(byte expected, byte actual, string message)
        {
            _NUnit.Assert.True(expected.Equals(actual), message);
        }


        private static JCG.SetEqualityComparer<T> GetSetComparer<T>(bool aggressive)
        {
            return aggressive
                ? JCG.SetEqualityComparer<T>.Aggressive
                : JCG.SetEqualityComparer<T>.Default;
        }

        private static JCG.ListEqualityComparer<T> GetListComparer<T>(bool aggressive)
        {
            return aggressive
                ? JCG.ListEqualityComparer<T>.Aggressive
                : JCG.ListEqualityComparer<T>.Default;
        }

        private static JCG.DictionaryEqualityComparer<TKey, TValue> GetDictionaryComparer<TKey, TValue>(bool aggressive)
        {
            return aggressive
                ? JCG.DictionaryEqualityComparer<TKey, TValue>.Aggressive
                : JCG.DictionaryEqualityComparer<TKey, TValue>.Default;
        }

        public static string FormatCollection(object collection)
        {
            return string.Format(StringFormatter.CurrentCulture, "{0}", collection);
        }

        public static void AreEqual<T>(ISet<T> expected, ISet<T> actual, bool aggressive = true)
        {
            if (!GetSetComparer<T>(aggressive).Equals(expected, actual))
            {
                _NUnit.Assert.Fail("Expected: '{0}', Actual: '{1}'", FormatCollection(expected), FormatCollection(actual));
            }
        }

        public static void AreEqual<T>(ISet<T> expected, ISet<T> actual, bool aggressive, string message, params object[] args)
        {
            if (!GetSetComparer<T>(aggressive).Equals(expected, actual))
            {
                _NUnit.Assert.Fail(message, args);
            }
        }

        public static void AreEqual<T>(IList<T> expected, IList<T> actual, bool aggressive = true)
        {
            if (!GetListComparer<T>(aggressive).Equals(expected, actual))
            {
                _NUnit.Assert.Fail("Expected: '{0}', Actual: '{1}'", FormatCollection(expected), FormatCollection(actual));
            }

        }

        public static void AreEqual<T>(IList<T> expected, IList<T> actual, bool aggressive, string message, params object[] args)
        {
            if (!GetListComparer<T>(aggressive).Equals(expected, actual))
            {
                _NUnit.Assert.Fail(message, args);
            }
        }

        public static void AreEqual<TKey, TValue>(IDictionary<TKey, TValue> expected, IDictionary<TKey, TValue> actual, bool aggressive = true)
        {
            if (!GetDictionaryComparer<TKey, TValue>(aggressive).Equals(expected, actual))
            {
                _NUnit.Assert.Fail("Expected: '{0}', Actual: '{1}'", FormatCollection(expected), FormatCollection(actual));
            }

        } 

        public static void AreEqual<TKey, TValue>(IDictionary<TKey, TValue> expected, IDictionary<TKey, TValue> actual, bool aggressive, string message, params object[] args)
        {
            if (!GetDictionaryComparer<TKey, TValue>(aggressive).Equals(expected, actual))
            {
                _NUnit.Assert.Fail(message, args);
            }
        }


        // From CollectionAssert
        public static void AreEqual<T>(T[] expected, T[] actual)
        {
            // LUCENENET: Do the initial check with the (fast) J2N array comparison. If it fails,
            // then use CollectionAssert to re-do the check in a slower way and generate the assert message.
            if (!J2N.Collections.ArrayEqualityComparer<T>.OneDimensional.Equals(expected, actual))
            {
                _NUnit.CollectionAssert.AreEqual(expected, actual);
            }
        }

        // From CollectionAssert
        public static void AreEqual<T>(T[] expected, T[] actual, string message, params object[] args)
        {
            // LUCENENET: Do the initial check with the (fast) J2N array comparison. If it fails,
            // then use CollectionAssert to re-do the check in a slower way and generate the assert message.
            if (!J2N.Collections.ArrayEqualityComparer<T>.OneDimensional.Equals(expected, actual))
            {
                _NUnit.CollectionAssert.AreEqual(expected, actual, message, args);
            }
        }

        // From CollectionAssert
        public static void AreEqual(ICollection expected, ICollection actual)
        {
            _NUnit.CollectionAssert.AreEqual(expected, actual);
        }

        // From CollectionAssert
        public static void AreEqual(ICollection expected, ICollection actual, string message, params object[] args)
        {
            _NUnit.CollectionAssert.AreEqual(expected, actual, message, args);
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
        public static void AreSame(object expected, object actual, string message, params object[] args)
        {
            _NUnit.Assert.AreSame(expected, actual, message, args);
        }

        public static void Fail(string message, params object[] args)
        {
            _NUnit.Assert.Fail(message, args);
        }
        //
        // Summary:
        //     Throws an NUnit.Framework.AssertionException. This is used by the other Assert
        //     functions.
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
        public static void Fail(string message)
        {
            _NUnit.Assert.Fail(message);
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
        public static void False(bool condition, string message, params object[] args)
        {
            _NUnit.Assert.False(condition, message, args);
        }
        //
        // Summary:
        //     Asserts that a condition is false. If the condition is true the method throws
        //     an NUnit.Framework.AssertionException.
        //
        // Parameters:
        //   condition:
        //     The evaluated condition
        public static void False(bool condition)
        {
            _NUnit.Assert.That(condition, _NUnit.Is.False);
        }

        //
        // Summary:
        //     Asserts that a condition is false. If the condition is true the method throws
        //     an NUnit.Framework.AssertionException.
        //
        // Parameters:
        //   condition:
        //     The evaluated condition
        public static void IsFalse(bool condition)
        {
            _NUnit.Assert.That(condition, _NUnit.Is.False);
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
        public static void IsFalse(bool condition, string message, params object[] args)
        {
            _NUnit.Assert.That(condition, _NUnit.Is.False, message, args);
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
        public static void IsTrue(bool condition, string message, params object[] args)
        {
            _NUnit.Assert.IsTrue(condition, message, args);
        }

        //
        // Summary:
        //     Asserts that a condition is true. If the condition is false the method throws
        //     an NUnit.Framework.AssertionException.
        //
        // Parameters:
        //   condition:
        //     The evaluated condition
        public static void IsTrue(bool condition)
        {
            _NUnit.Assert.IsTrue(condition);
        }

        //
        // Summary:
        //     Verifies that the object that is passed in is not equal to null If the object
        //     is null then an NUnit.Framework.AssertionException is thrown.
        //
        // Parameters:
        //   anObject:
        //     The object that is to be tested
        public static void NotNull(object anObject)
        {
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
        public static void NotNull(object anObject, string message, params object[] args)
        {
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
        public static void Null(object anObject, string message, params object[] args)
        {
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
        public static void Null(object anObject)
        {
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
        public static void True(bool condition, string message, params object[] args)
        {
            _NUnit.Assert.True(condition, message, args);
        }

        //
        // Summary:
        //     Asserts that a condition is true. If the condition is false the method throws
        //     an NUnit.Framework.AssertionException.
        //
        // Parameters:
        //   condition:
        //     The evaluated condition
        public static void True(bool condition)
        {
            _NUnit.Assert.True(condition);
        }


        public static void LessOrEqual(int arg1, int arg2)
        {
            _NUnit.Assert.LessOrEqual(arg1, arg2);
        }

        public static void Greater(int arg1, int arg2)
        {
            _NUnit.Assert.Greater(arg1, arg2);
        }

        public static Exception Throws<TException>(Action action)
        {
            return Throws(typeof(TException), action);
        }

        public static Exception Throws(Type expectedExceptionType, Action action)
        {
            return _NUnit.Assert.Throws(expectedExceptionType, () => action());
        }

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

        public static Exception ThrowsAnyOf<TException1, TException2>(Action action)
        {
            return ThrowsAnyOf(new Type[] { typeof(TException1), typeof(TException2) }, action);
        }

        public static Exception ThrowsAnyOf<TException1, TException2, TException3>(Action action)
        {
            return ThrowsAnyOf(new Type[] { typeof(TException1), typeof(TException2), typeof(TException3) }, action);
        }

        public static Exception ThrowsAnyOf(IEnumerable<Type> expectedExceptionTypes, Action action)
        {
            var messagePrefix = $"Expected one of: {Collections.ToString(expectedExceptionTypes.Select(ex => ex.FullName).ToArray())}\nBut was:";
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
            string exString = exception == null ? "<null>" : exception.GetType().FullName;
            throw new _NUnit.AssertionException($"{messagePrefix} {exString}");
        }
    }
}
