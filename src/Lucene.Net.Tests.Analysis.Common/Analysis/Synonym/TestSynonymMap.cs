// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.NGram;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Synonym
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

    /// @deprecated Remove this test in Lucene 5.0 
    [Obsolete("Remove this test in Lucene 5.0")]
    public class TestSynonymMap : LuceneTestCase
    {

        [Test]
        public virtual void TestInvalidMappingRules()
        {
            SlowSynonymMap synMap = new SlowSynonymMap(true);
            IList<string> rules = new JCG.List<string>(1);
            rules.Add("a=>b=>c");
            try
            {
                SlowSynonymFilterFactory.ParseRules(rules, synMap, "=>", ",", true, null);
                fail("IllegalArgumentException must be thrown.");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
            }
        }

        [Test]
        public virtual void TestReadMappingRules()
        {
            SlowSynonymMap synMap;

            // (a)->[b]
            IList<string> rules = new JCG.List<string>();
            rules.Add("a=>b");
            synMap = new SlowSynonymMap(true);
            SlowSynonymFilterFactory.ParseRules(rules, synMap, "=>", ",", true, null);
            assertEquals(1, synMap.Submap.size());
            AssertTokIncludes(synMap, "a", "b");

            // (a)->[c]
            // (b)->[c]
            rules.Clear();
            rules.Add("a,b=>c");
            synMap = new SlowSynonymMap(true);
            SlowSynonymFilterFactory.ParseRules(rules, synMap, "=>", ",", true, null);
            assertEquals(2, synMap.Submap.size());
            AssertTokIncludes(synMap, "a", "c");
            AssertTokIncludes(synMap, "b", "c");

            // (a)->[b][c]
            rules.Clear();
            rules.Add("a=>b,c");
            synMap = new SlowSynonymMap(true);
            SlowSynonymFilterFactory.ParseRules(rules, synMap, "=>", ",", true, null);
            assertEquals(1, synMap.Submap.size());
            AssertTokIncludes(synMap, "a", "b");
            AssertTokIncludes(synMap, "a", "c");

            // (a)->(b)->[a2]
            //      [a1]
            rules.Clear();
            rules.Add("a=>a1");
            rules.Add("a b=>a2");
            synMap = new SlowSynonymMap(true);
            SlowSynonymFilterFactory.ParseRules(rules, synMap, "=>", ",", true, null);
            assertEquals(1, synMap.Submap.size());
            AssertTokIncludes(synMap, "a", "a1");
            assertEquals(1, GetSubSynonymMap(synMap, "a").Submap.size());
            AssertTokIncludes(GetSubSynonymMap(synMap, "a"), "b", "a2");

            // (a)->(b)->[a2]
            //      (c)->[a3]
            //      [a1]
            rules.Clear();
            rules.Add("a=>a1");
            rules.Add("a b=>a2");
            rules.Add("a c=>a3");
            synMap = new SlowSynonymMap(true);
            SlowSynonymFilterFactory.ParseRules(rules, synMap, "=>", ",", true, null);
            assertEquals(1, synMap.Submap.size());
            AssertTokIncludes(synMap, "a", "a1");
            assertEquals(2, GetSubSynonymMap(synMap, "a").Submap.size());
            AssertTokIncludes(GetSubSynonymMap(synMap, "a"), "b", "a2");
            AssertTokIncludes(GetSubSynonymMap(synMap, "a"), "c", "a3");

            // (a)->(b)->[a2]
            //      [a1]
            // (b)->(c)->[b2]
            //      [b1]
            rules.Clear();
            rules.Add("a=>a1");
            rules.Add("a b=>a2");
            rules.Add("b=>b1");
            rules.Add("b c=>b2");
            synMap = new SlowSynonymMap(true);
            SlowSynonymFilterFactory.ParseRules(rules, synMap, "=>", ",", true, null);
            assertEquals(2, synMap.Submap.size());
            AssertTokIncludes(synMap, "a", "a1");
            assertEquals(1, GetSubSynonymMap(synMap, "a").Submap.size());
            AssertTokIncludes(GetSubSynonymMap(synMap, "a"), "b", "a2");
            AssertTokIncludes(synMap, "b", "b1");
            assertEquals(1, GetSubSynonymMap(synMap, "b").Submap.size());
            AssertTokIncludes(GetSubSynonymMap(synMap, "b"), "c", "b2");
        }

        [Test]
        public virtual void TestRead1waySynonymRules()
        {
            SlowSynonymMap synMap;

            // (a)->[a]
            // (b)->[a]
            IList<string> rules = new JCG.List<string>();
            rules.Add("a,b");
            synMap = new SlowSynonymMap(true);
            SlowSynonymFilterFactory.ParseRules(rules, synMap, "=>", ",", false, null);
            assertEquals(2, synMap.Submap.size());
            AssertTokIncludes(synMap, "a", "a");
            AssertTokIncludes(synMap, "b", "a");

            // (a)->[a]
            // (b)->[a]
            // (c)->[a]
            rules.Clear();
            rules.Add("a,b,c");
            synMap = new SlowSynonymMap(true);
            SlowSynonymFilterFactory.ParseRules(rules, synMap, "=>", ",", false, null);
            assertEquals(3, synMap.Submap.size());
            AssertTokIncludes(synMap, "a", "a");
            AssertTokIncludes(synMap, "b", "a");
            AssertTokIncludes(synMap, "c", "a");

            // (a)->[a]
            // (b1)->(b2)->[a]
            rules.Clear();
            rules.Add("a,b1 b2");
            synMap = new SlowSynonymMap(true);
            SlowSynonymFilterFactory.ParseRules(rules, synMap, "=>", ",", false, null);
            assertEquals(2, synMap.Submap.size());
            AssertTokIncludes(synMap, "a", "a");
            assertEquals(1, GetSubSynonymMap(synMap, "b1").Submap.size());
            AssertTokIncludes(GetSubSynonymMap(synMap, "b1"), "b2", "a");

            // (a1)->(a2)->[a1][a2]
            // (b)->[a1][a2]
            rules.Clear();
            rules.Add("a1 a2,b");
            synMap = new SlowSynonymMap(true);
            SlowSynonymFilterFactory.ParseRules(rules, synMap, "=>", ",", false, null);
            assertEquals(2, synMap.Submap.size());
            assertEquals(1, GetSubSynonymMap(synMap, "a1").Submap.size());
            AssertTokIncludes(GetSubSynonymMap(synMap, "a1"), "a2", "a1");
            AssertTokIncludes(GetSubSynonymMap(synMap, "a1"), "a2", "a2");
            AssertTokIncludes(synMap, "b", "a1");
            AssertTokIncludes(synMap, "b", "a2");
        }

        [Test]
        public virtual void TestRead2waySynonymRules()
        {
            SlowSynonymMap synMap;

            // (a)->[a][b]
            // (b)->[a][b]
            IList<string> rules = new JCG.List<string>();
            rules.Add("a,b");
            synMap = new SlowSynonymMap(true);
            SlowSynonymFilterFactory.ParseRules(rules, synMap, "=>", ",", true, null);
            assertEquals(2, synMap.Submap.size());
            AssertTokIncludes(synMap, "a", "a");
            AssertTokIncludes(synMap, "a", "b");
            AssertTokIncludes(synMap, "b", "a");
            AssertTokIncludes(synMap, "b", "b");

            // (a)->[a][b][c]
            // (b)->[a][b][c]
            // (c)->[a][b][c]
            rules.Clear();
            rules.Add("a,b,c");
            synMap = new SlowSynonymMap(true);
            SlowSynonymFilterFactory.ParseRules(rules, synMap, "=>", ",", true, null);
            assertEquals(3, synMap.Submap.size());
            AssertTokIncludes(synMap, "a", "a");
            AssertTokIncludes(synMap, "a", "b");
            AssertTokIncludes(synMap, "a", "c");
            AssertTokIncludes(synMap, "b", "a");
            AssertTokIncludes(synMap, "b", "b");
            AssertTokIncludes(synMap, "b", "c");
            AssertTokIncludes(synMap, "c", "a");
            AssertTokIncludes(synMap, "c", "b");
            AssertTokIncludes(synMap, "c", "c");

            // (a)->[a]
            //      [b1][b2]
            // (b1)->(b2)->[a]
            //             [b1][b2]
            rules.Clear();
            rules.Add("a,b1 b2");
            synMap = new SlowSynonymMap(true);
            SlowSynonymFilterFactory.ParseRules(rules, synMap, "=>", ",", true, null);
            assertEquals(2, synMap.Submap.size());
            AssertTokIncludes(synMap, "a", "a");
            AssertTokIncludes(synMap, "a", "b1");
            AssertTokIncludes(synMap, "a", "b2");
            assertEquals(1, GetSubSynonymMap(synMap, "b1").Submap.size());
            AssertTokIncludes(GetSubSynonymMap(synMap, "b1"), "b2", "a");
            AssertTokIncludes(GetSubSynonymMap(synMap, "b1"), "b2", "b1");
            AssertTokIncludes(GetSubSynonymMap(synMap, "b1"), "b2", "b2");

            // (a1)->(a2)->[a1][a2]
            //             [b]
            // (b)->[a1][a2]
            //      [b]
            rules.Clear();
            rules.Add("a1 a2,b");
            synMap = new SlowSynonymMap(true);
            SlowSynonymFilterFactory.ParseRules(rules, synMap, "=>", ",", true, null);
            assertEquals(2, synMap.Submap.size());
            assertEquals(1, GetSubSynonymMap(synMap, "a1").Submap.size());
            AssertTokIncludes(GetSubSynonymMap(synMap, "a1"), "a2", "a1");
            AssertTokIncludes(GetSubSynonymMap(synMap, "a1"), "a2", "a2");
            AssertTokIncludes(GetSubSynonymMap(synMap, "a1"), "a2", "b");
            AssertTokIncludes(synMap, "b", "a1");
            AssertTokIncludes(synMap, "b", "a2");
            AssertTokIncludes(synMap, "b", "b");
        }

        [Test]
        public virtual void TestBigramTokenizer()
        {
            SlowSynonymMap synMap;

            // prepare bi-gram tokenizer factory
            IDictionary<string, string> args = new Dictionary<string, string>();
            args[AbstractAnalysisFactory.LUCENE_MATCH_VERSION_PARAM] = "4.4";
            args["minGramSize"] = "2";
            args["maxGramSize"] = "2";
            TokenizerFactory tf = new NGramTokenizerFactory(args);

            // (ab)->(bc)->(cd)->[ef][fg][gh]
            IList<string> rules = new JCG.List<string>();
            rules.Add("abcd=>efgh");
            synMap = new SlowSynonymMap(true);
            SlowSynonymFilterFactory.ParseRules(rules, synMap, "=>", ",", true, tf);
            assertEquals(1, synMap.Submap.size());
            assertEquals(1, GetSubSynonymMap(synMap, "ab").Submap.size());
            assertEquals(1, GetSubSynonymMap(GetSubSynonymMap(synMap, "ab"), "bc").Submap.size());
            AssertTokIncludes(GetSubSynonymMap(GetSubSynonymMap(synMap, "ab"), "bc"), "cd", "ef");
            AssertTokIncludes(GetSubSynonymMap(GetSubSynonymMap(synMap, "ab"), "bc"), "cd", "fg");
            AssertTokIncludes(GetSubSynonymMap(GetSubSynonymMap(synMap, "ab"), "bc"), "cd", "gh");
        }


        [Test]
        public virtual void TestLoadRules()
        {
            IDictionary<string, string> args = new Dictionary<string, string>();
            args["synonyms"] = "something.txt";
            SlowSynonymFilterFactory ff = new SlowSynonymFilterFactory(args);
            ff.Inform(new ResourceLoaderAnonymousClass());

            SlowSynonymMap synMap = ff.SynonymMap;
            assertEquals(2, synMap.Submap.size());
            AssertTokIncludes(synMap, "a", "a");
            AssertTokIncludes(synMap, "a", "b");
            AssertTokIncludes(synMap, "b", "a");
            AssertTokIncludes(synMap, "b", "b");
        }

        private sealed class ResourceLoaderAnonymousClass : IResourceLoader
        {
            public T NewInstance<T>(string cname)
            {
                throw RuntimeException.Create("stub");
            }

            public Type FindType(string cname)
            {
                throw RuntimeException.Create("stub");
            }

            public Stream OpenResource(string resource)
            {
                if (!"something.txt".Equals(resource, StringComparison.Ordinal))
                {
                    throw RuntimeException.Create("should not get a differnt resource");
                }
                else
                {
                    return new MemoryStream(Encoding.UTF8.GetBytes("a,b"));
                }
            }
        }

        private void AssertTokIncludes(SlowSynonymMap map, string src, string exp)
        {
            Token[] tokens = map.Submap[src].Synonyms;
            bool inc = false;
            foreach (Token token in tokens)
            {
                if (exp.Equals(new string(token.Buffer, 0, token.Length), StringComparison.Ordinal))
                {
                    inc = true;
                }
            }
            assertTrue(inc);
        }

        private SlowSynonymMap GetSubSynonymMap(SlowSynonymMap map, string src)
        {
            return map.Submap.TryGetValue(src, out SlowSynonymMap result) ? result : null;
        }
    }
}