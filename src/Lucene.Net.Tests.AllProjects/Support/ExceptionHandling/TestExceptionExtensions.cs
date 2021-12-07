using J2N.Text;
using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Support.ExceptionHandling
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

#pragma warning disable IDE0001 // Name can be simplified
    [LuceneNetSpecific]
    public class TestExceptionExtensions : ExceptionScanningTestCase
    {
        public static IEnumerable<TestCaseData> ThrowableTypeExpressions
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                         // exception type (to make NUnit display them all)
                        !KnownThrowableExceptionTypes.Contains(exceptionType), // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));      // throw exception expression
                }
            }
        }


        public static IEnumerable<TestCaseData> ErrorTypeExpressions
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                      // exception type (to make NUnit display them all)
                        !KnownErrorExceptionTypes.Contains(exceptionType),  // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));   // throw exception expression
                }
            }
        }

        public static IEnumerable<TestCaseData> ExceptionTypeExpressions
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                      // exception type (to make NUnit display them all)
                        !KnownExceptionTypes.Contains(exceptionType),       // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));   // throw exception expression
                }
            }
        }

        public static IEnumerable<TestCaseData> RuntimeExceptionTypeExpressions
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
#if NETCOREAPP2_1
                    // These don't seem to match on .NET Core 2.1, but we don't care
                    if (exceptionType.FullName.Equals("System.CrossAppDomainMarshaledException") ||
                        exceptionType.FullName.Equals("System.AppDomainUnloadedException"))
                    {
                        continue;
                    }
