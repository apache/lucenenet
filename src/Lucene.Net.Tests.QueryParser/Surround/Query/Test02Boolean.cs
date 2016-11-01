using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.QueryParsers.Surround.Query
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
    public class Test02Boolean : LuceneTestCase
    {
        //public static void Main(string[] args) {
        //    TestRunner.run(new TestSuite(Test02Boolean.class));
        //}

        private readonly string fieldName = "bi";
        private bool verbose = false;
        private int maxBasicQueries = 16;

        string[] docs1 = {
            "word1 word2 word3",
            "word4 word5",
            "ord1 ord2 ord3",
            "orda1 orda2 orda3 word2 worda3",
            "a c e a b c"
        };

        public override void SetUp()
        {
            base.SetUp();
            db1 = new SingleFieldTestDb(Random(), docs1, fieldName);
        }

        private SingleFieldTestDb db1;


        public void NormalTest1(String query, int[] expdnrs)
        {
            BooleanQueryTst bqt = new BooleanQueryTst(query, expdnrs, db1, fieldName, this,
                                                        new BasicQueryFactory(maxBasicQueries));
            bqt.Verbose = (verbose);
            bqt.DoTest();
        }

        [Test]
        public virtual void Test02Terms01()
        {
            int[] expdnrs = { 0 }; NormalTest1("word1", expdnrs);
        }
        [Test]
        public virtual void Test02Terms02()
        {
            int[] expdnrs = { 0, 1, 3 }; NormalTest1("word*", expdnrs);
        }
        [Test]
        public virtual void Test02Terms03()
        {
            int[] expdnrs = { 2 }; NormalTest1("ord2", expdnrs);
        }
        [Test]
        public virtual void Test02Terms04()
        {
            int[] expdnrs = { }; NormalTest1("kxork*", expdnrs);
        }
        [Test]
        public virtual void Test02Terms05()
        {
            int[] expdnrs = { 0, 1, 3 }; NormalTest1("wor*", expdnrs);
        }
        [Test]
        public virtual void Test02Terms06()
        {
            int[] expdnrs = { }; NormalTest1("ab", expdnrs);
        }

        [Test]
        public virtual void Test02Terms10()
        {
            int[] expdnrs = { }; NormalTest1("abc?", expdnrs);
        }
        [Test]
        public virtual void Test02Terms13()
        {
            int[] expdnrs = { 0, 1, 3 }; NormalTest1("word?", expdnrs);
        }
        [Test]
        public virtual void Test02Terms14()
        {
            int[] expdnrs = { 0, 1, 3 }; NormalTest1("w?rd?", expdnrs);
        }
        [Test]
        public virtual void Test02Terms20()
        {
            int[] expdnrs = { 0, 1, 3 }; NormalTest1("w*rd?", expdnrs);
        }
        [Test]
        public virtual void Test02Terms21()
        {
            int[] expdnrs = { 3 }; NormalTest1("w*rd??", expdnrs);
        }
        [Test]
        public virtual void Test02Terms22()
        {
            int[] expdnrs = { 3 }; NormalTest1("w*?da?", expdnrs);
        }
        [Test]
        public virtual void Test02Terms23()
        {
            int[] expdnrs = { }; NormalTest1("w?da?", expdnrs);
        }

        [Test]
        public virtual void Test03And01()
        {
            int[] expdnrs = { 0 }; NormalTest1("word1 AND word2", expdnrs);
        }
        [Test]
        public virtual void Test03And02()
        {
            int[] expdnrs = { 3 }; NormalTest1("word* and ord*", expdnrs);
        }
        [Test]
        public virtual void Test03And03()
        {
            int[] expdnrs = { 0 }; NormalTest1("and(word1,word2)", expdnrs);
        }
        [Test]
        public virtual void Test04Or01()
        {
            int[] expdnrs = { 0, 3 }; NormalTest1("word1 or word2", expdnrs);
        }
        [Test]
        public virtual void Test04Or02()
        {
            int[] expdnrs = { 0, 1, 2, 3 }; NormalTest1("word* OR ord*", expdnrs);
        }
        [Test]
        public virtual void Test04Or03()
        {
            int[] expdnrs = { 0, 3 }; NormalTest1("OR (word1, word2)", expdnrs);
        }
        [Test]
        public virtual void Test05Not01()
        {
            int[] expdnrs = { 3 }; NormalTest1("word2 NOT word1", expdnrs);
        }
        [Test]
        public virtual void Test05Not02()
        {
            int[] expdnrs = { 0 }; NormalTest1("word2* not ord*", expdnrs);
        }
        [Test]
        public virtual void Test06AndOr01()
        {
            int[] expdnrs = { 0 }; NormalTest1("(word1 or ab)and or(word2,xyz, defg)", expdnrs);
        }
        [Test]
        public virtual void Test07AndOrNot02()
        {
            int[] expdnrs = { 0 }; NormalTest1("or( word2* not ord*, and(xyz,def))", expdnrs);
        }
    }
}
