using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Directory = Lucene.Net.Store.Directory;
using JCG = J2N.Collections.Generic;

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

    public abstract class AbstractTestCase : LuceneTestCase
    {
        protected readonly String F = "f";
        protected readonly String F1 = "f1";
        protected readonly String F2 = "f2";
        protected Directory dir;
        protected Analyzer analyzerW;
        protected Analyzer analyzerB;
        protected Analyzer analyzerK;
        protected IndexReader reader;

        protected static readonly String[] shortMVValues = {
            "",
            "",
            "a b c",
            "",   // empty data in multi valued field
            "d e"
        };

        protected static readonly String[] longMVValues = {
            "Followings are the examples of customizable parameters and actual examples of customization:",
            "The most search engines use only one of these methods. Even the search engines that says they can use the both methods basically"
        };

        // test data for LUCENE-1448 bug
        protected static readonly String[] biMVValues = {
            "\nLucene/Solr does not require such additional hardware.",
            "\nWhen you talk about processing speed, the"
        };

        protected static readonly String[] strMVValues = {
            "abc",
            "defg",
            "hijkl"
        };

        public override void SetUp()
        {
            base.SetUp();
            analyzerW = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            analyzerB = new BigramAnalyzer();
            analyzerK = new MockAnalyzer(Random, MockTokenizer.KEYWORD, false);
            dir = NewDirectory();
        }

        public override void TearDown()
        {
            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }
            dir.Dispose();
            base.TearDown();
        }

        protected Query tq(String text)
        {
            return tq(1F, text);
        }

        protected Query tq(float boost, String text)
        {
            return tq(boost, F, text);
        }

        protected Query tq(String field, String text)
        {
            return tq(1F, field, text);
        }

        protected Query tq(float boost, String field, String text)
        {
            Query query = new TermQuery(new Term(field, text));
            query.Boost = (boost);
            return query;
        }

        protected Query pqF(params String[] texts)
        {
            return pqF(1F, texts);
        }

        protected Query pqF(float boost, params String[] texts)
        {
            return pqF(boost, 0, texts);
        }

        protected Query pqF(float boost, int slop, params String[] texts)
        {
            return pq(boost, slop, F, texts);
        }

        protected Query pq(String field, params String[] texts)
        {
            return pq(1F, 0, field, texts);
        }

        protected Query pq(float boost, String field, params String[] texts)
        {
            return pq(boost, 0, field, texts);
        }

        protected Query pq(float boost, int slop, String field, params String[] texts)
        {
            PhraseQuery query = new PhraseQuery();
            foreach (String text in texts)
            {
                query.Add(new Term(field, text));
            }
            query.Boost = (boost);
            query.Slop = (slop);
            return query;
        }

        protected Query dmq(params Query[] queries)
        {
            return dmq(0.0F, queries);
        }

        protected Query dmq(float tieBreakerMultiplier, params Query[] queries)
        {
            DisjunctionMaxQuery query = new DisjunctionMaxQuery(tieBreakerMultiplier);
            foreach (Query q in queries)
            {
                query.Add(q);
            }
            return query;
        }

        protected void assertCollectionQueries(ICollection<Query> actual, params Query[] expected)
        {
            assertEquals(expected.Length, actual.size());
            foreach (Query query in expected)
            {
                assertTrue(actual.Contains(query));
            }
        }

        protected IList<BytesRef> analyze(String text, String field, Analyzer analyzer)
        {
            IList<BytesRef> bytesRefs = new JCG.List<BytesRef>();

            TokenStream tokenStream = analyzer.GetTokenStream(field, text);
            try
            {
                ITermToBytesRefAttribute termAttribute = tokenStream.GetAttribute<ITermToBytesRefAttribute>();

                BytesRef bytesRef = termAttribute.BytesRef;

                tokenStream.Reset();

                while (tokenStream.IncrementToken())
                {
                    termAttribute.FillBytesRef();
                    bytesRefs.Add(BytesRef.DeepCopyOf(bytesRef));
                }

                tokenStream.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(tokenStream);
            }

            return bytesRefs;
        }

        protected PhraseQuery toPhraseQuery(IList<BytesRef> bytesRefs, String field)
        {
            PhraseQuery phraseQuery = new PhraseQuery();
            foreach (BytesRef bytesRef in bytesRefs)
            {
                phraseQuery.Add(new Term(field, bytesRef));
            }
            return phraseQuery;
        }

        internal sealed class BigramAnalyzer : Analyzer
        {
            protected internal override TokenStreamComponents CreateComponents(String fieldName, TextReader reader)
            {
                return new TokenStreamComponents(new BasicNGramTokenizer(reader));
            }
        }

        internal sealed class BasicNGramTokenizer : Tokenizer
        {

            public static readonly int DEFAULT_N_SIZE = 2;
            public static readonly String DEFAULT_DELIMITERS = " \t\n.,";
            private readonly int n;
            private readonly String delimiters;
            private int startTerm;
            private int lenTerm;
            private int startOffset;
            private int nextStartOffset;
            private int ch;
            private String snippet;
            private StringBuilder snippetBuffer;
            private static readonly int BUFFER_SIZE = 4096;
            private char[] charBuffer;
            private int charBufferIndex;
            private int charBufferLen;

            public BasicNGramTokenizer(TextReader @in)
                            : this(@in, DEFAULT_N_SIZE)
            {

            }

            public BasicNGramTokenizer(TextReader @in, int n)
                            : this(@in, n, DEFAULT_DELIMITERS)
            {
            }

            public BasicNGramTokenizer(TextReader @in, String delimiters)
                            : this(@in, DEFAULT_N_SIZE, delimiters)
            {

            }

            public BasicNGramTokenizer(TextReader @in, int n, String delimiters)
                            : base(@in)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();

                this.n = n;
                this.delimiters = delimiters;
                startTerm = 0;
                nextStartOffset = 0;
                snippet = null;
                snippetBuffer = new StringBuilder();
                charBuffer = new char[BUFFER_SIZE];
                charBufferIndex = BUFFER_SIZE;
                charBufferLen = 0;
                ch = 0;
            }

            ICharTermAttribute termAtt;
            IOffsetAttribute offsetAtt;

            public override bool IncrementToken()
            {
                if (!GetNextPartialSnippet())
                    return false;
                ClearAttributes();
                termAtt.SetEmpty().Append(snippet, startTerm, lenTerm); // LUCENENET: Corrected 3rd parameter
                offsetAtt.SetOffset(CorrectOffset(startOffset), CorrectOffset(startOffset + lenTerm));
                return true;
            }

            private int getFinalOffset()
            {
                return nextStartOffset;
            }

            public override void End()
            {
                base.End();
                offsetAtt.SetOffset(getFinalOffset(), getFinalOffset());
            }

            internal bool GetNextPartialSnippet()
            {
                if (snippet != null && snippet.Length >= startTerm + 1 + n)
                {
                    startTerm++;
                    startOffset++;
                    lenTerm = n;
                    return true;
                }
                return GetNextSnippet();
            }

            internal bool GetNextSnippet()
            {
                startTerm = 0;
                startOffset = nextStartOffset;
                snippetBuffer.Remove(0, snippetBuffer.Length);
                while (true)
                {
                    if (ch != -1)
                        ch = ReadCharFromBuffer();
                    if (ch == -1) break;
                    else if (!IsDelimiter(ch))
                        snippetBuffer.append((char)ch);
                    else if (snippetBuffer.Length > 0)
                        break;
                    else
                        startOffset++;
                }
                if (snippetBuffer.Length == 0)
                    return false;
                snippet = snippetBuffer.toString();
                lenTerm = snippet.Length >= n ? n : snippet.Length;
                return true;
            }

            internal int ReadCharFromBuffer()
            {
                if (charBufferIndex >= charBufferLen)
                {
                    charBufferLen = m_input.read(charBuffer);
                    if (charBufferLen == -1)
                    {
                        return -1;
                    }
                    charBufferIndex = 0;
                }
                int c = charBuffer[charBufferIndex++];
                nextStartOffset++;
                return c;
            }

            internal bool IsDelimiter(int c)
            {
                return delimiters.IndexOf((char)c) >= 0;
            }

            public override void Reset()
            {
                base.Reset();
                startTerm = 0;
                nextStartOffset = 0;
                snippet = null;
                snippetBuffer.Length = (0);
                charBufferIndex = BUFFER_SIZE;
                charBufferLen = 0;
                ch = 0;
            }
        }

        protected void make1d1fIndex(String value)
        {
            make1dmfIndex(value);
        }

        protected void make1d1fIndexB(String value)
        {
            make1dmfIndexB(value);
        }

        protected void make1dmfIndex(params String[] values)
        {
            make1dmfIndex(analyzerW, values);
        }

        protected void make1dmfIndexB(params String[] values)
        {
            make1dmfIndex(analyzerB, values);
        }

        // make 1 doc with multi valued field
        protected void make1dmfIndex(Analyzer analyzer, params String[] values)
        {
            IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(
                TEST_VERSION_CURRENT, analyzer).SetOpenMode(OpenMode.CREATE));
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = (true);
            customType.StoreTermVectorOffsets = (true);
            customType.StoreTermVectorPositions = (true);
            foreach (String value in values)
            {
                doc.Add(new Field(F, value, customType));
            }
            writer.AddDocument(doc);
            writer.Dispose();
            if (reader != null) reader.Dispose();
            reader = DirectoryReader.Open(dir);
        }

        // make 1 doc with multi valued & not analyzed field
        protected void make1dmfIndexNA(params String[] values)
        {
            IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(
                TEST_VERSION_CURRENT, analyzerK).SetOpenMode(OpenMode.CREATE));
            Document doc = new Document();
            FieldType customType = new FieldType(TextField.TYPE_STORED);
            customType.StoreTermVectors = (true);
            customType.StoreTermVectorOffsets = (true);
            customType.StoreTermVectorPositions = (true);
            foreach (String value in values)
            {
                doc.Add(new Field(F, value, customType));
                //doc.Add( new Field( F, value, Store.YES, Index.NOT_ANALYZED, TermVector.WITH_POSITIONS_OFFSETS ) );
            }
            writer.AddDocument(doc);
            writer.Dispose();
            if (reader != null) reader.Dispose();
            reader = DirectoryReader.Open(dir);
        }

        protected void makeIndexShortMV()
        {

            //  0
            // ""
            //  1
            // ""

            //  234567
            // "a b c"
            //  0 1 2

            //  8
            // ""

            //   111
            //  9012
            // "d e"
            //  3 4
            make1dmfIndex(shortMVValues);
        }

        protected void makeIndexLongMV()
        {
            //           11111111112222222222333333333344444444445555555555666666666677777777778888888888999
            // 012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012
            // Followings are the examples of customizable parameters and actual examples of customization:
            // 0          1   2   3        4  5            6          7   8      9        10 11

            //        1                                                                                                   2
            // 999999900000000001111111111222222222233333333334444444444555555555566666666667777777777888888888899999999990000000000111111111122
            // 345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901
            // The most search engines use only one of these methods. Even the search engines that says they can use the both methods basically
            // 12  13  (14)   (15)     16  17   18  19 20    21       22   23 (24)   (25)     26   27   28   29  30  31  32   33      34

            make1dmfIndex(longMVValues);
        }

        protected void makeIndexLongMVB()
        {
            // "*" ... LF

            //           1111111111222222222233333333334444444444555555
            // 01234567890123456789012345678901234567890123456789012345
            // *Lucene/Solr does not require such additional hardware.
            //  Lu 0        do 10    re 15   su 21       na 31
            //   uc 1        oe 11    eq 16   uc 22       al 32
            //    ce 2        es 12    qu 17   ch 23         ha 33
            //     en 3          no 13  ui 18     ad 24       ar 34
            //      ne 4          ot 14  ir 19     dd 25       rd 35
            //       e/ 5                 re 20     di 26       dw 36
            //        /S 6                           it 27       wa 37
            //         So 7                           ti 28       ar 38
            //          ol 8                           io 29       re 39
            //           lr 9                           on 30

            // 5555666666666677777777778888888888999999999
            // 6789012345678901234567890123456789012345678
            // *When you talk about processing speed, the
            //  Wh 40         ab 48     es 56         th 65
            //   he 41         bo 49     ss 57         he 66
            //    en 42         ou 50     si 58
            //       yo 43       ut 51     in 59
            //        ou 44         pr 52   ng 60
            //           ta 45       ro 53     sp 61
            //            al 46       oc 54     pe 62
            //             lk 47       ce 55     ee 63
            //                                    ed 64

            make1dmfIndexB(biMVValues);
        }

        protected void makeIndexStrMV()
        {

            //  0123
            // "abc"

            //  34567
            // "defg"

            //     111
            //  789012
            // "hijkl"
            make1dmfIndexNA(strMVValues);
        }
    }
}
