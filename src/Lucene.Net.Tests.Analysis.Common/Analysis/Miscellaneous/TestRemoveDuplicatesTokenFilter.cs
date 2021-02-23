// Lucene version compatibility level 4.8.1
using J2N.Text;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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


    public class TestRemoveDuplicatesTokenFilter : BaseTokenStreamTestCase
    {

        public static Token Tok(int pos, string t, int start, int end)
        {
            Token tok = new Token(t, start, end);
            tok.PositionIncrement = pos;
            return tok;
        }
        public static Token Tok(int pos, string t)
        {
            return Tok(pos, t, 0, 0);
        }

        public virtual void TestDups(string expected, params Token[] tokens)
        {
            IEnumerator<Token> toks = ((IEnumerable<Token>)tokens).GetEnumerator();
            TokenStream ts = new RemoveDuplicatesTokenFilter((new TokenStreamAnonymousClass(toks)));

            AssertTokenStreamContents(ts, Regex.Split(expected, "\\s").TrimEnd());
        }

        private sealed class TokenStreamAnonymousClass : TokenStream
        {
            private readonly IEnumerator<Token> toks;

            public TokenStreamAnonymousClass(IEnumerator<Token> toks)
            {
                this.toks = toks;
                termAtt = AddAttribute<ICharTermAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();
                posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            }

            internal ICharTermAttribute termAtt;
            internal IOffsetAttribute offsetAtt;
            internal IPositionIncrementAttribute posIncAtt;
            public override sealed bool IncrementToken()
            {
                if (toks.MoveNext())
                {
                    ClearAttributes();

                    Token tok = toks.Current;
                    termAtt.SetEmpty().Append(tok);
                    offsetAtt.SetOffset(tok.StartOffset, tok.EndOffset);
                    posIncAtt.PositionIncrement = tok.PositionIncrement;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        [Test]
        public virtual void TestNoDups()
        {

            TestDups("A B B C D E", Tok(1, "A", 0, 4), Tok(1, "B", 5, 10), Tok(1, "B", 11, 15), Tok(1, "C", 16, 20), Tok(0, "D", 16, 20), Tok(1, "E", 21, 25));

        }


        [Test]
        public virtual void TestSimpleDups()
        {

            TestDups("A B C D E", Tok(1, "A", 0, 4), Tok(1, "B", 5, 10), Tok(0, "B", 11, 15), Tok(1, "C", 16, 20), Tok(0, "D", 16, 20), Tok(1, "E", 21, 25));

        }

        [Test]
        public virtual void TestComplexDups()
        {

            TestDups("A B C D E F G H I J K", Tok(1, "A"), Tok(1, "B"), Tok(0, "B"), Tok(1, "C"), Tok(1, "D"), Tok(0, "D"), Tok(0, "D"), Tok(1, "E"), Tok(1, "F"), Tok(0, "F"), Tok(1, "G"), Tok(0, "H"), Tok(0, "H"), Tok(1, "I"), Tok(1, "J"), Tok(0, "K"), Tok(0, "J"));

        }

        // some helper methods for the below test with synonyms
        private string RandomNonEmptyString()
        {
            while (true)
            {
                string s = TestUtil.RandomUnicodeString(Random).Trim();
                if (s.Length != 0 && s.IndexOf('\u0000') == -1)
                {
                    return s;
                }
            }
        }

        private void Add(SynonymMap.Builder b, string input, string output, bool keepOrig)
        {
            b.Add(new CharsRef(Regex.Replace(input, " +", "\u0000")), new CharsRef(Regex.Replace(output, " +", "\u0000")), keepOrig);
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        [Slow]
        public virtual void TestRandomStrings()
        {
            int numIters = AtLeast(10);
            for (int i = 0; i < numIters; i++)
            {
                SynonymMap.Builder b = new SynonymMap.Builder(Random.nextBoolean());
                int numEntries = AtLeast(10);
                for (int j = 0; j < numEntries; j++)
                {
                    Add(b, RandomNonEmptyString(), RandomNonEmptyString(), Random.nextBoolean());
                }
                SynonymMap map = b.Build();
                bool ignoreCase = Random.nextBoolean();

                Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
                {
                    Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.SIMPLE, true);
                    TokenStream stream = new SynonymFilter(tokenizer, map, ignoreCase);
                    return new TokenStreamComponents(tokenizer, new RemoveDuplicatesTokenFilter(stream));
                });

                CheckRandomData(Random, analyzer, 200);
            }
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new RemoveDuplicatesTokenFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}