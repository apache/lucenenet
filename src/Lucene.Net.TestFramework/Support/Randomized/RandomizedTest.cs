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


#if TESTFRAMEWORK_MSTEST
using Microsoft.VisualStudio.TestTools.UnitTesting;
#elif TESTFRAMEWORK_NUNIT
using NUnit.Framework;
#elif TESTFRAMEWORK_XUNIT
using Lucene.Net.TestFramework;
#endif

namespace Lucene.Net.Randomized
{
    public class RandomizedTest
    {
        public static void AssumeTrue(string msg, bool value)
        {
#if TESTFRAMEWORK_MSTEST
            if (!value)
                Assert.Inconclusive(msg);
#elif TESTFRAMEWORK_NUNIT
            Assume.That(value, msg);
#elif TESTFRAMEWORK_XUNIT
            if (!value)
                throw new SkipTestException(msg);
#endif
        }

        public static void AssumeFalse(string msg, bool value)
        {
#if TESTFRAMEWORK_MSTEST
            if (value)
                Assert.Inconclusive(msg);
#elif TESTFRAMEWORK_NUNIT
            Assume.That(!value, msg);
#elif TESTFRAMEWORK_XUNIT
            if (value)
                throw new SkipTestException(msg);
#endif
        }
    }
}

