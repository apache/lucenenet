using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Search.Suggest.Analyzing
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

    public class BlendedInfixSuggesterTest : LuceneTestCase
    {
        /**
        * Test the weight transformation depending on the position
        * of the matching term.
        */
        [Test]
        public void TestBlendedSort()
        {

            BytesRef payload = new BytesRef("star");

            Input[] keys = new Input[]{
                new Input("star wars: episode v - the empire strikes back", 8, payload)
            };

            DirectoryInfo tempDir = CreateTempDir("BlendedInfixSuggesterTest");

            Analyzer a = new StandardAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty);
            BlendedInfixSuggester suggester = new BlendedInfixSuggester(TEST_VERSION_CURRENT, NewFSDirectory(tempDir), a, a,
                                                                        AnalyzingInfixSuggester.DEFAULT_MIN_PREFIX_CHARS,
                                                                        BlendedInfixSuggester.BlenderType.POSITION_LINEAR,
                                                                        BlendedInfixSuggester.DEFAULT_NUM_FACTOR);     //LUCENENET TODO: add extra false param at version 4.11.0
            suggester.Build(new InputArrayEnumerator(keys));

            // we query for star wars and check that the weight
            // is smaller when we search for tokens that are far from the beginning

            long w0 = GetInResults(suggester, "star ", payload, 1);
            long w1 = GetInResults(suggester, "war", payload, 1);
            long w2 = GetInResults(suggester, "empire ba", payload, 1);
            long w3 = GetInResults(suggester, "back", payload, 1);
            long w4 = GetInResults(suggester, "bacc", payload, 1);

            assertTrue(w0 > w1);
            assertTrue(w1 > w2);
            assertTrue(w2 > w3);

            assertTrue(w4 < 0);

            suggester.Dispose();
        }

        /**
         * Verify the different flavours of the blender types
         */
        [Test]
        public void TestBlendingType()
        {

            BytesRef pl = new BytesRef("lake");
            long w = 20;

            Input[] keys = new Input[]{
                new Input("top of the lake", w, pl)
            };

            DirectoryInfo tempDir = CreateTempDir("BlendedInfixSuggesterTest");
            Analyzer a = new StandardAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty);

            // BlenderType.LINEAR is used by default (remove position*10%)
            BlendedInfixSuggester suggester = new BlendedInfixSuggester(TEST_VERSION_CURRENT, NewFSDirectory(tempDir), a);
            suggester.Build(new InputArrayEnumerator(keys));

            assertEquals(w, GetInResults(suggester, "top", pl, 1));
            assertEquals((int)(w * (1 - 0.10 * 2)), GetInResults(suggester, "the", pl, 1));
            assertEquals((int)(w * (1 - 0.10 * 3)), GetInResults(suggester, "lake", pl, 1));

            suggester.Dispose();

            // BlenderType.RECIPROCAL is using 1/(1+p) * w where w is weight and p the position of the word
            suggester = new BlendedInfixSuggester(TEST_VERSION_CURRENT, NewFSDirectory(tempDir), a, a,
                                                  AnalyzingInfixSuggester.DEFAULT_MIN_PREFIX_CHARS,
                                                  BlendedInfixSuggester.BlenderType.POSITION_RECIPROCAL, 1);    //LUCENENET TODO: add extra false param at version 4.11.0
            suggester.Build(new InputArrayEnumerator(keys));

            assertEquals(w, GetInResults(suggester, "top", pl, 1));
            assertEquals((int)(w * 1 / (1 + 2)), GetInResults(suggester, "the", pl, 1));
            assertEquals((int)(w * 1 / (1 + 3)), GetInResults(suggester, "lake", pl, 1));

            suggester.Dispose();
        }

        /**
         * Assert that the factor is important to get results that might be lower in term of weight but
         * would be pushed up after the blending transformation
         */
        [Test]
        public void TestRequiresMore()
        {

            BytesRef lake = new BytesRef("lake");
            BytesRef star = new BytesRef("star");
            BytesRef ret = new BytesRef("ret");

            Input[] keys = new Input[]{
                new Input("top of the lake", 18, lake),
                new Input("star wars: episode v - the empire strikes back", 12, star),
                new Input("the returned", 10, ret),
            };

            DirectoryInfo tempDir = CreateTempDir("BlendedInfixSuggesterTest");
            Analyzer a = new StandardAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty);

            // if factor is small, we don't get the expected element
            BlendedInfixSuggester suggester = new BlendedInfixSuggester(TEST_VERSION_CURRENT, NewFSDirectory(tempDir), a, a,
                                                                        AnalyzingInfixSuggester.DEFAULT_MIN_PREFIX_CHARS,
                                                                        BlendedInfixSuggester.BlenderType.POSITION_RECIPROCAL, 1);     //LUCENENET TODO: add extra false param at version 4.11.0

            suggester.Build(new InputArrayEnumerator(keys));


            // we don't find it for in the 2 first
            assertEquals(2, suggester.DoLookup("the", null, 2, true, false).size());
            long w0 = GetInResults(suggester, "the", ret, 2);
            assertTrue(w0 < 0);

            // but it's there if we search for 3 elements
            assertEquals(3, suggester.DoLookup("the", null, 3, true, false).size());
            long w1 = GetInResults(suggester, "the", ret, 3);
            assertTrue(w1 > 0);

            suggester.Dispose();

            // if we increase the factor we have it
            suggester = new BlendedInfixSuggester(TEST_VERSION_CURRENT, NewFSDirectory(tempDir), a, a,
                                                  AnalyzingInfixSuggester.DEFAULT_MIN_PREFIX_CHARS,
                                                  BlendedInfixSuggester.BlenderType.POSITION_RECIPROCAL, 2);     //LUCENENET TODO: add extra false param at version 4.11.0
            suggester.Build(new InputArrayEnumerator(keys));

            // we have it
            long w2 = GetInResults(suggester, "the", ret, 2);
            assertTrue(w2 > 0);

            // but we don't have the other
            long w3 = GetInResults(suggester, "the", star, 2);
            assertTrue(w3 < 0);

            suggester.Dispose();
        }
        
        /**
        * Handle trailing spaces that result in no prefix token LUCENE-6093
        */
        [Test]
        public void TestNullPrefixToken()
        {
            BytesRef payload = new BytesRef("lake");

            Input[] keys = new Input[] {
                new Input("top of the lake", 8, payload)
            };

            DirectoryInfo tempDir = CreateTempDir("BlendedInfixSuggesterTest");

            Analyzer a = new StandardAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty);
            BlendedInfixSuggester suggester = new BlendedInfixSuggester(TEST_VERSION_CURRENT, NewFSDirectory(tempDir), a, a,
                                                                        AnalyzingInfixSuggester.DEFAULT_MIN_PREFIX_CHARS,
                                                                        BlendedInfixSuggester.BlenderType.POSITION_LINEAR,
                                                                        BlendedInfixSuggester.DEFAULT_NUM_FACTOR);     //LUCENENET TODO: add extra false param at version 4.11.0
            suggester.Build(new InputArrayEnumerator(keys));

            GetInResults(suggester, "of ", payload, 1);
            GetInResults(suggester, "the ", payload, 1);
            GetInResults(suggester, "lake ", payload, 1);

            suggester.Dispose();
        }

        [Test]
        public void TestTrying()
        {

            BytesRef lake = new BytesRef("lake");
            BytesRef star = new BytesRef("star");
            BytesRef ret = new BytesRef("ret");

            Input[] keys = new Input[]{
                new Input("top of the lake", 15, lake),
                new Input("star wars: episode v - the empire strikes back", 12, star),
                new Input("the returned", 10, ret),
            };

            DirectoryInfo tempDir = CreateTempDir("BlendedInfixSuggesterTest");
            Analyzer a = new StandardAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty);

            // if factor is small, we don't get the expected element
            BlendedInfixSuggester suggester = new BlendedInfixSuggester(TEST_VERSION_CURRENT, NewFSDirectory(tempDir), a, a,
                                                                        AnalyzingInfixSuggester.DEFAULT_MIN_PREFIX_CHARS,
                                                                        BlendedInfixSuggester.BlenderType.POSITION_RECIPROCAL,
                                                                        BlendedInfixSuggester.DEFAULT_NUM_FACTOR);     //LUCENENET TODO: add extra false param at version 4.11.0
            suggester.Build(new InputArrayEnumerator(keys));


            IList<Lookup.LookupResult> responses = suggester.DoLookup("the", null, 4, true, false);

            foreach (Lookup.LookupResult response in responses)
            {
                Console.WriteLine(response);
            }

            suggester.Dispose();
        }


        private static long GetInResults(BlendedInfixSuggester suggester, string prefix, BytesRef payload, int num)
        {

            IList<Lookup.LookupResult> responses = suggester.DoLookup(prefix, null, num, true, false);

            foreach (Lookup.LookupResult response in responses)
            {
                if (response.Payload.equals(payload))
                {
                    return response.Value;
                }
            }

            return -1;
        }
    }
}
