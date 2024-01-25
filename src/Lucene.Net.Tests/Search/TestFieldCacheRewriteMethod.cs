using Lucene.Net.Util.Automaton;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Search
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
            RegexpQuery fieldCache = new RegexpQuery(new Term(fieldName, regexp), RegExpSyntax.NONE);
            fieldCache.MultiTermRewriteMethod = new FieldCacheRewriteMethod();

            RegexpQuery filter = new RegexpQuery(new Term(fieldName, regexp), RegExpSyntax.NONE);
            filter.MultiTermRewriteMethod = MultiTermQuery.CONSTANT_SCORE_FILTER_REWRITE;

            TopDocs fieldCacheDocs = searcher1.Search(fieldCache, 25);
            TopDocs filterDocs = searcher2.Search(filter, 25);

            CheckHits.CheckEqual(fieldCache, fieldCacheDocs.ScoreDocs, filterDocs.ScoreDocs);
        }

        [Test]
        public virtual void TestEquals()
        {
            RegexpQuery a1 = new RegexpQuery(new Term(fieldName, "[aA]"), RegExpSyntax.NONE);
            RegexpQuery a2 = new RegexpQuery(new Term(fieldName, "[aA]"), RegExpSyntax.NONE);
            RegexpQuery b = new RegexpQuery(new Term(fieldName, "[bB]"), RegExpSyntax.NONE);
            Assert.AreEqual(a1, a2);
            Assert.IsFalse(a1.Equals(b));

            a1.MultiTermRewriteMethod = new FieldCacheRewriteMethod();
            a2.MultiTermRewriteMethod = new FieldCacheRewriteMethod();
            b.MultiTermRewriteMethod = new FieldCacheRewriteMethod();
            Assert.AreEqual(a1, a2);
            Assert.IsFalse(a1.Equals(b));
            QueryUtils.Check(a1);
        }
    }
}
