using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Diagnostics
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
    /// LUCENENET specific tests for <see cref="Debugging"/> class
    /// </summary>
    public class TestDebugging
    {
        [Test, LuceneNetSpecific]
        public void TestConditionTrue()
        {
            TestWithAsserts(enabled: true, () =>
            {
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message"));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message", new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message", new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message", new object(), new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message", new object(), new object(), new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message", new object(), new object(), new object(), new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message", new object(), new object(), new object(), new object(), new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message", new object(), new object(), new object(), new object(), new object(), new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message", new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object()));
            });

            TestWithAsserts(enabled: false, () =>
            {
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message"));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message", new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message", new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message", new object(), new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message", new object(), new object(), new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message", new object(), new object(), new object(), new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message", new object(), new object(), new object(), new object(), new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message", new object(), new object(), new object(), new object(), new object(), new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: true, "test message", new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object()));
            });
        }

        [Test, LuceneNetSpecific]
        public void TestConditionFalse()
        {
            TestWithAsserts(enabled: true, () =>
            {
                Assert.Throws<AssertionException>(() => Debugging.Assert(condition: false));
                Assert.Throws<AssertionException>(() => Debugging.Assert(condition: false, "test message"));
                Assert.Throws<AssertionException>(() => Debugging.Assert(condition: false, "test message", new object()));
                Assert.Throws<AssertionException>(() => Debugging.Assert(condition: false, "test message", new object(), new object()));
                Assert.Throws<AssertionException>(() => Debugging.Assert(condition: false, "test message", new object(), new object(), new object()));
                Assert.Throws<AssertionException>(() => Debugging.Assert(condition: false, "test message", new object(), new object(), new object(), new object()));
                Assert.Throws<AssertionException>(() => Debugging.Assert(condition: false, "test message", new object(), new object(), new object(), new object(), new object()));
                Assert.Throws<AssertionException>(() => Debugging.Assert(condition: false, "test message", new object(), new object(), new object(), new object(), new object(), new object()));
                Assert.Throws<AssertionException>(() => Debugging.Assert(condition: false, "test message", new object(), new object(), new object(), new object(), new object(), new object(), new object()));
                Assert.Throws<AssertionException>(() => Debugging.Assert(condition: false, "test message", new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object()));
            });

            TestWithAsserts(enabled: false, () =>
            {
                Assert.DoesNotThrow(() => Debugging.Assert(condition: false));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: false, "test message"));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: false, "test message", new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: false, "test message", new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: false, "test message", new object(), new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: false, "test message", new object(), new object(), new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: false, "test message", new object(), new object(), new object(), new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: false, "test message", new object(), new object(), new object(), new object(), new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: false, "test message", new object(), new object(), new object(), new object(), new object(), new object(), new object()));
                Assert.DoesNotThrow(() => Debugging.Assert(condition: false, "test message", new object(), new object(), new object(), new object(), new object(), new object(), new object(), new object()));
            });
        }

        [Test, LuceneNetSpecific]
        [TestCaseSource(typeof(TestDebugging), "GetMessageFormatCases", new object[] { 1 })]
        public void TestMessageFormatting_1Parameter(string expectedMessage, string messageFormat, object p0)
        {
            TestWithAsserts(enabled: true, () =>
            {
                var e = Assert.Throws<AssertionException>(() => Debugging.Assert(condition: false, messageFormat, p0));
                Assert.AreEqual(expectedMessage, e.Message);
            });
        }

        [Test, LuceneNetSpecific]
        [TestCaseSource(typeof(TestDebugging), "GetMessageFormatCases", new object[] { 2 })]
        public void TestMessageFormatting_2Parameters(string expectedMessage, string messageFormat, object p0, object p1)
        {
            TestWithAsserts(enabled: true, () =>
            {
                var e = Assert.Throws<AssertionException>(() => Debugging.Assert(condition: false, messageFormat, p0, p1));
                Assert.AreEqual(expectedMessage, e.Message);
            });
        }

        [Test, LuceneNetSpecific]
        [TestCaseSource(typeof(TestDebugging), "GetMessageFormatCases", new object[] { 3 })]
        public void TestMessageFormatting_3Parameters(string expectedMessage, string messageFormat, object p0, object p1, object p2)
        {
            TestWithAsserts(enabled: true, () =>
            {
                var e = Assert.Throws<AssertionException>(() => Debugging.Assert(condition: false, messageFormat, p0, p1, p2));
                Assert.AreEqual(expectedMessage, e.Message);
            });
        }

        [Test, LuceneNetSpecific]
        [TestCaseSource(typeof(TestDebugging), "GetMessageFormatCases", new object[] { 4 })]
        public void TestMessageFormatting_4Parameters(string expectedMessage, string messageFormat, object p0, object p1, object p2, object p3)
        {
            TestWithAsserts(enabled: true, () =>
            {
                var e = Assert.Throws<AssertionException>(() => Debugging.Assert(condition: false, messageFormat, p0, p1, p2, p3));
                Assert.AreEqual(expectedMessage, e.Message);
            });
        }

        [Test, LuceneNetSpecific]
        [TestCaseSource(typeof(TestDebugging), "GetMessageFormatCases", new object[] { 5 })]
        public void TestMessageFormatting_5Parameters(string expectedMessage, string messageFormat, object p0, object p1, object p2, object p3, object p4)
        {
            TestWithAsserts(enabled: true, () =>
            {
                var e = Assert.Throws<AssertionException>(() => Debugging.Assert(condition: false, messageFormat, p0, p1, p2, p3, p4));
                Assert.AreEqual(expectedMessage, e.Message);
            });
        }

        [Test, LuceneNetSpecific]
        [TestCaseSource(typeof(TestDebugging), "GetMessageFormatCases", new object[] { 6 })]
        public void TestMessageFormatting_6Parameters(string expectedMessage, string messageFormat, object p0, object p1, object p2, object p3, object p4, object p5)
        {
            TestWithAsserts(enabled: true, () =>
            {
                var e = Assert.Throws<AssertionException>(() => Debugging.Assert(condition: false, messageFormat, p0, p1, p2, p3, p4, p5));
                Assert.AreEqual(expectedMessage, e.Message);
            });
        }

        [Test, LuceneNetSpecific]
        [TestCaseSource(typeof(TestDebugging), "GetMessageFormatCases", new object[] { 7 })]
        public void TestMessageFormatting_7Parameters(string expectedMessage, string messageFormat, object p0, object p1, object p2, object p3, object p4, object p5, object p6)
        {
            TestWithAsserts(enabled: true, () =>
            {
                var e = Assert.Throws<AssertionException>(() => Debugging.Assert(condition: false, messageFormat, p0, p1, p2, p3, p4, p5, p6));
                Assert.AreEqual(expectedMessage, e.Message);
            });
        }

        private void TestWithAsserts(bool enabled, Action expression)
        {
            var originalAssertsEnabled = Debugging.AssertsEnabled;

            try
            {
                Debugging.AssertsEnabled = enabled;

                expression();
            }
            finally
            {
                Debugging.AssertsEnabled = originalAssertsEnabled;
            }
        }

        public static IEnumerable<TestCaseData> GetMessageFormatCases(int parameterCount)
        {
            foreach (var test in GetMessageFormatData())
            {
                var messageFormat = new StringBuilder();
                var parameters = new List<object>();
                var expectedMessage = new StringBuilder();
                for (int i = 0; i < parameterCount; i++)
                {
                    parameters.Add(test.Parameter);
                    if (i > 0)
                    {
                        messageFormat.Append(' ');
                        expectedMessage.Append(' ');
                    }
                    messageFormat.Append(test.MessageFormatPrefix);
                    messageFormat.Append(i.ToString(CultureInfo.InvariantCulture));
                    messageFormat.Append(test.MessageFormatSuffix);
                    expectedMessage.Append(test.ExpectedMessage);
                }

                var args = new List<object>();
                args.Add(expectedMessage.ToString());
                args.Add(messageFormat.ToString());
                args.AddRange(parameters);
                yield return new TestCaseData(args.ToArray());
            }
        }

        private class MessageFormatData
        {
            public MessageFormatData(string messageFormatPrefix, string messageFormatSuffix, object parameter, string expectedMessage)
            {
                MessageFormatPrefix = messageFormatPrefix ?? throw new ArgumentNullException(nameof(messageFormatPrefix));
                MessageFormatSuffix = messageFormatSuffix ?? throw new ArgumentNullException(nameof(messageFormatSuffix));
                Parameter = parameter;
                ExpectedMessage = expectedMessage;
            }

            public string MessageFormatPrefix { get; set; }
            public string MessageFormatSuffix { get; set; }
            public object Parameter { get; set; }
            public string ExpectedMessage { get; set; }
        }

        private static IEnumerable<MessageFormatData> GetMessageFormatData()
        {
            yield return new MessageFormatData("{", "}", "foo", "foo"); // string
            yield return new MessageFormatData("{", "}", (string)null, "null"); // null string
            yield return new MessageFormatData("{", "}", 12345, "12345"); // int
            yield return new MessageFormatData("0x{", ":x}", 12345, "0x3039"); // int hex

            yield return new MessageFormatData("{", "}", new string[] { "foo", "bar", "baz" }, "[foo, bar, baz]"); // string array
            yield return new MessageFormatData("{", "}", new int[] { 10, 11, 12 }, "[10, 11, 12]"); // int array
            yield return new MessageFormatData("{", "}", (Array)null, "null"); // null array

            yield return new MessageFormatData("{", "}", new JCG.List<string> { "foo", "bar", "baz" }, "[foo, bar, baz]"); // string list
            yield return new MessageFormatData("{", "}", new JCG.List<int> { 10, 11, 12 }, "[10, 11, 12]"); // int list

            yield return new MessageFormatData("{", "}", new List<string> { "foo", "bar", "baz" }, "[foo, bar, baz]"); // string list
            yield return new MessageFormatData("{", "}", new List<int> { 10, 11, 12 }, "[10, 11, 12]"); // int list
            yield return new MessageFormatData("{", "}", (IList<int>)null, "null"); // null list

            yield return new MessageFormatData("{", "}", new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } }, "{key1=value1, key2=value2}"); // string dictionary
            yield return new MessageFormatData("{", "}", (IDictionary<string, string>)null, "null"); // null dictionary
        }
    }
}
