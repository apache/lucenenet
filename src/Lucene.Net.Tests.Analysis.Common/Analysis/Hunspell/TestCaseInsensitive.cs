// Lucene version compatibility level 4.10.4
using NUnit.Framework;

namespace Lucene.Net.Analysis.Hunspell
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

    public class TestCaseInsensitive : StemmerTestBase
    {
        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();
            Init(true, "simple.aff", "mixedcase.dic");
        }

        [Test]
        public virtual void TestCaseInsensitivity()
        {
            AssertStemsTo("lucene", "lucene", "lucen");
            AssertStemsTo("LuCeNe", "lucene", "lucen");
            AssertStemsTo("mahoute", "mahout");
            AssertStemsTo("MaHoUte", "mahout");
        }
        [Test]
        public virtual void TestSimplePrefix()
        {
            AssertStemsTo("solr", "olr");
        }
        [Test]
        public virtual void TestRecursiveSuffix()
        {
            // we should not recurse here! as the suffix has no continuation!
            AssertStemsTo("abcd");
        }

        // all forms unmunched from dictionary
        [Test]
        public virtual void TestAllStems()
        {
            AssertStemsTo("ab", "ab");
            AssertStemsTo("abc", "ab");
            AssertStemsTo("apach", "apach");
            AssertStemsTo("apache", "apach");
            AssertStemsTo("foo", "foo", "foo");
            AssertStemsTo("food", "foo");
            AssertStemsTo("foos", "foo");
            AssertStemsTo("lucen", "lucen");
            AssertStemsTo("lucene", "lucen", "lucene");
            AssertStemsTo("mahout", "mahout");
            AssertStemsTo("mahoute", "mahout");
            AssertStemsTo("moo", "moo");
            AssertStemsTo("mood", "moo");
            AssertStemsTo("olr", "olr");
            AssertStemsTo("solr", "olr");
        }

        // some bogus stuff that should not stem (empty lists)!
        [Test]
        public virtual void TestBogusStems()
        {
            AssertStemsTo("abs");
            AssertStemsTo("abe");
            AssertStemsTo("sab");
            AssertStemsTo("sapach");
            AssertStemsTo("sapache");
            AssertStemsTo("apachee");
            AssertStemsTo("sfoo");
            AssertStemsTo("sfoos");
            AssertStemsTo("fooss");
            AssertStemsTo("lucenee");
            AssertStemsTo("solre");
        }
    }
}