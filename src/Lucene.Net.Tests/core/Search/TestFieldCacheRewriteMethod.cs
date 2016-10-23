namespace Lucene.Net.Search
{
    using NUnit.Framework;
    using RegExp = Lucene.Net.Util.Automaton.RegExp;

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

    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// Tests the FieldcacheRewriteMethod with random regular expressions
    /// </summary>
    [TestFixture]
    public class TestFieldCacheRewriteMethod : TestRegexpRandom2
    {
        /// <summary>
        /// Test fieldcache rewrite against filter rewrite </summary>
        protected internal override void AssertSame(string regexp)
        {
            RegexpQuery fieldCache = new RegexpQuery(new Term(FieldName, regexp), RegExp.NONE);
            fieldCache.SetRewriteMethod(new FieldCacheRewriteMethod());

            RegexpQuery filter = new RegexpQuery(new Term(FieldName, regexp), RegExp.NONE);
            filter.SetRewriteMethod(MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE);

            TopDocs fieldCacheDocs = Searcher1.Search(fieldCache, 25);
            TopDocs filterDocs = Searcher2.Search(filter, 25);

            CheckHits.CheckEqual(fieldCache, fieldCacheDocs.ScoreDocs, filterDocs.ScoreDocs);
        }

        [Test]
        public virtual void TestEquals()
        {
            RegexpQuery a1 = new RegexpQuery(new Term(FieldName, "[aA]"), RegExp.NONE);
            RegexpQuery a2 = new RegexpQuery(new Term(FieldName, "[aA]"), RegExp.NONE);
            RegexpQuery b = new RegexpQuery(new Term(FieldName, "[bB]"), RegExp.NONE);
            Assert.AreEqual(a1, a2);
            Assert.IsFalse(a1.Equals(b));

            a1.SetRewriteMethod(new FieldCacheRewriteMethod());
            a2.SetRewriteMethod(new FieldCacheRewriteMethod());
            b.SetRewriteMethod(new FieldCacheRewriteMethod());
            Assert.AreEqual(a1, a2);
            Assert.IsFalse(a1.Equals(b));
            QueryUtils.Check(a1);
        }



        #region TestSnapshotDeletionPolicy
        // LUCENENET NOTE: Tests in a base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        /// <summary>
        /// test a bunch of random regular expressions </summary>
        [Test, MaxTime(60000)]
        public override void TestRegexps()
        {
            base.TestRegexps();
        }

        #endregion
    }
}