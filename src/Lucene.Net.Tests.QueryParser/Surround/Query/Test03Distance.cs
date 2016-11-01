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
    public class Test03Distance : LuceneTestCase
    {
        //public static void Main(string[] args) {
        //    TestRunner.run(new TestSuite(Test03Distance.class));
        //}

        private bool verbose = false;
        private int maxBasicQueries = 16;

        private string[] exceptionQueries = {
            "(aa and bb) w cc",
            "(aa or bb) w (cc and dd)",
            "(aa opt bb) w cc",
            "(aa not bb) w cc",
            "(aa or bb) w (bi:cc)",
            "(aa or bb) w bi:cc",
            "(aa or bi:bb) w cc",
            "(aa or (bi:bb)) w cc",
            "(aa or (bb and dd)) w cc"
        };

        [Test]
        public virtual void Test00Exceptions()
        {
            string m = ExceptionQueryTst.GetFailQueries(exceptionQueries, verbose);
            if (m.Length > 0)
            {
                fail("No ParseException for:\n" + m);
            }
        }

        private readonly string fieldName = "bi";

        private string[] docs1 = {
            "word1 word2 word3",
            "word4 word5",
            "ord1 ord2 ord3",
            "orda1 orda2 orda3 word2 worda3",
            "a c e a b c"
        };

        SingleFieldTestDb db1;

        public override void SetUp()
        {
            base.SetUp();
            db1 = new SingleFieldTestDb(Random(), docs1, fieldName);
            db2 = new SingleFieldTestDb(Random(), docs2, fieldName);
            db3 = new SingleFieldTestDb(Random(), docs3, fieldName);
        }

        private void DistanceTst(String query, int[] expdnrs, SingleFieldTestDb db)
        {
            BooleanQueryTst bqt = new BooleanQueryTst(query, expdnrs, db, fieldName, this,
                                                        new BasicQueryFactory(maxBasicQueries));
            bqt.Verbose = (verbose);
            bqt.DoTest();
        }

        public virtual void DistanceTest1(string query, int[] expdnrs)
        {
            DistanceTst(query, expdnrs, db1);
        }

        [Test]
        public virtual void Test0W01()
        {
            int[] expdnrs = { 0 }; DistanceTest1("word1 w word2", expdnrs);
        }
        [Test]
        public virtual void Test0N01()
        {
            int[] expdnrs = { 0 }; DistanceTest1("word1 n word2", expdnrs);
        }
        [Test]
        public virtual void Test0N01r()
        { /* r reverse */
            int[] expdnrs = { 0 }; DistanceTest1("word2 n word1", expdnrs);
        }
        [Test]
        public virtual void Test0W02()
        {
            int[] expdnrs = { }; DistanceTest1("word2 w word1", expdnrs);
        }
        [Test]
        public virtual void Test0W03()
        {
            int[] expdnrs = { }; DistanceTest1("word2 2W word1", expdnrs);
        }
        [Test]
        public virtual void Test0N03()
        {
            int[] expdnrs = { 0 }; DistanceTest1("word2 2N word1", expdnrs);
        }
        [Test]
        public virtual void Test0N03r()
        {
            int[] expdnrs = { 0 }; DistanceTest1("word1 2N word2", expdnrs);
        }

        [Test]
        public virtual void Test0W04()
        {
            int[] expdnrs = { }; DistanceTest1("word2 3w word1", expdnrs);
        }

        [Test]
        public virtual void Test0N04()
        {
            int[] expdnrs = { 0 }; DistanceTest1("word2 3n word1", expdnrs);
        }
        [Test]
        public virtual void Test0N04r()
        {
            int[] expdnrs = { 0 }; DistanceTest1("word1 3n word2", expdnrs);
        }

        [Test]
        public virtual void Test0W05()
        {
            int[] expdnrs = { }; DistanceTest1("orda1 w orda3", expdnrs);
        }
        [Test]
        public virtual void Test0W06()
        {
            int[] expdnrs = { 3 }; DistanceTest1("orda1 2w orda3", expdnrs);
        }

        [Test]
        public virtual void Test1Wtrunc01()
        {
            int[] expdnrs = { 0 }; DistanceTest1("word1* w word2", expdnrs);
        }
        [Test]
        public virtual void Test1Wtrunc02()
        {
            int[] expdnrs = { 0 }; DistanceTest1("word* w word2", expdnrs);
        }
        [Test]
        public virtual void Test1Wtrunc02r()
        {
            int[] expdnrs = { 0, 3 }; DistanceTest1("word2 w word*", expdnrs);
        }
        [Test]
        public virtual void Test1Ntrunc02()
        {
            int[] expdnrs = { 0, 3 }; DistanceTest1("word* n word2", expdnrs);
        }
        [Test]
        public virtual void Test1Ntrunc02r()
        {
            int[] expdnrs = { 0, 3 }; DistanceTest1("word2 n word*", expdnrs);
        }

        [Test]
        public virtual void Test1Wtrunc03()
        {
            int[] expdnrs = { 0 }; DistanceTest1("word1* w word2*", expdnrs);
        }
        [Test]
        public virtual void Test1Ntrunc03()
        {
            int[] expdnrs = { 0 }; DistanceTest1("word1* N word2*", expdnrs);
        }

        [Test]
        public virtual void Test1Wtrunc04()
        {
            int[] expdnrs = { }; DistanceTest1("kxork* w kxor*", expdnrs);
        }
        [Test]
        public virtual void Test1Ntrunc04()
        {
            int[] expdnrs = { }; DistanceTest1("kxork* 99n kxor*", expdnrs);
        }

        [Test]
        public virtual void Test1Wtrunc05()
        {
            int[] expdnrs = { }; DistanceTest1("word2* 2W word1*", expdnrs);
        }
        [Test]
        public virtual void Test1Ntrunc05()
        {
            int[] expdnrs = { 0 }; DistanceTest1("word2* 2N word1*", expdnrs);
        }

        [Test]
        public virtual void Test1Wtrunc06()
        {
            int[] expdnrs = { 3 }; DistanceTest1("ord* W word*", expdnrs);
        }
        [Test]
        public virtual void Test1Ntrunc06()
        {
            int[] expdnrs = { 3 }; DistanceTest1("ord* N word*", expdnrs);
        }
        [Test]
        public virtual void Test1Ntrunc06r()
        {
            int[] expdnrs = { 3 }; DistanceTest1("word* N ord*", expdnrs);
        }

        [Test]
        public virtual void Test1Wtrunc07()
        {
            int[] expdnrs = { 3 }; DistanceTest1("(orda2 OR orda3) W word*", expdnrs);
        }
        [Test]
        public virtual void Test1Wtrunc08()
        {
            int[] expdnrs = { 3 }; DistanceTest1("(orda2 OR orda3) W (word2 OR worda3)", expdnrs);
        }
        [Test]
        public virtual void Test1Wtrunc09()
        {
            int[] expdnrs = { 3 }; DistanceTest1("(orda2 OR orda3) 2W (word2 OR worda3)", expdnrs);
        }
        [Test]
        public virtual void Test1Ntrunc09()
        {
            int[] expdnrs = { 3 }; DistanceTest1("(orda2 OR orda3) 2N (word2 OR worda3)", expdnrs);
        }

        string[] docs2 = {
            "w1 w2 w3 w4 w5",
            "w1 w3 w2 w3",
            ""
        };

        SingleFieldTestDb db2;

        public virtual void DistanceTest2(string query, int[] expdnrs)
        {
            DistanceTst(query, expdnrs, db2);
        }

        [Test]
        public virtual void Test2Wprefix01()
        {
            int[] expdnrs = { 0 }; DistanceTest2("W (w1, w2, w3)", expdnrs);
        }
        [Test]
        public virtual void Test2Nprefix01a()
        {
            int[] expdnrs = { 0, 1 }; DistanceTest2("N(w1, w2, w3)", expdnrs);
        }
        [Test]
        public virtual void Test2Nprefix01b()
        {
            int[] expdnrs = { 0, 1 }; DistanceTest2("N(w3, w1, w2)", expdnrs);
        }

        [Test]
        public virtual void Test2Wprefix02()
        {
            int[] expdnrs = { 0, 1 }; DistanceTest2("2W(w1,w2,w3)", expdnrs);
        }

        [Test]
        public virtual void Test2Nprefix02a()
        {
            int[] expdnrs = { 0, 1 }; DistanceTest2("2N(w1,w2,w3)", expdnrs);
        }
        [Test]
        public virtual void Test2Nprefix02b()
        {
            int[] expdnrs = { 0, 1 }; DistanceTest2("2N(w2,w3,w1)", expdnrs);
        }

        [Test]
        public virtual void Test2Wnested01()
        {
            int[] expdnrs = { 0 }; DistanceTest2("w1 W w2 W w3", expdnrs);
        }
        [Test]
        public virtual void Test2Nnested01()
        {
            int[] expdnrs = { 0 }; DistanceTest2("w1 N w2 N w3", expdnrs);
        }

        [Test]
        public virtual void Test2Wnested02()
        {
            int[] expdnrs = { 0, 1 }; DistanceTest2("w1 2W w2 2W w3", expdnrs);
        }
        [Test]
        public virtual void Test2Nnested02()
        {
            int[] expdnrs = { 0, 1 }; DistanceTest2("w1 2N w2 2N w3", expdnrs);
        }

        string[] docs3 = {
            "low pressure temperature inversion and rain",
            "when the temperature has a negative height above a depression no precipitation gradient is expected",
            "when the temperature has a negative height gradient above a depression no precipitation is expected",
            ""
        };

        SingleFieldTestDb db3;

        public virtual void DistanceTest3(string query, int[] expdnrs)
        {
            DistanceTst(query, expdnrs, db3);
        }

        [Test]
        public virtual void Test3Example01()
        {
            int[] expdnrs = { 0, 2 }; // query does not match doc 1 because "gradient" is in wrong place there.
            DistanceTest3("50n((low w pressure*) or depression*,"
                           + "5n(temperat*, (invers* or (negativ* 3n gradient*))),"
                           + "rain* or precipitat*)",
                           expdnrs);
        }
    }
}
