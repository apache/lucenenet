using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
    /// Facade for xUnit Assertions
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
            Xunit.Assert.Equal(expected, actual);
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
            Xunit.Assert.True(object.Equals(expected, actual), FormatMessage(message, args));
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
            if (double.IsNaN(expected) || double.IsInfinity(expected))
                Xunit.Assert.True(expected.Equals(actual), string.Format(message, args));
            else
                Xunit.Assert.True(expected - actual <= delta || actual - expected <= delta, FormatMessage(message, args));
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
            if (double.IsNaN(expected) || double.IsInfinity(expected))
                Xunit.Assert.True(expected.Equals(actual));
            else
                Xunit.Assert.True(expected - actual <= delta || actual - expected <= delta);
        }

        // From CollectionAssert
        public static void AreEqual<T>(T[] expected, T[] actual)
        {
            Xunit.Assert.Equal(expected, actual);
        }

        // From CollectionAssert
        public static void AreEqual<T>(T[] expected, T[] actual, string message, params object[] args)
        {
            Xunit.Assert.True(System.Array.Equals(expected, actual), FormatMessage(message, args));
        }

        public static void AreEqual<T, S>(IDictionary<T, S> expected, IDictionary<T, S> actual)
        {
            AreEqual(expected.Count, actual.Count);
            foreach (var key in expected.Keys)
            {
                AreEqual(expected[key], actual[key]);
            }
        }

        // From CollectionAssert
        public static void AreEqual(ICollection expected, ICollection actual)
        {
            Xunit.Assert.Equal(expected, actual);
        }

        // From CollectionAssert
        public static void AreEqual(ICollection expected, ICollection actual, string message, params object[] args)
        {
            Xunit.Assert.True(Collections.Equals(expected, actual), FormatMessage(message, args));
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
            Xunit.Assert.True(!Collections.Equals(expected, actual), FormatMessage(message, args));
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
            Xunit.Assert.NotEqual(expected, actual);
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
            Xunit.Assert.True(!object.ReferenceEquals(expected, actual), FormatMessage(message, args));
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
            Xunit.Assert.NotSame(expected, actual);
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
            Xunit.Assert.Same(expected, actual);
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
            Xunit.Assert.True(object.ReferenceEquals(expected, actual), FormatMessage(message, args));
        }

        public static void Fail(string message, params object[] args)
        {
            Xunit.Assert.True(false, FormatMessage(message, args));
        }
        //
        // Summary:
        //     Throws an NUnit.Framework.AssertionException. This is used by the other Assert
        //     functions.
        public static void Fail()
        {
            Xunit.Assert.True(false);
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
            Xunit.Assert.True(false, message);
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
            Xunit.Assert.False(condition, FormatMessage(message, args));
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
            Xunit.Assert.False(condition);
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
            Xunit.Assert.False(condition);
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
            Xunit.Assert.False(condition, FormatMessage(message, args));
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
            Xunit.Assert.True(anObject != null, FormatMessage(message, args));
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
            Xunit.Assert.NotNull(anObject);
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
            Xunit.Assert.Null(anObject);
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
            Xunit.Assert.True(anObject == null, FormatMessage(message, args));
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
            Xunit.Assert.True(condition, FormatMessage(message, args));
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
            Xunit.Assert.True(condition);
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
            Xunit.Assert.NotNull(anObject);
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
            Xunit.Assert.True(anObject != null, FormatMessage(message, args));
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
            Xunit.Assert.True(anObject == null, FormatMessage(message, args));
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
            Xunit.Assert.Null(anObject);
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
            Xunit.Assert.True(condition, FormatMessage(message, args));
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
            Xunit.Assert.True(condition);
        }


        public static Exception Throws<TException>(Action action)
        {
            return Throws(typeof(TException), action);
        }

        public static Exception Throws(Type expectedExceptionType, Action action)
        {
            return Xunit.Assert.Throws(expectedExceptionType, () => action());
        }

        public static Exception ThrowsFileAlreadyExistsException(string filePath, Action action)
        {
            var messagePrefix = $"Expected: IOException indicating file not found\nBut was:";
            Exception exception = null;
            try
            {
                action();
            }
            catch (Exception ex) when (!IsFileAlreadyExistsException(ex, filePath))
            {
                exception = ex;
            }
            catch (Exception ex)
            {
                return ex; // Success
            }
            string exString = exception == null ? "<null>" : exception.GetType().FullName;
            Xunit.Assert.False(true, $"{messagePrefix} {exString}");
            return null;
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
            Xunit.Assert.False(true, $"{messagePrefix} {exString}");
            return null;
        }


        private static string FormatMessage(string message, object[] args)
        {
            if (args?.Length > 0)
                return string.Format(message, args);
            else
                return message;
        }
    }
}
