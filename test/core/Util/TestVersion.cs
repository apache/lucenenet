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
using System.Linq;
using NUnit.Framework;

namespace Lucene.Net.Util
{
    [TestFixture]
    public class TestVersion : LuceneTestCase
    {
        [Test]
        public void Test()
        {
            foreach (var v in Enum.GetValues(typeof(Version)))
            {
                Assert.IsTrue("LUCENE_CURRENT must be always onOrAfter(" + v + ")", Version.LUCENE_CURRENT.OnOrAfter(v));
            }
            Assert.IsTrue(Version.LUCENE_40.OnOrAfter(Version.LUCENE_31));
            Assert.IsTrue(Version.LUCENE_40.OnOrAfter(Version.LUCENE_40));
            Assert.IsFalse(Version.LUCENE_30.OnOrAfter(Version.LUCENE_31));
        }

        [Test]
        public void TestParseLeniently()
        {
            assertEquals(Version.LUCENE_40, Version.ParseLeniently("4.0"));
            assertEquals(Version.LUCENE_40, Version.ParseLeniently("LUCENE_40"));
            assertEquals(Version.LUCENE_CURRENT, Version.ParseLeniently("LUCENE_CURRENT"));
        }

        [Test]
        public void TestDeprecations()
        {
            var values = Enum.GetValues(typeof (Version)).OfType<Version>().ToList();
            // all but the latest version should be deprecated
            for (int i = 0; i < values.Count; i++)
            {
                if (i + 1 == values.Count)
                {
                    Assert.AreSame(Version.LUCENE_CURRENT, values[i], "Last constant must be LUCENE_CURRENT");
                }
                var field = typeof(Version).GetField(Enum.GetName(typeof(Version), values[i]));
                bool dep = field.IsDefined(typeof(ObsoleteAttribute), true);
                if (i + 2 != values.Count)
                {
                    Assert.IsTrue(dep, Enum.GetName(typeof(Version), values[i]) + " should be deprecated");
                }
                else
                {
                    Assert.IsFalse(dep, Enum.GetName(typeof(Version), values[i]) + " should not be deprecated");
                }
            }
        }
    }
}