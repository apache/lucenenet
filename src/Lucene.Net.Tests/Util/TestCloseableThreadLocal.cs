using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

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

    [TestFixture]
    public class TestIDisposableThreadLocal : LuceneTestCase
    {
        public const string TEST_VALUE = "initvaluetest";

        [Test]
        public virtual void TestInitValue()
        {
            DisposableThreadLocal<object> tl = new DisposableThreadLocal<object>(() => TEST_VALUE);
            string str = (string)tl.Value;
            Assert.AreEqual(TEST_VALUE, str);
        }

        [Test]
        public virtual void TestNullValue()
        {
            // Tests that null can be set as a valid value (LUCENE-1805). this
            // previously failed in get().
            DisposableThreadLocal<object> ctl = new DisposableThreadLocal<object>();
            ctl.Value = (null);
            Assert.IsNull(ctl.Value);
        }

        [Test]
        public virtual void TestDefaultValueWithoutSetting()
        {
            // LUCENE-1805: make sure default get returns null,
            // twice in a row
            DisposableThreadLocal<object> ctl = new DisposableThreadLocal<object>();
            Assert.IsNull(ctl.Value);
        }
    }
}