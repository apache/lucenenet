using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;
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

    public class IndexTimeSynonymTest : AbstractTestCase
    {
        [Test]
        public void TestFieldTermStackIndex1wSearch1term()
        {
            makeIndex1w();

            FieldQuery fq = new FieldQuery(tq("Mac"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            assertEquals(1, stack.termList.size());
            assertEquals("Mac(11,20,3)", stack.Pop().toString());
        }

        [Test]
        public void TestFieldTermStackIndex1wSearch2terms()
        {
            makeIndex1w();

            BooleanQuery bq = new BooleanQuery();
            bq.Add(tq("Mac"), Occur.SHOULD);
            bq.Add(tq("MacBook"), Occur.SHOULD);
            FieldQuery fq = new FieldQuery(bq, true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            assertEquals(1, stack.termList.size());
            TermInfo ti = stack.Pop();
            assertEquals("Mac(11,20,3)", ti.toString());
            assertEquals("MacBook(11,20,3)", ti.Next.toString());
            assertSame(ti, ti.Next.Next);
        }

        [Test]
        public void TestFieldTermStackIndex1w2wSearch1term()
        {
            makeIndex1w2w();

            FieldQuery fq = new FieldQuery(tq("pc"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            assertEquals(1, stack.termList.size());
            assertEquals("pc(3,5,1)", stack.Pop().toString());
        }

        [Test]
        public void TestFieldTermStackIndex1w2wSearch1phrase()
        {
            makeIndex1w2w();

            FieldQuery fq = new FieldQuery(pqF("personal", "computer"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            assertEquals(2, stack.termList.size());
            assertEquals("personal(3,5,1)", stack.Pop().toString());
            assertEquals("computer(3,5,2)", stack.Pop().toString());
        }

        [Test]
        public void TestFieldTermStackIndex1w2wSearch1partial()
        {
            makeIndex1w2w();

            FieldQuery fq = new FieldQuery(tq("computer"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            assertEquals(1, stack.termList.size());
            assertEquals("computer(3,5,2)", stack.Pop().toString());
        }

        [Test]
        public void TestFieldTermStackIndex1w2wSearch1term1phrase()
        {
            makeIndex1w2w();

            BooleanQuery bq = new BooleanQuery();
            bq.Add(tq("pc"), Occur.SHOULD);
            bq.Add(pqF("personal", "computer"), Occur.SHOULD);
            FieldQuery fq = new FieldQuery(bq, true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            assertEquals(2, stack.termList.size());
            TermInfo ti = stack.Pop();
            assertEquals("pc(3,5,1)", ti.toString());
            assertEquals("personal(3,5,1)", ti.Next.toString());
            assertSame(ti, ti.Next.Next);
            assertEquals("computer(3,5,2)", stack.Pop().toString());
        }

        [Test]
        public void TestFieldTermStackIndex2w1wSearch1term()
        {
            makeIndex2w1w();

            FieldQuery fq = new FieldQuery(tq("pc"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            assertEquals(1, stack.termList.size());
            assertEquals("pc(3,20,1)", stack.Pop().toString());
        }

        [Test]
        public void TestFieldTermStackIndex2w1wSearch1phrase()
        {
            makeIndex2w1w();

            FieldQuery fq = new FieldQuery(pqF("personal", "computer"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            assertEquals(2, stack.termList.size());
            assertEquals("personal(3,20,1)", stack.Pop().toString());
            assertEquals("computer(3,20,2)", stack.Pop().toString());
        }

        [Test]
        public void TestFieldTermStackIndex2w1wSearch1partial()
        {
            makeIndex2w1w();

            FieldQuery fq = new FieldQuery(tq("computer"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            assertEquals(1, stack.termList.size());
            assertEquals("computer(3,20,2)", stack.Pop().toString());
        }

        [Test]
        public void TestFieldTermStackIndex2w1wSearch1term1phrase()
        {
            makeIndex2w1w();

            BooleanQuery bq = new BooleanQuery();
            bq.Add(tq("pc"), Occur.SHOULD);
            bq.Add(pqF("personal", "computer"), Occur.SHOULD);
            FieldQuery fq = new FieldQuery(bq, true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            assertEquals(2, stack.termList.size());
            TermInfo ti = stack.Pop();
            assertEquals("pc(3,20,1)", ti.toString());
            assertEquals("personal(3,20,1)", ti.Next.toString());
            assertSame(ti, ti.Next.Next);
            assertEquals("computer(3,20,2)", stack.Pop().toString());
        }

        [Test]
        public void TestFieldPhraseListIndex1w2wSearch1phrase()
        {
            makeIndex1w2w();

            FieldQuery fq = new FieldQuery(pqF("personal", "computer"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(1, fpl.PhraseList.size());
            assertEquals("personalcomputer(1.0)((3,5))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
            assertEquals(3, fpl.PhraseList[0].StartOffset);
            assertEquals(5, fpl.PhraseList[0].EndOffset);
        }

        [Test]
        public void TestFieldPhraseListIndex1w2wSearch1partial()
        {
            makeIndex1w2w();

            FieldQuery fq = new FieldQuery(tq("computer"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(1, fpl.PhraseList.size());
            assertEquals("computer(1.0)((3,5))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
            assertEquals(3, fpl.PhraseList[0].StartOffset);
            assertEquals(5, fpl.PhraseList[0].EndOffset);
        }

        [Test]
        public void TestFieldPhraseListIndex1w2wSearch1term1phrase()
        {
            makeIndex1w2w();

            BooleanQuery bq = new BooleanQuery();
            bq.Add(tq("pc"), Occur.SHOULD);
            bq.Add(pqF("personal", "computer"), Occur.SHOULD);
            FieldQuery fq = new FieldQuery(bq, true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(1, fpl.PhraseList.size());
            assertTrue(fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture).IndexOf("(1.0)((3,5))", StringComparison.Ordinal) > 0); // LUCENENET specific: use invariant culture, since we are culture-aware
            assertEquals(3, fpl.PhraseList[0].StartOffset);
            assertEquals(5, fpl.PhraseList[0].EndOffset);
        }

        [Test]
        public void TestFieldPhraseListIndex2w1wSearch1term()
        {
            makeIndex2w1w();

            FieldQuery fq = new FieldQuery(tq("pc"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(1, fpl.PhraseList.size());
            assertEquals("pc(1.0)((3,20))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
            assertEquals(3, fpl.PhraseList[0].StartOffset);
            assertEquals(20, fpl.PhraseList[0].EndOffset);
        }

        [Test]
        public void TestFieldPhraseListIndex2w1wSearch1phrase()
        {
            makeIndex2w1w();

            FieldQuery fq = new FieldQuery(pqF("personal", "computer"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(1, fpl.PhraseList.size());
            assertEquals("personalcomputer(1.0)((3,20))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
            assertEquals(3, fpl.PhraseList[0].StartOffset);
            assertEquals(20, fpl.PhraseList[0].EndOffset);
        }

        [Test]
        public void TestFieldPhraseListIndex2w1wSearch1partial()
        {
            makeIndex2w1w();

            FieldQuery fq = new FieldQuery(tq("computer"), true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(1, fpl.PhraseList.size());
            assertEquals("computer(1.0)((3,20))", fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture)); // LUCENENET specific: use invariant culture, since we are culture-aware
            assertEquals(3, fpl.PhraseList[0].StartOffset);
            assertEquals(20, fpl.PhraseList[0].EndOffset);
        }

        [Test]
        public void TestFieldPhraseListIndex2w1wSearch1term1phrase()
        {
            makeIndex2w1w();

            BooleanQuery bq = new BooleanQuery();
            bq.Add(tq("pc"), Occur.SHOULD);
            bq.Add(pqF("personal", "computer"), Occur.SHOULD);
            FieldQuery fq = new FieldQuery(bq, true, true);
            FieldTermStack stack = new FieldTermStack(reader, 0, F, fq);
            FieldPhraseList fpl = new FieldPhraseList(stack, fq);
            assertEquals(1, fpl.PhraseList.size());
            assertTrue(fpl.PhraseList[0].ToString(CultureInfo.InvariantCulture).IndexOf("(1.0)((3,20))", StringComparison.Ordinal) > 0); // LUCENENET specific: use invariant culture, since we are culture-aware
            assertEquals(3, fpl.PhraseList[0].StartOffset);
            assertEquals(20, fpl.PhraseList[0].EndOffset);
        }

        private void makeIndex1w()
        {
            //           11111111112
            // 012345678901234567890
            // I'll buy a Macintosh
            //            Mac
            //            MacBook
            // 0    1   2 3
            makeSynonymIndex("I'll buy a Macintosh",
                t("I'll", 0, 4),
                t("buy", 5, 8),
                t("a", 9, 10),
                t("Macintosh", 11, 20), t("Mac", 11, 20, 0), t("MacBook", 11, 20, 0));
        }

        private void makeIndex1w2w()
        {
            //           1111111
            // 01234567890123456
            // My pc was broken
            //    personal computer
            // 0  1  2   3
            makeSynonymIndex("My pc was broken",
                t("My", 0, 2),
                t("pc", 3, 5), t("personal", 3, 5, 0), t("computer", 3, 5),
                t("was", 6, 9),
                t("broken", 10, 16));
        }

        private void makeIndex2w1w()
        {
            //           1111111111222222222233
            // 01234567890123456789012345678901
            // My personal computer was broken
            //    pc
            // 0  1        2        3   4
            makeSynonymIndex("My personal computer was broken",
                t("My", 0, 2),
                t("personal", 3, 20), t("pc", 3, 20, 0), t("computer", 3, 20),
                t("was", 21, 24),
                t("broken", 25, 31));
        }

        void makeSynonymIndex(String value, params Token[] tokens)
        {
            Analyzer analyzer = new TokenArrayAnalyzer(tokens);
            make1dmfIndex(analyzer, value);
        }

        public static Token t(String text, int startOffset, int endOffset)
        {
            return t(text, startOffset, endOffset, 1);
        }

        public static Token t(String text, int startOffset, int endOffset, int positionIncrement)
        {
            Token token = new Token(text, startOffset, endOffset);
            token.PositionIncrement = (positionIncrement);
            return token;
        }

        private sealed class TokenizerAnonymousClass : Tokenizer
        {
            private readonly Token[] tokens;

            public TokenizerAnonymousClass(AttributeFactory factory, TextReader reader, Token[] tokens)
                : base(factory, reader)
            {
                reusableToken = AddAttribute<ICharTermAttribute>();
                this.tokens = tokens;
            }

            private ICharTermAttribute reusableToken;
            private int p = 0;

            public override bool IncrementToken()
            {
                if (p >= tokens.Length) return false;
                ClearAttributes();
                tokens[p++].CopyTo(reusableToken);
                return true;
            }

            public override void Reset()
            {
                base.Reset();
                this.p = 0;
            }
        }

        public sealed class TokenArrayAnalyzer : Analyzer
        {
            internal readonly Token[] tokens;
            public TokenArrayAnalyzer(params Token[] tokens)
            {
                this.tokens = tokens;
            }

            protected internal override TokenStreamComponents CreateComponents(String fieldName, TextReader reader)
            {
                Tokenizer ts = new TokenizerAnonymousClass(Token.TOKEN_ATTRIBUTE_FACTORY, reader, tokens);
                return new TokenStreamComponents(ts);
            }
        }
    }
}
