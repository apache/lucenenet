using Lucene.Net.Support;
using Lucene.Net.Support.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MSTest = Microsoft.VisualStudio.TestTools.UnitTesting;

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
            MSTest.Assert.AreEqual(expected, actual);
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
            MSTest.Assert.AreEqual(expected, actual, message, args);
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
            MSTest.Assert.AreEqual(expected, actual, delta, message, args);
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
            MSTest.Assert.AreEqual(expected, actual, delta);
        }

        // From CollectionAssert
        public static void AreEqual<T>(T[] expected, T[] actual)
        {
            MSTest.CollectionAssert.AreEqual(expected, actual);
        }

        // From CollectionAssert
        public static void AreEqual<T>(T[] expected, T[] actual, string message, params object[] args)
        {
            MSTest.CollectionAssert.AreEqual(expected, actual, message, args);
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
            MSTest.CollectionAssert.AreEqual(expected, actual);
        }

        // From CollectionAssert
        public static void AreEqual(ICollection expected, ICollection actual, string message, params object[] args)
        {
            MSTest.CollectionAssert.AreEqual(expected, actual, message, args);
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
            MSTest.Assert.AreNotEqual(expected, actual, message, args);
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
            MSTest.Assert.AreNotEqual(expected, actual);
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
            MSTest.Assert.AreNotSame(expected, actual, message, args);
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
            MSTest.Assert.AreNotSame(expected, actual);
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
            MSTest.Assert.AreSame(expected, actual);
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
            MSTest.Assert.AreSame(expected, actual, message, args);
        }
 
        public static void Fail(string message, params object[] args)
        {
            MSTest.Assert.Fail(message, args);
        }
        //
        // Summary:
        //     Throws an NUnit.Framework.AssertionException. This is used by the other Assert
        //     functions.
        public static void Fail()
        {
            MSTest.Assert.Fail();
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
            MSTest.Assert.Fail(message);
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
            MSTest.Assert.IsFalse(condition, message, args);
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
            MSTest.Assert.IsFalse(condition);
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
            MSTest.Assert.IsFalse(condition);
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
            MSTest.Assert.IsFalse(condition, message, args);
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
            MSTest.Assert.IsNotNull(anObject, message, args);
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
            MSTest.Assert.IsNotNull(anObject);
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
            MSTest.Assert.IsNull(anObject);
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
            MSTest.Assert.IsNull(anObject, message, args);
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
            MSTest.Assert.IsTrue(condition, message, args);
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
            MSTest.Assert.IsTrue(condition);
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
            MSTest.Assert.IsNotNull(anObject);
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
            MSTest.Assert.IsNotNull(anObject, message, args);
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
            MSTest.Assert.IsNull(anObject, message, args);
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
            MSTest.Assert.IsNull(anObject);
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
            MSTest.Assert.IsTrue(condition, message, args);
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
            MSTest.Assert.IsTrue(condition);
        }


        public static Exception Throws<TException>(Action action)
        {
            return Throws(typeof(TException), action);
        }

        public static Exception Throws(Type expectedExceptionType, Action action)
        {
            if (expectedExceptionType == null)
                throw new ArgumentNullException(nameof(expectedExceptionType));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var messagePrefix = $"Expected: {expectedExceptionType.GetType().FullName}\nBut was:";
            Exception exception = null;
            try
            {
                action();
            }
            catch (Exception ex) when (!ex.GetType().Equals(expectedExceptionType))
            {
                exception = ex;
            }
            catch (Exception ex)
            {
                return ex; // Success
            }
            string exString = exception == null ? "<null>" : exception.GetType().FullName;
            MSTest.Assert.Fail($"{messagePrefix} {exString}");
            return null;
        }

        public static Exception ThrowsFileAlreadyExistsException(string filePath, Action action)
        {
            var messagePrefix = $"Expected: IOException indicating file not found\nBut was:";
            Exception exception = null;
            try
            {
                action();
            }
            catch (Exception ex) when (!FileSupport.IsFileAlreadyExistsException(ex, filePath))
            {
                exception = ex;
            }
            catch (Exception ex)
            {
                return ex; // Success
            }
            string exString = exception == null ? "<null>" : exception.GetType().FullName;
            MSTest.Assert.Fail($"{messagePrefix} {exString}");
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
            try
            {
                action();
                throw new MSTest.AssertFailedException($"{messagePrefix} <null>");
            }
            catch (Exception ex) when (!expectedExceptionTypes.Contains(ex.GetType()))
            {
                throw new MSTest.AssertFailedException($"{messagePrefix} {ex.GetType().FullName}", ex);
            }
            catch (Exception ex)
            {
                return ex; // Success
            }
        }
    }
}