#endif

                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                      // exception type (to make NUnit display them all)
                        !KnownRuntimeExceptionTypes.Contains(exceptionType),// expectedToThrow
                        new Action(() => ThrowException(exceptionType)));   // throw exception expression
                }
            }
        }

        public static IEnumerable<TestCaseData> IOExceptionTypeExpressions
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                      // exception type (to make NUnit display them all)
                        !KnownIOExceptionTypes.Contains(exceptionType),     // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));   // throw exception expression
                }
            }
        }

        public static IEnumerable<TestCaseData> AssertionErrorTypeExpressions
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                         // exception type (to make NUnit display them all)
                        !KnownAssertionErrorTypes.Contains(exceptionType),     // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));      // throw exception expression
                }
            }
        }

        public static IEnumerable<TestCaseData> IndexOutOfBoundsExceptionTypeExpressions
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                                    // exception type (to make NUnit display them all)
                        !KnownIndexOutOfBoundsExceptionTypes.Contains(exceptionType),     // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));                 // throw exception expression
                }
            }
        }

        public static IEnumerable<TestCaseData> NullPointerExceptionTypeExpressions
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                                    // exception type (to make NUnit display them all)
                        !KnownNullPointerExceptionTypes.Contains(exceptionType),          // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));                 // throw exception expression
                }
            }
        }

        public static IEnumerable<TestCaseData> IllegalArgumentExceptionTypeExpressions
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                                    // exception type (to make NUnit display them all)
                        !KnownIllegalArgumentExceptionTypes.Contains(exceptionType),      // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));                 // throw exception expression
                }
            }
        }

        public static IEnumerable<TestCaseData> IllegalArgumentExceptionTypeExpressions_TestEnvironment
        {
            get
            {
                foreach (var exceptionType in AllExceptionTypes)
                {
                    // expectedToThrow is true when we expect the error to be thrown and false when we expect it to be caught
                    yield return new TestCaseData(
                        exceptionType,                                                               // exception type (to make NUnit display them all)
                        !KnownIllegalArgumentExceptionTypes_TestEnvironment.Contains(exceptionType), // expectedToThrow
                        new Action(() => ThrowException(exceptionType)));                            // throw exception expression
                }
            }
        }

        private static void ThrowException(Type exceptionType)
        {
            throw TryInstantiate(exceptionType);
        }

        [Test]
        [TestCaseSource("ThrowableTypeExpressions")]
        public void TestIsThrowable(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            static bool extensionMethod(Exception e) => e.IsThrowable();

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        [Test]
        [TestCaseSource("ErrorTypeExpressions")]
        public void TestIsError(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            static bool extensionMethod(Exception e) => e.IsError();

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        // This test ensures that all known Error types from Java are not caught by
        // our IsException() handler.
        [Test]
        [TestCaseSource("ExceptionTypeExpressions")]
        public void TestIsException(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            static bool extensionMethod(Exception e) => e.IsException();

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        // This test ensures that all known Error types from Java are not caught by
        // our IsRuntimeException() handler.
        [Test]
        [TestCaseSource("RuntimeExceptionTypeExpressions")]
        public void TestIsRuntimeException(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            static bool extensionMethod(Exception e) => e.IsRuntimeException();

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        [Test]
        [TestCaseSource("IOExceptionTypeExpressions")]
        public void TestIsIOException(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            static bool extensionMethod(Exception e) => e.IsIOException();

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        // This test ensures that Lucene.NET's AssertionException, the .NET platform's DebugAssertException, and
        // NUnit's AssertionException and MultipleAssertException types are all treated as if they were AssertionError
        // in Java.
        [Test]
        [TestCaseSource("AssertionErrorTypeExpressions")]
        public void TestIsAssertionError(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            static bool extensionMethod(Exception e) => e.IsAssertionError();

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        // This test ensures that ArgumentOutOfRangeException and IndexOutOfRangeException are both caught by our
        // IndexOutOfBoundsException handler, because they both correspond to IndexOutOfBoundsException in Java.
        // Java has 2 other types ArrayIndexOutOfBoundsException and StringIndexOutOfBoundsException, whose alias
        // exception types are also part of the test.
        [Test]
        [TestCaseSource("IndexOutOfBoundsExceptionTypeExpressions")]
        public void TestIsIndexOutOfBoundsException(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            static bool extensionMethod(Exception e) => e.IsIndexOutOfBoundsException();

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        // This test ensures that ArgumentNullException and NullReferenceException are both caught by our
        // NullPointerException handler, because they both correspond to NullPointerException in Java
        [Test]
        [TestCaseSource("NullPointerExceptionTypeExpressions")]
        public void TestIsNullPointerException(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            static bool extensionMethod(Exception e) => e.IsNullPointerException();

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        // This test ensures that any known ArgumentException will be caught.
        // We do it this way in production to ensure that if we "upgrade" to a .NET
        // ArgumentNullException or ArgumentOutOfRangeException it won't break the code.
        [Test]
        [TestCaseSource("IllegalArgumentExceptionTypeExpressions")]
        public void TestIsIllegalArgumentException(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            // Make sure we are testing the production code
            static bool extensionMethod(Exception e) => Lucene.ExceptionExtensions.IsIllegalArgumentException(e);

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        // This test ensures that ArgumentNullException and ArgumentOutOfRangeException are not caught by our
        // IllegalArgumentException handler in tests, because they wouldn't be in Java. We do this differently
        // in the test environment to ensure that if a test is specified wrong it will fail and should be updated
        // and commented to indicate we diverged from Lucene.
        [Test]
        [TestCaseSource("IllegalArgumentExceptionTypeExpressions_TestEnvironment")]
        public void TestIsIllegalArgumentException_TestEnvironment(Type exceptionType, bool expectedToThrow, Action expression) // LUCENENET NOTE: exceptionType is only here to make NUnit display them all
        {
            // Make sure we are testing the test environment code
            static bool extensionMethod(Exception e) => Lucene.Net.ExceptionExtensions.IsIllegalArgumentException(e);

            if (expectedToThrow)
            {
                AssertDoesNotCatch(expression, extensionMethod);
            }
            else
            {
                AssertCatches(expression, extensionMethod);
            }
        }

        private void AssertCatches(Action action, Func<Exception, bool> extensionMethodExpression)
        {
            try
            {
                try
                {
                    action();
                }
                catch (Exception e) when (extensionMethodExpression(e))
                {
                    // expected
                    Assert.Pass($"Expected: Caught exception {e.GetType().FullName}");
                }
            }
            catch (Exception e) when (!(e is NUnit.Framework.SuccessException))
            {
                // not expected
                Assert.Fail($"Exception thrown when expected to be caught: {e.GetType().FullName}");
            }
        }

        private void AssertDoesNotCatch(Action action, Func<Exception, bool> extensionMethodExpression)
        {
            try
            {
                try
                {
                    action();
                }
                catch (NUnit.Framework.AssertionException e)
                {
                    // Special case - need to suppress this from being thrown to the outer catch
                    // or we will get a false failure
                    Assert.IsFalse(extensionMethodExpression(e));
                    Assert.Pass($"Expected: Did not catch exception {e.GetType().FullName}");
                }
                catch (Exception e) when (extensionMethodExpression(e))
                {
                    // not expected
                    Assert.Fail($"Exception caught when expected to be thrown: {e.GetType().FullName}");
                }
            }
            catch (Exception e) when (!(e is NUnit.Framework.AssertionException))
            {
                // expected
                Assert.Pass($"Expected: Did not catch exception {e.GetType().FullName}");
            }
        }
    }
}
