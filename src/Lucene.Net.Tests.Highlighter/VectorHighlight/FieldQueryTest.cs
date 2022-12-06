using Lucene.Net.Index;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;
using Occur = Lucene.Net.Search.Occur;
using QueryPhraseMap = Lucene.Net.Search.VectorHighlight.FieldQuery.QueryPhraseMap;
using TermInfo = Lucene.Net.Search.VectorHighlight.FieldTermStack.TermInfo;

namespace Lucene.Net.Search.VectorHighlight
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

    public class FieldQueryTest : AbstractTestCase
    {
        private float boost;

        /**
         * Set boost to a random value each time it is called.
         */
        private void initBoost()
        {
            boost = Usually() ? 1F : ((float)(Random.NextDouble() / 2)) * 10000;
        }

        [Test]
        public void TestFlattenBoolean()
        {
            initBoost();
            BooleanQuery booleanQuery = new BooleanQuery();
            booleanQuery.Boost = (boost);
            booleanQuery.Add(tq("A"), Occur.MUST);
            booleanQuery.Add(tq("B"), Occur.MUST);
            booleanQuery.Add(tq("C"), Occur.SHOULD);

            BooleanQuery innerQuery = new BooleanQuery();
            innerQuery.Add(tq("D"), Occur.MUST);
            innerQuery.Add(tq("E"), Occur.MUST);
            booleanQuery.Add(innerQuery, Occur.MUST_NOT);

            FieldQuery fq = new FieldQuery(booleanQuery, true, true);
            ISet<Query> flatQueries = new JCG.HashSet<Query>();
            fq.Flatten(booleanQuery, reader, flatQueries);
            assertCollectionQueries(flatQueries, tq(boost, "A"), tq(boost, "B"), tq(boost, "C"));
        }

        [Test]
        public void TestFlattenDisjunctionMaxQuery()
        {
            initBoost();
            Query query = dmq(tq("A"), tq("B"), pqF("C", "D"));
            query.Boost = (boost);
            FieldQuery fq = new FieldQuery(query, true, true);
            ISet<Query> flatQueries = new JCG.HashSet<Query>();
            fq.Flatten(query, reader, flatQueries);
            assertCollectionQueries(flatQueries, tq(boost, "A"), tq(boost, "B"), pqF(boost, "C", "D"));
        }

        [Test]
        public void TestFlattenTermAndPhrase()
        {
            initBoost();
            BooleanQuery booleanQuery = new BooleanQuery();
            booleanQuery.Boost = (boost);
            booleanQuery.Add(tq("A"), Occur.MUST);
            booleanQuery.Add(pqF("B", "C"), Occur.MUST);

            FieldQuery fq = new FieldQuery(booleanQuery, true, true);
            ISet<Query> flatQueries = new JCG.HashSet<Query>();
            fq.Flatten(booleanQuery, reader, flatQueries);
            assertCollectionQueries(flatQueries, tq(boost, "A"), pqF(boost, "B", "C"));
        }

        [Test]
        public void TestFlattenTermAndPhrase2gram()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term(F, "AA")), Occur.MUST);
            query.Add(toPhraseQuery(analyze("BCD", F, analyzerB), F), Occur.MUST);
            query.Add(toPhraseQuery(analyze("EFGH", F, analyzerB), F), Occur.SHOULD);

            FieldQuery fq = new FieldQuery(query, true, true);
            ISet<Query> flatQueries = new JCG.HashSet<Query>();
            fq.Flatten(query, reader, flatQueries);
            assertCollectionQueries(flatQueries, tq("AA"), pqF("BC", "CD"), pqF("EF", "FG", "GH"));
        }


        [Test]
        public void TestFlatten1TermPhrase()
        {
            Query query = pqF("A");
            FieldQuery fq = new FieldQuery(query, true, true);
            ISet<Query> flatQueries = new JCG.HashSet<Query>();
            fq.Flatten(query, reader, flatQueries);
            assertCollectionQueries(flatQueries, tq("A"));
        }

        [Test]
        public void TestExpand()
        {
            Query dummy = pqF("DUMMY");
            FieldQuery fq = new FieldQuery(dummy, true, true);

            // "a b","b c" => "a b","b c","a b c"
            ISet<Query> flatQueries = new JCG.HashSet<Query>();
            flatQueries.Add(pqF("a", "b"));
            flatQueries.Add(pqF("b", "c"));
            assertCollectionQueries(fq.Expand(flatQueries),
                pqF("a", "b"), pqF("b", "c"), pqF("a", "b", "c"));

            // "a b","b c d" => "a b","b c d","a b c d"
            flatQueries = new JCG.HashSet<Query>();
            flatQueries.Add(pqF("a", "b"));
            flatQueries.Add(pqF("b", "c", "d"));
            assertCollectionQueries(fq.Expand(flatQueries),
                pqF("a", "b"), pqF("b", "c", "d"), pqF("a", "b", "c", "d"));

            // "a b c","b c d" => "a b c","b c d","a b c d"
            flatQueries = new JCG.HashSet<Query>();
            flatQueries.Add(pqF("a", "b", "c"));
            flatQueries.Add(pqF("b", "c", "d"));
            assertCollectionQueries(fq.Expand(flatQueries),
                pqF("a", "b", "c"), pqF("b", "c", "d"), pqF("a", "b", "c", "d"));

            // "a b c","c d e" => "a b c","c d e","a b c d e"
            flatQueries = new JCG.HashSet<Query>();
            flatQueries.Add(pqF("a", "b", "c"));
            flatQueries.Add(pqF("c", "d", "e"));
            assertCollectionQueries(fq.Expand(flatQueries),
                pqF("a", "b", "c"), pqF("c", "d", "e"), pqF("a", "b", "c", "d", "e"));

            // "a b c d","b c" => "a b c d","b c"
            flatQueries = new JCG.HashSet<Query>();
            flatQueries.Add(pqF("a", "b", "c", "d"));
            flatQueries.Add(pqF("b", "c"));
            assertCollectionQueries(fq.Expand(flatQueries),
                pqF("a", "b", "c", "d"), pqF("b", "c"));

            // "a b b","b c" => "a b b","b c","a b b c"
            flatQueries = new JCG.HashSet<Query>();
            flatQueries.Add(pqF("a", "b", "b"));
            flatQueries.Add(pqF("b", "c"));
            assertCollectionQueries(fq.Expand(flatQueries),
                pqF("a", "b", "b"), pqF("b", "c"), pqF("a", "b", "b", "c"));

            // "a b","b a" => "a b","b a","a b a", "b a b"
            flatQueries = new JCG.HashSet<Query>();
            flatQueries.Add(pqF("a", "b"));
            flatQueries.Add(pqF("b", "a"));
            assertCollectionQueries(fq.Expand(flatQueries),
                pqF("a", "b"), pqF("b", "a"), pqF("a", "b", "a"), pqF("b", "a", "b"));

            // "a b","a b c" => "a b","a b c"
            flatQueries = new JCG.HashSet<Query>();
            flatQueries.Add(pqF("a", "b"));
            flatQueries.Add(pqF("a", "b", "c"));
            assertCollectionQueries(fq.Expand(flatQueries),
                pqF("a", "b"), pqF("a", "b", "c"));
        }

        [Test]
        public void TestNoExpand()
        {
            Query dummy = pqF("DUMMY");
            FieldQuery fq = new FieldQuery(dummy, true, true);

            // "a b","c d" => "a b","c d"
            ISet<Query> flatQueries = new JCG.HashSet<Query>();
            flatQueries.Add(pqF("a", "b"));
            flatQueries.Add(pqF("c", "d"));
            assertCollectionQueries(fq.Expand(flatQueries),
                pqF("a", "b"), pqF("c", "d"));

            // "a","a b" => "a", "a b"
            flatQueries = new JCG.HashSet<Query>();
            flatQueries.Add(tq("a"));
            flatQueries.Add(pqF("a", "b"));
            assertCollectionQueries(fq.Expand(flatQueries),
                tq("a"), pqF("a", "b"));

            // "a b","b" => "a b", "b"
            flatQueries = new JCG.HashSet<Query>();
            flatQueries.Add(pqF("a", "b"));
            flatQueries.Add(tq("b"));
            assertCollectionQueries(fq.Expand(flatQueries),
                pqF("a", "b"), tq("b"));

            // "a b c","b c" => "a b c","b c"
            flatQueries = new JCG.HashSet<Query>();
            flatQueries.Add(pqF("a", "b", "c"));
            flatQueries.Add(pqF("b", "c"));
            assertCollectionQueries(fq.Expand(flatQueries),
                pqF("a", "b", "c"), pqF("b", "c"));

            // "a b","a b c" => "a b","a b c"
            flatQueries = new JCG.HashSet<Query>();
            flatQueries.Add(pqF("a", "b"));
            flatQueries.Add(pqF("a", "b", "c"));
            assertCollectionQueries(fq.Expand(flatQueries),
                pqF("a", "b"), pqF("a", "b", "c"));

            // "a b c","b d e" => "a b c","b d e"
            flatQueries = new JCG.HashSet<Query>();
            flatQueries.Add(pqF("a", "b", "c"));
            flatQueries.Add(pqF("b", "d", "e"));
            assertCollectionQueries(fq.Expand(flatQueries),
                pqF("a", "b", "c"), pqF("b", "d", "e"));
        }

        [Test]
        public void TestExpandNotFieldMatch()
        {
            Query dummy = pqF("DUMMY");
            FieldQuery fq = new FieldQuery(dummy, true, false);

            // f1:"a b",f2:"b c" => f1:"a b",f2:"b c",f1:"a b c"
            ISet<Query> flatQueries = new JCG.HashSet<Query>();
            flatQueries.Add(pq(F1, "a", "b"));
            flatQueries.Add(pq(F2, "b", "c"));
            assertCollectionQueries(fq.Expand(flatQueries),
                pq(F1, "a", "b"), pq(F2, "b", "c"), pq(F1, "a", "b", "c"));
        }

        [Test]
        public void TestGetFieldTermMap()
        {
            Query query = tq("a");
            FieldQuery fq = new FieldQuery(query, true, true);

            FieldQuery.QueryPhraseMap pqm = fq.GetFieldTermMap(F, "a");
            assertNotNull(pqm);
            assertTrue(pqm.IsTerminal);

            pqm = fq.GetFieldTermMap(F, "b");
            assertNull(pqm);

            pqm = fq.GetFieldTermMap(F1, "a");
            assertNull(pqm);
        }

        [Test]
        public void TestGetRootMap()
        {
            Query dummy = pqF("DUMMY");
            FieldQuery fq = new FieldQuery(dummy, true, true);

            QueryPhraseMap rootMap1 = fq.GetRootMap(tq("a"));
            QueryPhraseMap rootMap2 = fq.GetRootMap(tq("a"));
            assertTrue(rootMap1 == rootMap2);
            QueryPhraseMap rootMap3 = fq.GetRootMap(tq("b"));
            assertTrue(rootMap1 == rootMap3);
            QueryPhraseMap rootMap4 = fq.GetRootMap(tq(F1, "b"));
            assertFalse(rootMap4 == rootMap3);
        }

        [Test]
        public void TestGetRootMapNotFieldMatch()
        {
            Query dummy = pqF("DUMMY");
            FieldQuery fq = new FieldQuery(dummy, true, false);

            QueryPhraseMap rootMap1 = fq.GetRootMap(tq("a"));
            QueryPhraseMap rootMap2 = fq.GetRootMap(tq("a"));
            assertTrue(rootMap1 == rootMap2);
            QueryPhraseMap rootMap3 = fq.GetRootMap(tq("b"));
            assertTrue(rootMap1 == rootMap3);
            QueryPhraseMap rootMap4 = fq.GetRootMap(tq(F1, "b"));
            assertTrue(rootMap4 == rootMap3);
        }

        [Test]
        public void TestGetTermSet()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term(F, "A")), Occur.MUST);
            query.Add(new TermQuery(new Term(F, "B")), Occur.MUST);
            query.Add(new TermQuery(new Term("x", "C")), Occur.SHOULD);

            BooleanQuery innerQuery = new BooleanQuery();
            innerQuery.Add(new TermQuery(new Term(F, "D")), Occur.MUST);
            innerQuery.Add(new TermQuery(new Term(F, "E")), Occur.MUST);
            query.Add(innerQuery, Occur.MUST_NOT);

            FieldQuery fq = new FieldQuery(query, true, true);
            assertEquals(2, fq.termSetMap.size());
            ISet<String> termSet = fq.GetTermSet(F);
            assertEquals(2, termSet.size());
            assertTrue(termSet.contains("A"));
            assertTrue(termSet.contains("B"));
            termSet = fq.GetTermSet("x");
            assertEquals(1, termSet.size());
            assertTrue(termSet.contains("C"));
            termSet = fq.GetTermSet("y");
            assertNull(termSet);
        }

        [Test]
        public void TestQueryPhraseMap1Term()
        {
            // LUCENENET specific - altered some of the tests because
            // dictionaries throw KeyNotFoundException rather than returning null.

            Query query = tq("a");

            // phraseHighlight = true, fieldMatch = true
            FieldQuery fq = new FieldQuery(query, true, true);
            IDictionary<String, QueryPhraseMap> map = fq.rootMaps;
            assertEquals(1, map.size());
            assertFalse(map.TryGetValue(null, out _)); // assertNull(map[null]);
            assertNotNull(map[F]);
            QueryPhraseMap qpm = map[F];
            assertEquals(1, qpm.subMap.size());
            assertTrue(qpm.subMap["a"] != null);
            assertTrue(qpm.subMap["a"].terminal);
            assertEquals(1F, qpm.subMap["a"].boost, 0);

            // phraseHighlight = true, fieldMatch = false
            fq = new FieldQuery(query, true, false);
            map = fq.rootMaps;
            assertEquals(1, map.size());
            assertFalse(map.TryGetValue(F, out _)); // assertNull(map[F]);
            assertNotNull(map[null]);
            qpm = map[null];
            assertEquals(1, qpm.subMap.size());
            assertTrue(qpm.subMap["a"] != null);
            assertTrue(qpm.subMap["a"].terminal);
            assertEquals(1F, qpm.subMap["a"].boost, 0);

            // phraseHighlight = false, fieldMatch = true
            fq = new FieldQuery(query, false, true);
            map = fq.rootMaps;
            assertEquals(1, map.size());
            assertFalse(map.TryGetValue(null, out _)); // assertNull(map[null]);
            assertNotNull(map[F]);
            qpm = map[F];
            assertEquals(1, qpm.subMap.size());
            assertTrue(qpm.subMap["a"] != null);
            assertTrue(qpm.subMap["a"].terminal);
            assertEquals(1F, qpm.subMap["a"].boost, 0);

            // phraseHighlight = false, fieldMatch = false
            fq = new FieldQuery(query, false, false);
            map = fq.rootMaps;
            assertEquals(1, map.size());
            assertFalse(map.TryGetValue(F, out _)); // assertNull(map[F]);
            assertNotNull(map[null]);
            qpm = map[null];
            assertEquals(1, qpm.subMap.size());
            assertTrue(qpm.subMap["a"] != null);
            assertTrue(qpm.subMap["a"].terminal);
            assertEquals(1F, qpm.subMap["a"].boost, 0);

            // boost != 1
            query = tq(2, "a");
            fq = new FieldQuery(query, true, true);
            map = fq.rootMaps;
            qpm = map[F];
            assertEquals(2F, qpm.subMap["a"].boost, 0);
        }

        [Test]
        public void TestQueryPhraseMap1Phrase()
        {
            // LUCENENET specific - altered some of the tests because
            // dictionaries throw KeyNotFoundException rather than returning null.

            Query query = pqF("a", "b");

            // phraseHighlight = true, fieldMatch = true
            FieldQuery fq = new FieldQuery(query, true, true);
            IDictionary<String, QueryPhraseMap> map = fq.rootMaps;
            assertEquals(1, map.size());
            assertFalse(map.TryGetValue(null, out _)); //assertNull(map[null]);
            assertNotNull(map[F]);
            QueryPhraseMap qpm = map[F];
            assertEquals(1, qpm.subMap.size());
            assertNotNull(qpm.subMap["a"]);
            QueryPhraseMap qpm2 = qpm.subMap["a"];
            assertFalse(qpm2.terminal);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["b"]);
            QueryPhraseMap qpm3 = qpm2.subMap["b"];
            assertTrue(qpm3.terminal);
            assertEquals(1F, qpm3.boost, 0);

            // phraseHighlight = true, fieldMatch = false
            fq = new FieldQuery(query, true, false);
            map = fq.rootMaps;
            assertEquals(1, map.size());
            assertFalse(map.TryGetValue(F, out _)); //assertNull(map[F]);
            assertNotNull(map[null]);
            qpm = map[null];
            assertEquals(1, qpm.subMap.size());
            assertNotNull(qpm.subMap["a"]);
            qpm2 = qpm.subMap["a"];
            assertFalse(qpm2.terminal);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["b"]);
            qpm3 = qpm2.subMap["b"];
            assertTrue(qpm3.terminal);
            assertEquals(1F, qpm3.boost, 0);

            // phraseHighlight = false, fieldMatch = true
            fq = new FieldQuery(query, false, true);
            map = fq.rootMaps;
            assertEquals(1, map.size());
            assertFalse(map.TryGetValue(null, out _)); // assertNull(map[null]);
            assertNotNull(map[F]);
            qpm = map[F];
            assertEquals(2, qpm.subMap.size());
            assertNotNull(qpm.subMap["a"]);
            qpm2 = qpm.subMap["a"];
            assertTrue(qpm2.terminal);
            assertEquals(1F, qpm2.boost, 0);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["b"]);
            qpm3 = qpm2.subMap["b"];
            assertTrue(qpm3.terminal);
            assertEquals(1F, qpm3.boost, 0);

            assertNotNull(qpm.subMap["b"]);
            qpm2 = qpm.subMap["b"];
            assertTrue(qpm2.terminal);
            assertEquals(1F, qpm2.boost, 0);

            // phraseHighlight = false, fieldMatch = false
            fq = new FieldQuery(query, false, false);
            map = fq.rootMaps;
            assertEquals(1, map.size());
            assertFalse(map.TryGetValue(F, out _)); // assertNull(map[F]);
            assertNotNull(map[null]);
            qpm = map[null];
            assertEquals(2, qpm.subMap.size());
            assertNotNull(qpm.subMap["a"]);
            qpm2 = qpm.subMap["a"];
            assertTrue(qpm2.terminal);
            assertEquals(1F, qpm2.boost, 0);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["b"]);
            qpm3 = qpm2.subMap["b"];
            assertTrue(qpm3.terminal);
            assertEquals(1F, qpm3.boost, 0);

            assertNotNull(qpm.subMap["b"]);
            qpm2 = qpm.subMap["b"];
            assertTrue(qpm2.terminal);
            assertEquals(1F, qpm2.boost, 0);

            // boost != 1
            query = pqF(2, "a", "b");
            // phraseHighlight = false, fieldMatch = false
            fq = new FieldQuery(query, false, false);
            map = fq.rootMaps;
            qpm = map[null];
            qpm2 = qpm.subMap["a"];
            assertEquals(2F, qpm2.boost, 0);
            qpm3 = qpm2.subMap["b"];
            assertEquals(2F, qpm3.boost, 0);
            qpm2 = qpm.subMap["b"];
            assertEquals(2F, qpm2.boost, 0);
        }

        [Test]
        public void TestQueryPhraseMap1PhraseAnother()
        {
            // LUCENENET specific - altered some of the tests because
            // dictionaries throw KeyNotFoundException rather than returning null.

            Query query = pqF("search", "engines");

            // phraseHighlight = true, fieldMatch = true
            FieldQuery fq = new FieldQuery(query, true, true);
            IDictionary<String, QueryPhraseMap> map = fq.rootMaps;
            assertEquals(1, map.size());
            assertFalse(map.TryGetValue(null, out _)); // assertNull(map[null]);
            assertNotNull(map[F]);
            QueryPhraseMap qpm = map[F];
            assertEquals(1, qpm.subMap.size());
            assertNotNull(qpm.subMap["search"]);
            QueryPhraseMap qpm2 = qpm.subMap["search"];
            assertFalse(qpm2.terminal);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["engines"]);
            QueryPhraseMap qpm3 = qpm2.subMap["engines"];
            assertTrue(qpm3.terminal);
            assertEquals(1F, qpm3.boost, 0);
        }

        [Test]
        public void TestQueryPhraseMap2Phrases()
        {
            // LUCENENET specific - altered some of the tests because
            // dictionaries throw KeyNotFoundException rather than returning null.

            BooleanQuery query = new BooleanQuery();
            query.Add(pqF("a", "b"), Occur.SHOULD);
            query.Add(pqF(2, "c", "d"), Occur.SHOULD);

            // phraseHighlight = true, fieldMatch = true
            FieldQuery fq = new FieldQuery(query, true, true);
            IDictionary<String, QueryPhraseMap> map = fq.rootMaps;
            assertEquals(1, map.size());
            assertFalse(map.TryGetValue(null, out _)); // assertNull(map[null]);
            assertNotNull(map[F]);
            QueryPhraseMap qpm = map[F];
            assertEquals(2, qpm.subMap.size());

            // "a b"
            assertNotNull(qpm.subMap["a"]);
            QueryPhraseMap qpm2 = qpm.subMap["a"];
            assertFalse(qpm2.terminal);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["b"]);
            QueryPhraseMap qpm3 = qpm2.subMap["b"];
            assertTrue(qpm3.terminal);
            assertEquals(1F, qpm3.boost, 0);

            // "c d"^2
            assertNotNull(qpm.subMap["c"]);
            qpm2 = qpm.subMap["c"];
            assertFalse(qpm2.terminal);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["d"]);
            qpm3 = qpm2.subMap["d"];
            assertTrue(qpm3.terminal);
            assertEquals(2F, qpm3.boost, 0);
        }

        [Test]
        public void TestQueryPhraseMap2PhrasesFields()
        {
            // LUCENENET specific - altered some of the tests because
            // dictionaries throw KeyNotFoundException rather than returning null.

            BooleanQuery query = new BooleanQuery();
            query.Add(pq(F1, "a", "b"), Occur.SHOULD);
            query.Add(pq(2F, F2, "c", "d"), Occur.SHOULD);

            // phraseHighlight = true, fieldMatch = true
            FieldQuery fq = new FieldQuery(query, true, true);
            IDictionary<String, QueryPhraseMap> map = fq.rootMaps;
            assertEquals(2, map.size());
            assertFalse(map.TryGetValue(null, out _)); // assertNull(map[null]);

            // "a b"
            assertNotNull(map[F1]);
            QueryPhraseMap qpm = map[F1];
            assertEquals(1, qpm.subMap.size());
            assertNotNull(qpm.subMap["a"]);
            QueryPhraseMap qpm2 = qpm.subMap["a"];
            assertFalse(qpm2.terminal);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["b"]);
            QueryPhraseMap qpm3 = qpm2.subMap["b"];
            assertTrue(qpm3.terminal);
            assertEquals(1F, qpm3.boost, 0);

            // "c d"^2
            assertNotNull(map[F2]);
            qpm = map[F2];
            assertEquals(1, qpm.subMap.size());
            assertNotNull(qpm.subMap["c"]);
            qpm2 = qpm.subMap["c"];
            assertFalse(qpm2.terminal);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["d"]);
            qpm3 = qpm2.subMap["d"];
            assertTrue(qpm3.terminal);
            assertEquals(2F, qpm3.boost, 0);

            // phraseHighlight = true, fieldMatch = false
            fq = new FieldQuery(query, true, false);
            map = fq.rootMaps;
            assertEquals(1, map.size());
            assertFalse(map.TryGetValue(F1, out _)); // assertNull(map[F1]);
            assertFalse(map.TryGetValue(F2, out _)); // assertNull(map[F2]);
            assertNotNull(map[null]);
            qpm = map[null];
            assertEquals(2, qpm.subMap.size());

            // "a b"
            assertNotNull(qpm.subMap["a"]);
            qpm2 = qpm.subMap["a"];
            assertFalse(qpm2.terminal);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["b"]);
            qpm3 = qpm2.subMap["b"];
            assertTrue(qpm3.terminal);
            assertEquals(1F, qpm3.boost, 0);

            // "c d"^2
            assertNotNull(qpm.subMap["c"]);
            qpm2 = qpm.subMap["c"];
            assertFalse(qpm2.terminal);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["d"]);
            qpm3 = qpm2.subMap["d"];
            assertTrue(qpm3.terminal);
            assertEquals(2F, qpm3.boost, 0);
        }

        /*
         * <t>...terminal
         * 
         * a-b-c-<t>
         *     +-d-<t>
         * b-c-d-<t>
         * +-d-<t>
         */
        [Test]
        public void TestQueryPhraseMapOverlapPhrases()
        {
            // LUCENENET specific - altered some of the tests because
            // dictionaries throw KeyNotFoundException rather than returning null.

            BooleanQuery query = new BooleanQuery();
            query.Add(pqF("a", "b", "c"), Occur.SHOULD);
            query.Add(pqF(2, "b", "c", "d"), Occur.SHOULD);
            query.Add(pqF(3, "b", "d"), Occur.SHOULD);

            // phraseHighlight = true, fieldMatch = true
            FieldQuery fq = new FieldQuery(query, true, true);
            IDictionary<String, QueryPhraseMap> map = fq.rootMaps;
            assertEquals(1, map.size());
            assertFalse(map.TryGetValue(null, out _)); // assertNull(map[null]);
            assertNotNull(map[F]);
            QueryPhraseMap qpm = map[F];
            assertEquals(2, qpm.subMap.size());

            // "a b c"
            assertNotNull(qpm.subMap["a"]);
            QueryPhraseMap qpm2 = qpm.subMap["a"];
            assertFalse(qpm2.terminal);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["b"]);
            QueryPhraseMap qpm3 = qpm2.subMap["b"];
            assertFalse(qpm3.terminal);
            assertEquals(1, qpm3.subMap.size());
            assertNotNull(qpm3.subMap["c"]);
            QueryPhraseMap qpm4 = qpm3.subMap["c"];
            assertTrue(qpm4.terminal);
            assertEquals(1F, qpm4.boost, 0);
            assertNotNull(qpm4.subMap["d"]);
            QueryPhraseMap qpm5 = qpm4.subMap["d"];
            assertTrue(qpm5.terminal);
            assertEquals(1F, qpm5.boost, 0);

            // "b c d"^2, "b d"^3
            assertNotNull(qpm.subMap["b"]);
            qpm2 = qpm.subMap["b"];
            assertFalse(qpm2.terminal);
            assertEquals(2, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["c"]);
            qpm3 = qpm2.subMap["c"];
            assertFalse(qpm3.terminal);
            assertEquals(1, qpm3.subMap.size());
            assertNotNull(qpm3.subMap["d"]);
            qpm4 = qpm3.subMap["d"];
            assertTrue(qpm4.terminal);
            assertEquals(2F, qpm4.boost, 0);
            assertNotNull(qpm2.subMap["d"]);
            qpm3 = qpm2.subMap["d"];
            assertTrue(qpm3.terminal);
            assertEquals(3F, qpm3.boost, 0);
        }

        /*
         * <t>...terminal
         * 
         * a-b-<t>
         *   +-c-<t>
         */
        [Test]
        public void TestQueryPhraseMapOverlapPhrases2()
        {
            // LUCENENET specific - altered some of the tests because
            // dictionaries throw KeyNotFoundException rather than returning null.

            BooleanQuery query = new BooleanQuery();
            query.Add(pqF("a", "b"), Occur.SHOULD);
            query.Add(pqF(2, "a", "b", "c"), Occur.SHOULD);

            // phraseHighlight = true, fieldMatch = true
            FieldQuery fq = new FieldQuery(query, true, true);
            IDictionary<String, QueryPhraseMap> map = fq.rootMaps;
            assertEquals(1, map.size());
            assertFalse(map.TryGetValue(null, out _)); // assertNull(map[null]);
            assertNotNull(map[F]);
            QueryPhraseMap qpm = map[F];
            assertEquals(1, qpm.subMap.size());

            // "a b"
            assertNotNull(qpm.subMap["a"]);
            QueryPhraseMap qpm2 = qpm.subMap["a"];
            assertFalse(qpm2.terminal);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["b"]);
            QueryPhraseMap qpm3 = qpm2.subMap["b"];
            assertTrue(qpm3.terminal);
            assertEquals(1F, qpm3.boost, 0);

            // "a b c"^2
            assertEquals(1, qpm3.subMap.size());
            assertNotNull(qpm3.subMap["c"]);
            QueryPhraseMap qpm4 = qpm3.subMap["c"];
            assertTrue(qpm4.terminal);
            assertEquals(2F, qpm4.boost, 0);
        }

        /*
         * <t>...terminal
         * 
         * a-a-a-<t>
         *     +-a-<t>
         *       +-a-<t>
         *         +-a-<t>
         */
        [Test]
        public void TestQueryPhraseMapOverlapPhrases3()
        {
            // LUCENENET specific - altered some of the tests because
            // dictionaries throw KeyNotFoundException rather than returning null.

            BooleanQuery query = new BooleanQuery();
            query.Add(pqF("a", "a", "a", "a"), Occur.SHOULD);
            query.Add(pqF(2, "a", "a", "a"), Occur.SHOULD);

            // phraseHighlight = true, fieldMatch = true
            FieldQuery fq = new FieldQuery(query, true, true);
            IDictionary<String, QueryPhraseMap> map = fq.rootMaps;
            assertEquals(1, map.size());
            assertFalse(map.TryGetValue(null, out _)); // assertNull(map[null]);
            assertNotNull(map[F]);
            QueryPhraseMap qpm = map[F];
            assertEquals(1, qpm.subMap.size());

            // "a a a"
            assertNotNull(qpm.subMap["a"]);
            QueryPhraseMap qpm2 = qpm.subMap["a"];
            assertFalse(qpm2.terminal);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["a"]);
            QueryPhraseMap qpm3 = qpm2.subMap["a"];
            assertFalse(qpm3.terminal);
            assertEquals(1, qpm3.subMap.size());
            assertNotNull(qpm3.subMap["a"]);
            QueryPhraseMap qpm4 = qpm3.subMap["a"];
            assertTrue(qpm4.terminal);

            // "a a a a"
            assertEquals(1, qpm4.subMap.size());
            assertNotNull(qpm4.subMap["a"]);
            QueryPhraseMap qpm5 = qpm4.subMap["a"];
            assertTrue(qpm5.terminal);

            // "a a a a a"
            assertEquals(1, qpm5.subMap.size());
            assertNotNull(qpm5.subMap["a"]);
            QueryPhraseMap qpm6 = qpm5.subMap["a"];
            assertTrue(qpm6.terminal);

            // "a a a a a a"
            assertEquals(1, qpm6.subMap.size());
            assertNotNull(qpm6.subMap["a"]);
            QueryPhraseMap qpm7 = qpm6.subMap["a"];
            assertTrue(qpm7.terminal);
        }

        [Test]
        public void TestQueryPhraseMapOverlap2gram()
        {
            // LUCENENET specific - altered some of the tests because
            // dictionaries throw KeyNotFoundException rather than returning null.

            BooleanQuery query = new BooleanQuery();
            query.Add(toPhraseQuery(analyze("abc", F, analyzerB), F), Occur.MUST);
            query.Add(toPhraseQuery(analyze("bcd", F, analyzerB), F), Occur.MUST);

            // phraseHighlight = true, fieldMatch = true
            FieldQuery fq = new FieldQuery(query, true, true);
            IDictionary<String, QueryPhraseMap> map = fq.rootMaps;
            assertEquals(1, map.size());
            assertFalse(map.TryGetValue(null, out _)); // assertNull(map[null]);
            assertNotNull(map[F]);
            QueryPhraseMap qpm = map[F];
            assertEquals(2, qpm.subMap.size());

            // "ab bc"
            assertNotNull(qpm.subMap["ab"]);
            QueryPhraseMap qpm2 = qpm.subMap["ab"];
            assertFalse(qpm2.terminal);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["bc"]);
            QueryPhraseMap qpm3 = qpm2.subMap["bc"];
            assertTrue(qpm3.terminal);
            assertEquals(1F, qpm3.boost, 0);

            // "ab bc cd"
            assertEquals(1, qpm3.subMap.size());
            assertNotNull(qpm3.subMap["cd"]);
            QueryPhraseMap qpm4 = qpm3.subMap["cd"];
            assertTrue(qpm4.terminal);
            assertEquals(1F, qpm4.boost, 0);

            // "bc cd"
            assertNotNull(qpm.subMap["bc"]);
            qpm2 = qpm.subMap["bc"];
            assertFalse(qpm2.terminal);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["cd"]);
            qpm3 = qpm2.subMap["cd"];
            assertTrue(qpm3.terminal);
            assertEquals(1F, qpm3.boost, 0);

            // phraseHighlight = false, fieldMatch = true
            fq = new FieldQuery(query, false, true);
            map = fq.rootMaps;
            assertEquals(1, map.size());
            assertFalse(map.TryGetValue(null, out _)); // assertNull(map[null]);
            assertNotNull(map[F]);
            qpm = map[F];
            assertEquals(3, qpm.subMap.size());

            // "ab bc"
            assertNotNull(qpm.subMap["ab"]);
            qpm2 = qpm.subMap["ab"];
            assertTrue(qpm2.terminal);
            assertEquals(1F, qpm2.boost, 0);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["bc"]);
            qpm3 = qpm2.subMap["bc"];
            assertTrue(qpm3.terminal);
            assertEquals(1F, qpm3.boost, 0);

            // "ab bc cd"
            assertEquals(1, qpm3.subMap.size());
            assertNotNull(qpm3.subMap["cd"]);
            qpm4 = qpm3.subMap["cd"];
            assertTrue(qpm4.terminal);
            assertEquals(1F, qpm4.boost, 0);

            // "bc cd"
            assertNotNull(qpm.subMap["bc"]);
            qpm2 = qpm.subMap["bc"];
            assertTrue(qpm2.terminal);
            assertEquals(1F, qpm2.boost, 0);
            assertEquals(1, qpm2.subMap.size());
            assertNotNull(qpm2.subMap["cd"]);
            qpm3 = qpm2.subMap["cd"];
            assertTrue(qpm3.terminal);
            assertEquals(1F, qpm3.boost, 0);

            // "cd"
            assertNotNull(qpm.subMap["cd"]);
            qpm2 = qpm.subMap["cd"];
            assertTrue(qpm2.terminal);
            assertEquals(1F, qpm2.boost, 0);
            assertEquals(0, qpm2.subMap.size());
        }

        [Test]
        public void TestSearchPhrase()
        {
            Query query = pqF("a", "b", "c");

            // phraseHighlight = true, fieldMatch = true
            FieldQuery fq = new FieldQuery(query, true, true);

            // "a"
            IList<TermInfo> phraseCandidate = new JCG.List<TermInfo>();
            phraseCandidate.Add(new TermInfo("a", 0, 1, 0, 1));
            assertNull(fq.SearchPhrase(F, phraseCandidate));
            // "a b"
            phraseCandidate.Add(new TermInfo("b", 2, 3, 1, 1));
            assertNull(fq.SearchPhrase(F, phraseCandidate));
            // "a b c"
            phraseCandidate.Add(new TermInfo("c", 4, 5, 2, 1));
            assertNotNull(fq.SearchPhrase(F, phraseCandidate));
            assertNull(fq.SearchPhrase("x", phraseCandidate));

            // phraseHighlight = true, fieldMatch = false
            fq = new FieldQuery(query, true, false);

            // "a b c"
            assertNotNull(fq.SearchPhrase(F, phraseCandidate));
            assertNotNull(fq.SearchPhrase("x", phraseCandidate));

            // phraseHighlight = false, fieldMatch = true
            fq = new FieldQuery(query, false, true);

            // "a"
            phraseCandidate.Clear();
            phraseCandidate.Add(new TermInfo("a", 0, 1, 0, 1));
            assertNotNull(fq.SearchPhrase(F, phraseCandidate));
            // "a b"
            phraseCandidate.Add(new TermInfo("b", 2, 3, 1, 1));
            assertNull(fq.SearchPhrase(F, phraseCandidate));
            // "a b c"
            phraseCandidate.Add(new TermInfo("c", 4, 5, 2, 1));
            assertNotNull(fq.SearchPhrase(F, phraseCandidate));
            assertNull(fq.SearchPhrase("x", phraseCandidate));
        }

        [Test]
        public void TestSearchPhraseSlop()
        {
            // "a b c"~0
            Query query = pqF("a", "b", "c");

            // phraseHighlight = true, fieldMatch = true
            FieldQuery fq = new FieldQuery(query, true, true);

            // "a b c" w/ position-gap = 2
            IList<TermInfo> phraseCandidate = new JCG.List<TermInfo>();
            phraseCandidate.Add(new TermInfo("a", 0, 1, 0, 1));
            phraseCandidate.Add(new TermInfo("b", 2, 3, 2, 1));
            phraseCandidate.Add(new TermInfo("c", 4, 5, 4, 1));
            assertNull(fq.SearchPhrase(F, phraseCandidate));

            // "a b c"~1
            query = pqF(1F, 1, "a", "b", "c");

            // phraseHighlight = true, fieldMatch = true
            fq = new FieldQuery(query, true, true);

            // "a b c" w/ position-gap = 2
            assertNotNull(fq.SearchPhrase(F, phraseCandidate));

            // "a b c" w/ position-gap = 3
            phraseCandidate.Clear();
            phraseCandidate.Add(new TermInfo("a", 0, 1, 0, 1));
            phraseCandidate.Add(new TermInfo("b", 2, 3, 3, 1));
            phraseCandidate.Add(new TermInfo("c", 4, 5, 6, 1));
            assertNull(fq.SearchPhrase(F, phraseCandidate));
        }

        [Test]
        public void TestHighlightQuery()
        {
            makeIndexStrMV();
            defgMultiTermQueryTest(new WildcardQuery(new Term(F, "d*g")));
        }

        [Test]
        public void TestPrefixQuery()
        {
            makeIndexStrMV();
            defgMultiTermQueryTest(new PrefixQuery(new Term(F, "de")));
        }

        [Test]
        public void TestRegexpQuery()
        {
            makeIndexStrMV();
            Term term = new Term(F, "d[a-z].g");
            defgMultiTermQueryTest(new RegexpQuery(term));
        }

        [Test]
        public void TestRangeQuery()
        {
            makeIndexStrMV();
            defgMultiTermQueryTest(new TermRangeQuery(F, new BytesRef("d"), new BytesRef("e"), true, true));
        }

        private void defgMultiTermQueryTest(Query query)
        {
            FieldQuery fq = new FieldQuery(query, reader, true, true);
            QueryPhraseMap qpm = fq.GetFieldTermMap(F, "defg");
            assertNotNull(qpm);
            assertNull(fq.GetFieldTermMap(F, "dog"));
            IList<TermInfo> phraseCandidate = new JCG.List<TermInfo>();
            phraseCandidate.Add(new TermInfo("defg", 0, 12, 0, 1));
            assertNotNull(fq.SearchPhrase(F, phraseCandidate));
        }
        private sealed class TestStopRewriteQueryAnonymousClass : Query
        {
            public override string ToString(string field)
            {
                return "DummyQuery";
            }
        }

        [Test]
        public void TestStopRewrite()
        {
            Query q = new TestStopRewriteQueryAnonymousClass();
            make1d1fIndex("a");
            assertNotNull(reader);
            new FieldQuery(q, reader, true, true);
        }

        private sealed class TestFlattenFilteredQueryFilterAnonymousClass : Filter
        {
            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                return null;
            }
        }

        [Test]
        public void TestFlattenFilteredQuery()
        {
            initBoost();
            Query query = new FilteredQuery(pqF("A"), new TestFlattenFilteredQueryFilterAnonymousClass());
            query.Boost = (boost);
            FieldQuery fq = new FieldQuery(query, true, true);
            ISet<Query> flatQueries = new JCG.HashSet<Query>();
            fq.Flatten(query, reader, flatQueries);
            assertCollectionQueries(flatQueries, tq(boost, "A"));
        }

        [Test]
        public void TestFlattenConstantScoreQuery()
        {
            initBoost();
            Query query = new ConstantScoreQuery(pqF("A"));
            query.Boost = (boost);
            FieldQuery fq = new FieldQuery(query, true, true);
            ISet<Query> flatQueries = new JCG.HashSet<Query>();
            fq.Flatten(query, reader, flatQueries);
            assertCollectionQueries(flatQueries, tq(boost, "A"));
        }
    }
}
