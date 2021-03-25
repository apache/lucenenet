// Lucene version compatibility level 4.8.1
using NUnit.Framework;
using System.IO;

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


    public class TestWordnetSynonymParser : BaseTokenStreamTestCase
    {

        internal string synonymsFile = "s(100000001,1,'woods',n,1,0).\n" + "s(100000001,2,'wood',n,1,0).\n" + "s(100000001,3,'forest',n,1,0).\n" + "s(100000002,1,'wolfish',n,1,0).\n" + "s(100000002,2,'ravenous',n,1,0).\n" + "s(100000003,1,'king',n,1,1).\n" + "s(100000003,2,'baron',n,1,1).\n" + "s(100000004,1,'king''s evil',n,1,1).\n" + "s(100000004,2,'king''s meany',n,1,1).\n";

        [Test]
        public virtual void TestSynonyms()
        {
            WordnetSynonymParser parser = new WordnetSynonymParser(true, true, new MockAnalyzer(Random));
            parser.Parse(new StringReader(synonymsFile));
            SynonymMap map = parser.Build();

            Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new SynonymFilter(tokenizer, map, false));
            });

            /* all expansions */
            AssertAnalyzesTo(analyzer, "Lost in the woods", new string[] { "Lost", "in", "the", "woods", "wood", "forest" }, new int[] { 0, 5, 8, 12, 12, 12 }, new int[] { 4, 7, 11, 17, 17, 17 }, new int[] { 1, 1, 1, 1, 0, 0 });

            /* single quote */
            AssertAnalyzesTo(analyzer, "king", new string[] { "king", "baron" });

            /* multi words */
            AssertAnalyzesTo(analyzer, "king's evil", new string[] { "king's", "king's", "evil", "meany" });
        }
    }
}