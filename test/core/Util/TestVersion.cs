/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using NUnit.Framework;

namespace Lucene.Net.Util
{
    [TestFixture]
    public class TestVersion : LuceneTestCase
    {
        [Test]
        public virtual void TestOnOrAfter()
        {
            foreach (Version v in Enum.GetValues(typeof(Version)))
            {
                Assert.IsTrue(Version.LUCENE_CURRENT.OnOrAfter(v), string.Format("LUCENE_CURRENT must be always OnOrAfter({0})", v));
            }
            Assert.IsTrue(Version.LUCENE_30.OnOrAfter(Version.LUCENE_29));
            Assert.IsTrue(Version.LUCENE_30.OnOrAfter(Version.LUCENE_30));
            Assert.IsFalse(Version.LUCENE_29.OnOrAfter(Version.LUCENE_30));
        }
    }
}