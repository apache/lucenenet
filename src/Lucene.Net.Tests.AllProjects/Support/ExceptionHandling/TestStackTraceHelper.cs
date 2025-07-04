using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System.IO;

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

    [TestFixture, LuceneNetSpecific]
    public class TestStackTraceHelper : LuceneTestCase
    {
        [Test]
        [TestCase(true, nameof(TestDoesStackTraceContainMethod))]
        [TestCase(false, nameof(GetHashCode))]
        public void TestDoesStackTraceContainMethod(bool expected, string methodName)
        {
            Assert.AreEqual(expected, StackTraceHelper.DoesStackTraceContainMethod(methodName));
        }

        [Test]
        [TestCase(true, nameof(TestStackTraceHelper), nameof(TestDoesStackTraceContainMethodWithClassName))]
        [TestCase(false, nameof(TestStackTraceHelper), nameof(GetHashCode))]
        [TestCase(false, nameof(LuceneTestCase), nameof(TestDoesStackTraceContainMethodWithClassName))]
        public void TestDoesStackTraceContainMethodWithClassName(bool expected, string className, string methodName)
        {
            Assert.AreEqual(expected, StackTraceHelper.DoesStackTraceContainMethod(className, methodName));
        }

        [Test]
        public void TestPrintCurrentStackTrace()
        {
            using var sw = new StringWriter();
            StackTraceHelper.PrintCurrentStackTrace(sw);

            string str = sw.ToString();

            using var sr = new StringReader(str);
            string topLine = sr.ReadLine();
            Assert.NotNull(topLine);
            Assert.IsTrue(topLine.Contains(nameof(TestPrintCurrentStackTrace)));
        }
    }
}
