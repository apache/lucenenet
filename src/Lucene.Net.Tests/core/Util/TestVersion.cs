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

using NUnit.Framework;
using System;
using System.Linq;

namespace Lucene.Net.Util
{
    [TestFixture]
    public class TestVersion : LuceneTestCase
    {
        [Test]
        public virtual void Test()
        {
            foreach (Version v in Enum.GetValues(typeof(Version)))
            {
                Assert.IsTrue(Version.LUCENE_CURRENT.OnOrAfter(v), "LUCENE_CURRENT must be always onOrAfter(" + v + ")");
            }
            Assert.IsTrue(Version.LUCENE_40.OnOrAfter(Version.LUCENE_31));
            Assert.IsTrue(Version.LUCENE_40.OnOrAfter(Version.LUCENE_40));
            Assert.IsFalse(Version.LUCENE_30.OnOrAfter(Version.LUCENE_31));
        }

        [Test]
        public virtual void TestParseLeniently()
        {
            Assert.AreEqual(Version.LUCENE_40, VersionEnumExtensionMethods.ParseLeniently("4.0"));
            Assert.AreEqual(Version.LUCENE_40, VersionEnumExtensionMethods.ParseLeniently("LUCENE_40"));
            Assert.AreEqual(Version.LUCENE_CURRENT, VersionEnumExtensionMethods.ParseLeniently("LUCENE_CURRENT"));
        }

        [Test]
        public virtual void TestDeprecations()
        {
            Version[] values = Enum.GetValues(typeof(Version)).Cast<Version>().ToArray();
            // all but the latest version should be deprecated
            for (int i = 0; i < values.Length; i++)
            {
                if (i + 1 == values.Length)
                {
                    Assert.AreEqual(Version.LUCENE_CURRENT, values[i], "Last constant must be LUCENE_CURRENT");
                }
                /*bool dep = typeof(Version).GetField(values[i].Name()).isAnnotationPresent(typeof(Deprecated));
                if (i + 2 != values.Length)
                {
                  Assert.IsTrue(values[i].name() + " should be deprecated", dep);
                }
                else
                {
                  Assert.IsFalse(values[i].name() + " should not be deprecated", dep);
                }*/
            }
        }

        [Test]
        public virtual void TestAgainstMainVersionConstant()
        {
            Version[] values = Enum.GetValues(typeof(Version)).Cast<Version>().ToArray();
            Assert.IsTrue(values.Length >= 2);
            string mainVersionWithoutAlphaBeta = Constants.MainVersionWithoutAlphaBeta();
            Version mainVersionParsed = VersionEnumExtensionMethods.ParseLeniently(mainVersionWithoutAlphaBeta);
            Assert.AreEqual(mainVersionParsed, values[values.Length - 2], "Constant one before last must be the same as the parsed LUCENE_MAIN_VERSION (without alpha/beta) constant: " + mainVersionWithoutAlphaBeta);
        }
    }
}