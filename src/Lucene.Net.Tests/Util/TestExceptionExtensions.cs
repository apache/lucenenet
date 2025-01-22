using NUnit.Framework;
using System;
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
    /// Tests for <see cref="ExceptionExtensions"/>
    /// </summary>
    [TestFixture]
    public class TestExceptionExtensions : LuceneTestCase
    {
        [Test]
        public void TestAddSuppressed()
        {
            var e = new Exception("main");
            var suppressed1 = new Exception("suppressed1");
            var suppressed2 = new Exception("suppressed2");

            e.AddSuppressed(suppressed1);
            e.AddSuppressed(suppressed2);

            Assert.AreEqual(2, e.GetSuppressed().Length);
            Assert.IsTrue(e.GetSuppressed().Any(i => i.Message == suppressed1.Message));
            Assert.IsTrue(e.GetSuppressed().Any(i => i.Message == suppressed2.Message));

            // test GetSuppressedAsList
            Assert.AreEqual(2, e.GetSuppressedAsList().Count);
            Assert.IsTrue(e.GetSuppressedAsList().Any(i => i.Message == suppressed1.Message));
            Assert.IsTrue(e.GetSuppressedAsList().Any(i => i.Message == suppressed2.Message));

            // test GetSuppressedAsListOrDefault
            var listOrDefault = e.GetSuppressedAsListOrDefault();
            Assert.IsNotNull(listOrDefault);
            Assert.AreEqual(2, listOrDefault!.Count);
            Assert.IsTrue(listOrDefault.Any(i => i.Message == suppressed1.Message));
            Assert.IsTrue(listOrDefault.Any(i => i.Message == suppressed2.Message));
        }

        [Test]
        public void TestGetSuppressedAsListOrDefault_InitialStateNull()
        {
            var e = new Exception("test");
            var list = e.GetSuppressedAsListOrDefault();
            Assert.IsNull(list);
        }

        [Test]
        public void TestGetSuppressedAsList_CreatesEmptyList()
        {
            var e = new Exception("test");
            var list = e.GetSuppressedAsList();
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }
    }
}
