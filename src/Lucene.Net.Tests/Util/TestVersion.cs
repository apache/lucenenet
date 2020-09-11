using NUnit.Framework;
using System;
using System.Linq;
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

#pragma warning disable 612, 618
    [TestFixture]
    public class TestVersion : LuceneTestCase
    {
        [Test]
        public virtual void Test()
        {
            foreach (LuceneVersion v in Enum.GetValues(typeof(LuceneVersion)))
            {
                Assert.IsTrue(LuceneVersion.LUCENE_CURRENT.OnOrAfter(v), "LUCENE_CURRENT must be always onOrAfter(" + v + ")");
            }
            Assert.IsTrue(LuceneVersion.LUCENE_40.OnOrAfter(LuceneVersion.LUCENE_31));
            Assert.IsTrue(LuceneVersion.LUCENE_40.OnOrAfter(LuceneVersion.LUCENE_40));
            Assert.IsFalse(LuceneVersion.LUCENE_30.OnOrAfter(LuceneVersion.LUCENE_31));
        }

        [Test]
        public virtual void TestParseLeniently()
        {
            Assert.AreEqual(LuceneVersion.LUCENE_40, LuceneVersionExtensions.ParseLeniently("4.0"));
            Assert.AreEqual(LuceneVersion.LUCENE_40, LuceneVersionExtensions.ParseLeniently("LUCENE_40"));
            Assert.AreEqual(LuceneVersion.LUCENE_CURRENT, LuceneVersionExtensions.ParseLeniently("LUCENE_CURRENT"));
        }

        [Test]
        public virtual void TestDeprecations()
        {
            LuceneVersion[] values = Enum.GetValues(typeof(LuceneVersion)).Cast<LuceneVersion>().ToArray();
            // all but the latest version should be deprecated
            for (int i = 0; i < values.Length; i++)
            {
                if (i + 1 == values.Length)
                {
                    Assert.AreEqual(LuceneVersion.LUCENE_CURRENT, values[i], "Last constant must be LUCENE_CURRENT");
                }
                bool dep = typeof(LuceneVersion).GetField(values[i].ToString()).GetCustomAttributes(typeof(ObsoleteAttribute), false).Any();
                if (i + 2 != values.Length)
                {
                    assertTrue(values[i].ToString() + " should be deprecated", dep);
                }
                else
                {
                    assertFalse(values[i].ToString() + " should not be deprecated", dep);
                }
            }
        }

        [Test]
        public virtual void TestAgainstMainVersionConstant()
        {
            LuceneVersion[] values = Enum.GetValues(typeof(LuceneVersion)).Cast<LuceneVersion>().ToArray();
            Assert.IsTrue(values.Length >= 2);
            string mainVersionWithoutAlphaBeta = Constants.MainVersionWithoutAlphaBeta;
            LuceneVersion mainVersionParsed = LuceneVersionExtensions.ParseLeniently(mainVersionWithoutAlphaBeta);
            Assert.AreEqual(mainVersionParsed, values[values.Length - 2], "Constant one before last must be the same as the parsed LUCENE_MAIN_VERSION (without alpha/beta) constant: " + mainVersionWithoutAlphaBeta);
        }
    }
#pragma warning restore 612, 618
}