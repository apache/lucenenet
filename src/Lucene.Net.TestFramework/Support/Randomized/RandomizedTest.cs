using System;

#if TESTFRAMEWORK_MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AssumptionViolatedException = Microsoft.VisualStudio.TestTools.UnitTesting.AssertInconclusiveException;
#elif TESTFRAMEWORK_NUNIT
using NUnit.Framework;
using AssumptionViolatedException = NUnit.Framework.InconclusiveException;
#elif TESTFRAMEWORK_XUNIT
using Lucene.Net.TestFramework;
using AssumptionViolatedException = Lucene.Net.TestFramework.SkipTestException;
#endif

namespace Lucene.Net.Randomized
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
    /// Common scaffolding for subclassing randomized tests.
    /// </summary>
    public static class RandomizedTest // LUCENENET: Made static because all members are static
    {
        /// <param name="msg">Message to be included in the exception's string.</param>
        /// <param name="condition">
        /// If <c>false</c> an <see cref="AssumptionViolatedException"/> is
        /// thrown by this method and the test case (should be) ignored (or
        /// rather technically, flagged as a failure not passing a certain
        /// assumption). Tests that are assumption-failures do not break
        /// builds (again: typically).
        /// </param>
        public static void AssumeTrue(string msg, bool condition)
        {
#if TESTFRAMEWORK_MSTEST
            if (!condition)
                Assert.Inconclusive(msg);
#elif TESTFRAMEWORK_NUNIT
            Assume.That(condition, msg);
#elif TESTFRAMEWORK_XUNIT
            if (!condition)
                throw new SkipTestException(msg);
#endif
        }

        /// <param name="msg">Message to be included in the exception's string.</param>
        /// <param name="condition">
        /// If <c>true</c> an <see cref="AssumptionViolatedException"/> is
        /// thrown by this method and the test case (should be) ignored (or
        /// rather technically, flagged as a failure not passing a certain
        /// assumption). Tests that are assumption-failures do not break
        /// builds (again: typically).
        /// </param>
        public static void AssumeFalse(string msg, bool condition)
        {
#if TESTFRAMEWORK_MSTEST
            if (condition)
                Assert.Inconclusive(msg);
#elif TESTFRAMEWORK_NUNIT
            Assume.That(!condition, msg);
#elif TESTFRAMEWORK_XUNIT
            if (condition)
                throw new SkipTestException(msg);
#endif
        }

        /// <summary>
        /// Assume <paramref name="t"/> is <c>null</c>.
        /// </summary>
        public static void AssumeNoException(string msg, Exception t)
        {
            if (t != null)
            {
                // This does chain the exception as the cause.
                throw new AssumptionViolatedException(msg, t);
            }
        }
    }
}

