using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Miscellaneous
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

    /// <summary>
    /// Testcase for <seealso cref="KeywordMarkerFilter"/>
    /// </summary>
    public class TestKeywordMarkerFilter : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestSetFilterIncrementToken()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 5, true);
            set.add("lucenefox");
            string[] output = new string[] { "the", "quick", "brown", "LuceneFox", "jumps" };
            AssertTokenStreamContents(new LowerCaseFilterMock(new SetKeywordMarkerFilter(new MockTokenizer(new StringReader("The quIck browN LuceneFox Jumps"), MockTokenizer.WHITESPACE, false), set)), output);
            CharArraySet mixedCaseSet = new CharArraySet(TEST_VERSION_CURRENT, AsSet("LuceneFox"), false);
            AssertTokenStreamContents(new LowerCaseFilterMock(new SetKeywordMarkerFilter(new MockTokenizer(new StringReader("The quIck browN LuceneFox Jumps"), MockTokenizer.WHITESPACE, false), mixedCaseSet)), output);
            CharArraySet set2 = set;
            AssertTokenStreamContents(new LowerCaseFilterMock(new SetKeywordMarkerFilter(new MockTokenizer(new StringReader("The quIck browN LuceneFox Jumps"), MockTokenizer.WHITESPACE, false), set2)), output);
        }

        [Test]
        public virtual void TestPatternFilterIncrementToken()
        {
            string[] output = new string[] { "the", "quick", "brown", "LuceneFox", "jumps" };
            AssertTokenStreamContents(new LowerCaseFilterMock(new PatternKeywordMarkerFilter(new MockTokenizer(new StringReader("The quIck browN LuceneFox Jumps"), MockTokenizer.WHITESPACE, false), new Regex("[a-zA-Z]+[fF]ox", RegexOptions.Compiled))), output);

            output = new string[] { "the", "quick", "brown", "lucenefox", "jumps" };

            AssertTokenStreamContents(new LowerCaseFilterMock(new PatternKeywordMarkerFilter(new MockTokenizer(new StringReader("The quIck browN LuceneFox Jumps"), MockTokenizer.WHITESPACE, false), new Regex("[a-zA-Z]+[f]ox", RegexOptions.Compiled))), output);
        }

        // LUCENE-2901
        [Test]
        public virtual void TestComposition()
        {
            TokenStream ts = new LowerCaseFilterMock(new SetKeywordMarkerFilter(new SetKeywordMarkerFilter(new MockTokenizer(new StringReader("Dogs Trees Birds Houses"), MockTokenizer.WHITESPACE, false), new CharArraySet(TEST_VERSION_CURRENT, AsSet("Birds", "Houses"), false)), new CharArraySet(TEST_VERSION_CURRENT, AsSet("Dogs", "Trees"), false)));

            AssertTokenStreamContents(ts, new string[] { "Dogs", "Trees", "Birds", "Houses" });

            ts = new LowerCaseFilterMock(new PatternKeywordMarkerFilter(new PatternKeywordMarkerFilter(new MockTokenizer(new StringReader("Dogs Trees Birds Houses"), MockTokenizer.WHITESPACE, false), new Regex("Birds|Houses", RegexOptions.Compiled)), new Regex("Dogs|Trees", RegexOptions.Compiled)));

            AssertTokenStreamContents(ts, new string[] { "Dogs", "Trees", "Birds", "Houses" });

            ts = new LowerCaseFilterMock(new SetKeywordMarkerFilter(new PatternKeywordMarkerFilter(new MockTokenizer(new StringReader("Dogs Trees Birds Houses"), MockTokenizer.WHITESPACE, false), new Regex("Birds|Houses", RegexOptions.Compiled)), new CharArraySet(TEST_VERSION_CURRENT, AsSet("Dogs", "Trees"), false)));

            AssertTokenStreamContents(ts, new string[] { "Dogs", "Trees", "Birds", "Houses" });
        }

        public sealed class LowerCaseFilterMock : TokenFilter
        {

            internal readonly ICharTermAttribute termAtt;
            internal readonly IKeywordAttribute keywordAttr;

            public LowerCaseFilterMock(TokenStream @in) : base(@in)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                keywordAttr = AddAttribute<IKeywordAttribute>();
            }

            public override bool IncrementToken()
            {
                if (input.IncrementToken())
                {
                    if (!keywordAttr.Keyword)
                    {
                        string term = CultureInfo.InvariantCulture.TextInfo.ToLower(termAtt.ToString());
                        termAtt.SetEmpty().Append(term);
                    }
                    return true;
                }
                return false;
            }

        }
    }
}