using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
#nullable enable

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
    /// Extensions to <see cref="Test"/>.
    /// </summary>
    internal static class TestExtensions
    {
        /// <summary>
        /// Retrieves the <see cref="RandomizedContext"/> instance for a given <see cref="Test"/>.
        /// <para/>
        /// 
        /// </summary>
        /// <param name="test"></param>
        /// <returns>The <see cref="RandomizedContext"/>. If the <paramref name="test"/> is <c>null</c>
        /// or doesn't have a <see cref="RandomizedContext"/>
        /// (it doesn't belong to our framework), then the return value is <c>null</c>.</returns>
        public static RandomizedContext? GetRandomizedContext(this Test? test)
        {
            if (test is null)
                return null;

            if (test.Properties.ContainsKey(RandomizedContext.RandomizedContextPropertyName))
                return (RandomizedContext?)test.Properties.Get(RandomizedContext.RandomizedContextPropertyName);

            return null;
        }

        /// <summary>
        /// Determines if the current <paramref name="test"/> is a
        /// type of runnable test (for example, a [Test] or [TestCase()] as
        /// opposed to a class or other container).
        /// </summary>
        /// <param name="test">This <see cref="Test"/>.</param>
        /// <returns><c>true</c> if the current <paramref name="test"/> is a
        /// type of runnable test; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="test"/> is <c>null</c>.</exception>
        public static bool IsTest(this ITest test)
        {
            if (test is null)
                throw new ArgumentNullException(nameof(test));
            if (test is Test t)
                return !t.IsSuite;
            return false;
        }

        /// <summary>
        /// Determines if the current <paramref name="test"/> is a
        /// test class (as opposed to a runnable test).
        /// </summary>
        /// <param name="test">This <see cref="Test"/>.</param>
        /// <returns><c>true</c> if the current <paramref name="test"/> is a
        /// test class; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsTestClass(this ITest test)
        {
            if (test is null)
                throw new ArgumentNullException(nameof(test));
            if (test is Test t)
                return t.IsSuite && t.TypeInfo is not null;
            return false;
        }

        /// <summary>
        /// Mark the test and all descendents as Invalid (not runnable) specifying a reason and an exception.
        /// </summary>
        /// <param name="test">This <see cref="Test"/>.</param>
        /// <param name="reason">The reason the test is not runnable</param>
        /// <exception cref="ArgumentNullException"><paramref name="test"/> or <paramref name="reason"/> is <c>null</c>.</exception>
        public static void MakeAllInvalid(this Test test, string reason)
            => MakeAllInvalidInternal(test, null, reason);

        /// <summary>
        /// Mark the test and all descendents as Invalid (not runnable) specifying a reason and an exception.
        /// </summary>
        /// <param name="test">This <see cref="Test"/>.</param>
        /// <param name="exception">The exception that was the cause.</param>
        /// <param name="reason">The reason the test is not runnable</param>
        /// <exception cref="ArgumentNullException"><paramref name="test"/>, <paramref name="exception"/> or <paramref name="reason"/> is <c>null</c>.</exception>
        public static void MakeAllInvalid(this Test test, Exception exception, string reason)
        {
            if (exception is null)
                throw new ArgumentNullException(nameof(exception));
            MakeAllInvalidInternal(test, exception, reason);
        }

        private static void MakeAllInvalidInternal(this Test test, Exception? exception, string reason)
        {
            if (test is null)
                throw new ArgumentNullException(nameof(test));
            if (reason is null)
                throw new ArgumentNullException(nameof(reason));

            if (exception is null)
                test.MakeInvalid(reason);
            else
                test.MakeInvalid(exception, reason);

            if (test.HasChildren)
            {
                var stack = new Stack<Test>(test.Tests.OfType<Test>());

                while (stack.Count > 0)
                {
                    var currentTest = stack.Pop();
                    if (exception is null)
                        currentTest.MakeInvalid(reason);
                    else
                        currentTest.MakeInvalid(exception, reason);

                    // Add children to the stack if they exist
                    if (currentTest.HasChildren)
                    {
                        foreach (var child in currentTest.Tests.OfType<Test>())
                        {
                            stack.Push(child);
                        }
                    }
                }
            }
        }
    }
}
